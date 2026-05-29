using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    [FormerlySerializedAs("levelManager")]
    [SerializeField] private LevelManager _levelManager;
    [FormerlySerializedAs("slotManager")]
    [SerializeField] private SlotManager _slotManager;
    [FormerlySerializedAs("unitPool")]
    [SerializeField] private UnitPool _unitPool;
    [FormerlySerializedAs("levels")]
    [SerializeField] private LevelCatalogConfig _levelCatalog;
    [FormerlySerializedAs("debugEndStateLogs")]
    [SerializeField] private bool _debugEndStateLogs;

    private readonly HashSet<Unit> _unitsInFlight = new HashSet<Unit>();

    private int _currentLevelIndex;
    private int _pendingOperations;
    private bool _winPending;
    private bool _failPending;

    public GameState CurrentState { get; private set; } = GameState.Loading;
    public bool HasUnitsInFlight => _unitsInFlight.Count > 0;
    public int CurrentLevelIndex => _currentLevelIndex;
    public int TotalLevelCount => _levelCatalog != null ? _levelCatalog.LevelCount : 0;

    public bool CanAcceptPlayerInput()
    {
        return CurrentState == GameState.Playing || CurrentState == GameState.Resolving;
    }

    private void Awake()
    {
        _unitPool.Initialize();
        _levelManager.Initialize();
        _slotManager.Initialize(_unitPool);
    }

    private void OnEnable()
    {
        GameplayEvents.UnitSelectionRequested += TrySelectUnit;
        GameplayEvents.RetryRequested += RetryLevel;
        GameplayEvents.ContinueRequested += ContinueToNextLevel;
        GameplayEvents.SlotResolutionStarted += OnSlotResolutionStarted;
        GameplayEvents.SlotResolutionCompleted += OnSlotResolutionCompleted;
        GameplayEvents.SlotQueueDrained += OnSlotQueueDrained;
        GameplayEvents.GridCleared += OnGridCleared;
        GameplayEvents.SlotsFullNoMatch += OnSlotsFullNoMatch;
        GameplayEvents.UnitsInFlightCheckRequested += HasActiveUnitsInFlight;
    }

    private void OnDisable()
    {
        GameplayEvents.UnitSelectionRequested -= TrySelectUnit;
        GameplayEvents.RetryRequested -= RetryLevel;
        GameplayEvents.ContinueRequested -= ContinueToNextLevel;
        GameplayEvents.SlotResolutionStarted -= OnSlotResolutionStarted;
        GameplayEvents.SlotResolutionCompleted -= OnSlotResolutionCompleted;
        GameplayEvents.SlotQueueDrained -= OnSlotQueueDrained;
        GameplayEvents.GridCleared -= OnGridCleared;
        GameplayEvents.SlotsFullNoMatch -= OnSlotsFullNoMatch;
        GameplayEvents.UnitsInFlightCheckRequested -= HasActiveUnitsInFlight;
    }

    private void Start()
    {
        _currentLevelIndex = SaveManager.LoadLevelIndex();
        StartCurrentLevel();
    }

    private void Update()
    {
        if (!CanAcceptPlayerInput() || CurrentState == GameState.Failed || CurrentState == GameState.Won)
        {
            return;
        }

        if (_slotManager != null && _slotManager.IsHardFailState && !HasUnitsInFlight)
        {
            LogEndState("Update watchdog detected hard fail state");
            GameplayEvents.RaiseSlotsFullNoMatch();
        }

        if (_pendingOperations > 0 && _slotManager != null && (!_slotManager.HasPendingWork || _slotManager.IsSettledAndEmpty) && !HasUnitsInFlight)
        {
            LogEndState("Update watchdog cleared stale pending operation");
            _pendingOperations = 0;
        }

        if (_winPending || _failPending)
        {
            TryResolveEndStates();
        }
    }

    public void TrySelectUnit(Unit unit)
    {
        if (unit == null || !CanAcceptPlayerInput())
        {
            return;
        }

        if (_unitsInFlight.Contains(unit))
        {
            return;
        }

        if (unit.State != UnitState.OnGrid || !unit.IsSelectable || unit.IsBoxed)
        {
            return;
        }

        if (!GameplayEvents.CanAcceptIncomingUnit(unit))
        {
            return;
        }

        _unitsInFlight.Add(unit);
        _levelManager.RequestSelectUnit(unit, () =>
        {
            _unitsInFlight.Remove(unit);
            TryResolveEndStates();
        });
    }

    private void OnSlotResolutionStarted()
    {
        _pendingOperations++;
        if (CurrentState == GameState.Playing)
        {
            SetState(GameState.Resolving);
        }
    }

    private void OnSlotResolutionCompleted()
    {
        _pendingOperations = Mathf.Max(0, _pendingOperations - 1);
        if (_pendingOperations > 0)
        {
            return;
        }

        TryResolveEndStates();

        if (CurrentState == GameState.Resolving)
        {
            SetState(GameState.Playing);
        }
    }

    private void OnGridCleared()
    {
        _winPending = true;
        LogEndState("OnGridCleared: win pending true");
        TryResolveEndStates();
    }

    private void OnSlotsFullNoMatch()
    {
        if (CurrentState == GameState.Won)
        {
            LogEndState("OnSlotsFullNoMatch ignored because game already won");
            return;
        }

        // Full slot with no match must lose, even if all grid units were already cleared.
        _winPending = false;
        _failPending = true;
        LogEndState("OnSlotsFullNoMatch: fail pending true");
        if (!TryTriggerFailNow())
        {
            TryResolveEndStates();
        }
    }

    private void OnSlotQueueDrained()
    {
        LogEndState("OnSlotQueueDrained");
        TryResolveEndStates();
    }

    private void TryResolveEndStates()
    {
        if (_pendingOperations > 0 || (_slotManager.HasPendingWork && !_slotManager.IsSettledAndEmpty) || HasUnitsInFlight)
        {
            LogEndState(
                $"TryResolveEndStates blocked | pendingOps={_pendingOperations} " +
                $"slotPending={_slotManager.HasPendingWork} inFlight={HasUnitsInFlight}");
            return;
        }

        LogEndState(
            $"TryResolveEndStates evaluating | winPending={_winPending} failPending={_failPending} " +
            $"gridEmpty={_levelManager.IsGridEmpty} slotEmpty={_slotManager.IsSettledAndEmpty}");

        if (TryTriggerWin())
        {
            _failPending = false;
            return;
        }

        TryTriggerFailNow();
    }

    private bool TryTriggerWin()
    {
        if (!_winPending)
        {
            LogEndState("TryTriggerWin skipped: winPending false");
            return false;
        }

        if (!_levelManager.IsGridEmpty || !_slotManager.IsSettledAndEmpty)
        {
            LogEndState(
                $"TryTriggerWin blocked | gridEmpty={_levelManager.IsGridEmpty} " +
                $"slotSettledEmpty={_slotManager.IsSettledAndEmpty} slotPending={_slotManager.HasPendingWork}");
            return false;
        }

        SetState(GameState.Won);
        GameplayEvents.RaiseGameWon();
        _winPending = false;
        LogEndState("Win UI shown");
        return true;
    }

    private bool TryTriggerFailNow()
    {
        if (!_failPending)
        {
            return false;
        }

        if (_pendingOperations > 0 || _slotManager.HasPendingWork || HasUnitsInFlight)
        {
            LogEndState(
                $"TryTriggerFailNow blocked | pendingOps={_pendingOperations} " +
                $"slotPending={_slotManager.HasPendingWork} inFlight={HasUnitsInFlight}");
            return false;
        }

        if (_slotManager.OccupiedCount < SlotManager.SlotCount || _slotManager.HasMatchPossible() || !_slotManager.IsQueueEmpty)
        {
            LogEndState(
                $"TryTriggerFailNow blocked by slots | occupied={_slotManager.OccupiedCount} " +
                $"hasMatch={_slotManager.HasMatchPossible()} queueEmpty={_slotManager.IsQueueEmpty}");
            return false;
        }

        SetState(GameState.Failed);
        GameplayEvents.RaiseGameFailed();
        _failPending = false;
        LogEndState("Fail UI shown");
        return true;
    }

    public void RetryLevel()
    {
        if (CurrentState != GameState.Won && CurrentState != GameState.Failed)
        {
            return;
        }

        StartCurrentLevel();
    }

    public void ContinueToNextLevel()
    {
        if (CurrentState != GameState.Won)
        {
            return;
        }

        if (_levelCatalog == null || _levelCatalog.LevelCount == 0)
        {
            return;
        }

        _currentLevelIndex = (_currentLevelIndex + 1) % _levelCatalog.LevelCount;
        SaveManager.SaveLevelIndex(_currentLevelIndex);
        StartCurrentLevel();
    }

    private void StartCurrentLevel()
    {
        _winPending = false;
        _failPending = false;
        _pendingOperations = 0;
        _unitsInFlight.Clear();

        SetState(GameState.Loading);
        GameplayEvents.RaiseLevelStarting();

        if (_levelCatalog == null || _levelCatalog.LevelCount == 0)
        {
            Debug.LogError("GameManager: Assign a Level Catalog Config with at least one level entry.");
            return;
        }

        _currentLevelIndex = Mathf.Clamp(_currentLevelIndex, 0, _levelCatalog.LevelCount - 1);
        LevelCatalogEntry entry = _levelCatalog.GetEntry(_currentLevelIndex);
        if (entry == null || entry.levelData == null)
        {
            Debug.LogError($"GameManager: Level entry at index {_currentLevelIndex} is missing Level Data.");
            return;
        }

        _levelManager.LoadLevel(entry.levelData);
        GameplayEvents.RaiseLevelDisplayChanged(_levelCatalog.GetDisplayText(_currentLevelIndex));
        SetState(GameState.Playing);
    }

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        LogEndState($"State -> {newState}");
    }

    private bool HasActiveUnitsInFlight()
    {
        return HasUnitsInFlight;
    }

    private void LogEndState(string message)
    {
        if (!_debugEndStateLogs)
        {
            return;
        }

        Debug.Log($"[GameManager] {message}");
    }
}
