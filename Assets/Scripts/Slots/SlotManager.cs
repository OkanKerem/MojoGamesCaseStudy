using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SlotManager : MonoBehaviour
{
    public const int SlotCount = 7;

    [FormerlySerializedAs("slotAnchors")]
    [SerializeField] private Transform[] _slotAnchors = new Transform[SlotCount];
    [FormerlySerializedAs("normalShiftHopHeight")]
    [SerializeField] private float _normalShiftHopHeight = 0.2f;
    [FormerlySerializedAs("matchHopHeight")]
    [SerializeField] private float _matchHopHeight = 0.5f;
    [FormerlySerializedAs("slotMoveDuration")]
    [SerializeField] private float _slotMoveDuration = 0.25f;
    [FormerlySerializedAs("matchMoveDuration")]
    [SerializeField] private float _matchMoveDuration = 0.3f;
    [FormerlySerializedAs("matchVfxDelay")]
    [SerializeField] private float _matchVfxDelay = 0.12f;
    [FormerlySerializedAs("matchJumpStartDelay")]
    [SerializeField] private float _matchJumpStartDelay = 0.03f;
    [FormerlySerializedAs("matchCollapseToMiddle")]
    [SerializeField] [Range(0f, 1f)] private float _matchCollapseToMiddle = 0.5f;
    [FormerlySerializedAs("failCheckDelay")]
    [SerializeField] private float _failCheckDelay = 0.2f;
    [FormerlySerializedAs("debugSlotLogs")]
    [SerializeField] private bool _debugSlotLogs;

    private readonly List<Unit> _slotUnits = new List<Unit>(SlotCount);
    private readonly HashSet<Unit> _slotUnitSet = new HashSet<Unit>();
    private readonly Dictionary<Unit, int> _unitSlotIndex = new Dictionary<Unit, int>();
    private readonly Queue<Unit> _pendingSlotUnits = new Queue<Unit>();
    private readonly HashSet<Unit> _queuedUnits = new HashSet<Unit>();
    private readonly HashSet<int> _reservedMatchSlotIndices = new HashSet<int>();
    private readonly Dictionary<Unit, Action<bool>> _insertCallbacks = new Dictionary<Unit, Action<bool>>();

    private UnitPool _unitPool;
    private bool _isResolvingSlotAction;
    private bool _isResolvingMatches;
    private bool _matchResolutionRequested;
    private bool _slotResolutionEventActive;
    private int _internalAnimations;
    private Unit _preferredMatchUnit;
    private UnitTypeData _preferredMatchType;
    private Coroutine _queueProcessor;
    private Coroutine _delayedFailCheckRoutine;

    public int OccupiedCount => _slotUnits.Count;
    public int PendingCount => _pendingSlotUnits.Count;
    public bool IsEmpty => _slotUnits.Count == 0 && _pendingSlotUnits.Count == 0;
    public bool IsQueueEmpty => _pendingSlotUnits.Count == 0;
    public bool HasPendingWork => _isResolvingSlotAction || _isResolvingMatches || _pendingSlotUnits.Count > 0 || _internalAnimations > 0;
    public bool IsSettledAndEmpty =>
        _slotUnits.Count == 0
        && _pendingSlotUnits.Count == 0
        && _queuedUnits.Count == 0
        && _reservedMatchSlotIndices.Count == 0
        && _insertCallbacks.Count == 0
        && _internalAnimations == 0;
    public bool IsBusy => _internalAnimations > 0;
    public bool IsHardFailState =>
        _slotUnits.Count >= SlotCount
        && !HasPendingWork
        && _pendingSlotUnits.Count == 0
        && !HasMatchPossible();

    private void OnEnable()
    {
        GameplayEvents.CanAcceptIncomingUnitCheckRequested += CanAcceptIncomingUnit;
        GameplayEvents.SlotInsertionRequested += RequestSlotInsertion;
        GameplayEvents.SlotClearRequested += ClearAll;
    }

    private void OnDisable()
    {
        GameplayEvents.CanAcceptIncomingUnitCheckRequested -= CanAcceptIncomingUnit;
        GameplayEvents.SlotInsertionRequested -= RequestSlotInsertion;
        GameplayEvents.SlotClearRequested -= ClearAll;
    }

    public void Initialize(UnitPool unitPool)
    {
        _unitPool = unitPool;

        if (_slotAnchors == null || _slotAnchors.Length < SlotCount)
        {
            Debug.LogError("SlotManager: Assign 7 slot anchor transforms.");
        }
    }

    public bool CanAcceptIncomingUnit(Unit unit)
    {
        if (unit == null || unit.UnitType == null)
        {
            return false;
        }

        if (_slotUnitSet.Contains(unit) || _queuedUnits.Contains(unit))
        {
            return false;
        }

        int projectedCount = _slotUnits.Count + _queuedUnits.Count + _reservedMatchSlotIndices.Count + 1;
        if (projectedCount <= SlotCount)
        {
            return true;
        }

        return HasPendingWork;
    }

    public void RequestSlotInsertion(Unit unit, Action<bool> onComplete)
    {
        if (unit == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        if (_slotUnitSet.Contains(unit))
        {
            LogSlot("Rejected duplicate slot unit");
            onComplete?.Invoke(false);
            return;
        }

        if (_queuedUnits.Contains(unit))
        {
            LogSlot("Rejected duplicate queued unit");
            onComplete?.Invoke(false);
            return;
        }

        if (!CanAcceptIncomingUnit(unit))
        {
            onComplete?.Invoke(false);
            return;
        }

        _insertCallbacks[unit] = onComplete;
        _pendingSlotUnits.Enqueue(unit);
        _queuedUnits.Add(unit);
        CancelDelayedFailCheck();

        if (_isResolvingSlotAction)
        {
            unit.SetState(UnitState.WaitingForSlot);
            LogSlot("Unit queued for slot insertion");
        }
        else
        {
            unit.SetState(UnitState.MovingToSlot);
        }

        EnsureQueueProcessor();
    }

    public void TryAddUnit(Unit unit, Action<bool> onComplete)
    {
        RequestSlotInsertion(unit, onComplete);
    }

    public bool HasMatchPossible()
    {
        return FindFirstConsecutiveMatch() != null;
    }

    public void ClearAll()
    {
        if (_queueProcessor != null)
        {
            StopCoroutine(_queueProcessor);
            _queueProcessor = null;
        }

        CancelDelayedFailCheck();

        for (int i = _slotUnits.Count - 1; i >= 0; i--)
        {
            _unitPool.ReturnUnit(_slotUnits[i]);
        }

        _slotUnits.Clear();
        _slotUnitSet.Clear();
        _unitSlotIndex.Clear();
        _reservedMatchSlotIndices.Clear();
        _pendingSlotUnits.Clear();
        _queuedUnits.Clear();
        _insertCallbacks.Clear();
        _isResolvingSlotAction = false;
        _isResolvingMatches = false;
        _matchResolutionRequested = false;
        _slotResolutionEventActive = false;
        _internalAnimations = 0;
        _preferredMatchUnit = null;
        _preferredMatchType = null;
    }

    private void EnsureQueueProcessor()
    {
        if (_queueProcessor != null)
        {
            return;
        }

        _queueProcessor = StartCoroutine(ProcessSlotQueue());
    }

    private IEnumerator ProcessSlotQueue()
    {
        _isResolvingSlotAction = true;
        BeginSlotResolutionEvent();

        while (_pendingSlotUnits.Count > 0 || HasMatchPossible() || _isResolvingMatches || _internalAnimations > 0)
        {
            yield return SeatQueuedUnitsIntoAvailableSlots();

            if (HasMatchPossible())
            {
                yield return ResolveMatchesRoutine();
                continue;
            }

            if (_pendingSlotUnits.Count > 0 && !HasAvailableVisualSlot() && !IsWaitingForSlotSpace())
            {
                Unit unit = _pendingSlotUnits.Dequeue();
                _queuedUnits.Remove(unit);
                CompleteInsertCallback(unit, false);
                continue;
            }

            if (_internalAnimations > 0)
            {
                yield return null;
                continue;
            }

            break;
        }

        _queueProcessor = null;
        _isResolvingSlotAction = false;
        LogSlot("Slot resolution complete");
        EndSlotResolutionEvent();
        GameplayEvents.RaiseSlotQueueDrained();
        EvaluateFailCondition();
    }

    private IEnumerator ProcessSingleUnitInsertion(Unit unit, Action<bool> onComplete)
    {
        _slotUnitSet.Add(unit);

        int insertIndex = GetInsertIndex(unit.UnitType);
        _slotUnits.Insert(insertIndex, unit);

        bool repositionDone = false;
        RepositionSlotUnits(insertIndex, unit, () => repositionDone = true);

        while (!repositionDone)
        {
            yield return null;
        }

        if (!_slotUnitSet.Contains(unit))
        {
            onComplete?.Invoke(false);
            yield break;
        }

        unit.SetState(UnitState.InSlot);
        RequestMatchResolution(unit.UnitType, unit);
        onComplete?.Invoke(true);
    }

    private IEnumerator SeatQueuedUnitsIntoAvailableSlots()
    {
        while (_pendingSlotUnits.Count > 0 && HasAvailableVisualSlot())
        {
            Unit unit = _pendingSlotUnits.Dequeue();
            _queuedUnits.Remove(unit);

            if (unit == null || unit.State == UnitState.Pooled || unit.State == UnitState.Matched)
            {
                CompleteInsertCallback(unit, false);
                continue;
            }

            LogSlot("Processing queued slot unit");
            unit.SetState(UnitState.MovingToSlot);

            bool success = false;
            yield return ProcessSingleUnitInsertion(unit, result => success = result);
            CompleteInsertCallback(unit, success);
        }
    }

    private void CompleteInsertCallback(Unit unit, bool success)
    {
        if (unit != null && _insertCallbacks.TryGetValue(unit, out Action<bool> callback))
        {
            _insertCallbacks.Remove(unit);
            callback?.Invoke(success);
        }
    }

    private bool WouldCreateMatchIfInserted(Unit unit)
    {
        if (unit == null || unit.UnitType == null)
        {
            return false;
        }

        int insertIndex = GetInsertIndex(unit.UnitType);
        int start = insertIndex;
        int end = insertIndex;

        while (start > 0 && _slotUnits[start - 1].UnitType != null && _slotUnits[start - 1].UnitType.Matches(unit.UnitType))
        {
            start--;
        }

        while (end < _slotUnits.Count && _slotUnits[end].UnitType != null && _slotUnits[end].UnitType.Matches(unit.UnitType))
        {
            end++;
        }

        return end - start + 1 >= 3;
    }

    private int GetInsertIndex(UnitTypeData unitType)
    {
        for (int i = _slotUnits.Count - 1; i >= 0; i--)
        {
            if (_slotUnits[i].UnitType != null && _slotUnits[i].UnitType.Matches(unitType))
            {
                return i + 1;
            }
        }

        return _slotUnits.Count;
    }

    private List<Unit> GetMatchingUnits(UnitTypeData unitType, Unit insertedUnit)
    {
        if (unitType == null || insertedUnit == null)
        {
            return null;
        }

        int insertIndex = _slotUnits.IndexOf(insertedUnit);
        if (insertIndex < 0)
        {
            return FindFirstConsecutiveMatch();
        }

        int start = insertIndex;
        int end = insertIndex;

        while (start > 0 && _slotUnits[start - 1].UnitType != null && _slotUnits[start - 1].UnitType.Matches(unitType))
        {
            start--;
        }

        while (end < _slotUnits.Count - 1 && _slotUnits[end + 1].UnitType != null && _slotUnits[end + 1].UnitType.Matches(unitType))
        {
            end++;
        }

        int runLength = end - start + 1;
        if (runLength < 3)
        {
            return null;
        }

        int matchStart = Mathf.Clamp(insertIndex - 2, start, end - 2);
        return _slotUnits.GetRange(matchStart, 3);
    }

    private List<Unit> FindFirstConsecutiveMatch()
    {
        for (int i = 0; i <= _slotUnits.Count - 3; i++)
        {
            UnitTypeData type = _slotUnits[i].UnitType;
            if (type == null)
            {
                continue;
            }

            if (type.Matches(_slotUnits[i + 1].UnitType) && type.Matches(_slotUnits[i + 2].UnitType))
            {
                return _slotUnits.GetRange(i, 3);
            }
        }

        return null;
    }

    private void RequestMatchResolution(UnitTypeData insertedType, Unit insertedUnit)
    {
        _preferredMatchType = insertedType;
        _preferredMatchUnit = insertedUnit;
        _matchResolutionRequested = true;
    }

    private IEnumerator ResolveMatchesRoutine()
    {
        _isResolvingMatches = true;

        while (true)
        {
            _matchResolutionRequested = false;

            List<Unit> matchedUnits = GetMatchingUnits(_preferredMatchType, _preferredMatchUnit);
            _preferredMatchType = null;
            _preferredMatchUnit = null;

            if (matchedUnits == null)
            {
                matchedUnits = FindFirstConsecutiveMatch();
            }

            if (matchedUnits == null)
            {
                if (_matchResolutionRequested)
                {
                    continue;
                }

                break;
            }

            yield return ResolveMatchBatch(matchedUnits);
        }

        _isResolvingMatches = false;
    }

    private IEnumerator ResolveMatchBatch(List<Unit> matchedUnits)
    {
        Vector3 matchVfxPosition = Vector3.zero;
        Vector3 collapseTarget = Vector3.zero;
        bool hasVfxPosition = matchedUnits != null && matchedUnits.Count > 0;
        if (hasVfxPosition)
        {
            int middleIndex = matchedUnits.Count / 2;
            collapseTarget = matchedUnits[middleIndex].transform.position;
            matchVfxPosition = collapseTarget;
        }

        for (int i = 0; i < matchedUnits.Count; i++)
        {
            Unit unit = matchedUnits[i];
            if (_unitSlotIndex.TryGetValue(unit, out int slotIndex))
            {
                _reservedMatchSlotIndices.Add(slotIndex);
            }

            _slotUnits.Remove(unit);
            _slotUnitSet.Remove(unit);
            _unitSlotIndex.Remove(unit);
            unit.SetState(UnitState.Matching);
            unit.transform.position += Vector3.up * Mathf.Max(0.35f, _matchHopHeight);
        }

        GameplayEvents.RaiseUnitsMatched(matchVfxPosition);

        if (_matchJumpStartDelay > 0f)
        {
            yield return new WaitForSeconds(_matchJumpStartDelay);
        }

        BeginInternalAnimation(matchedUnits.Count);
        int completedAnimations = 0;

        for (int i = 0; i < matchedUnits.Count; i++)
        {
            Unit unit = matchedUnits[i];
            Vector3 startPosition = unit.transform.position;
            Vector3 endPosition = Vector3.Lerp(startPosition, collapseTarget, _matchCollapseToMiddle);
            unit.PlayJumpAnimation();

            UnitAnimationHelper.MoveUnitWithHop(
                this,
                unit,
                startPosition,
                endPosition,
                _matchHopHeight,
                _matchMoveDuration,
                () =>
                {
                    unit.transform.position = endPosition;
                    completedAnimations++;
                    unit.SetState(UnitState.Matched);
                    _unitPool.ReturnUnit(unit);
                });
        }

        if (hasVfxPosition)
        {
            StartCoroutine(RaiseUnitsMatchedAfterDelay(matchVfxPosition));
        }

        while (completedAnimations < matchedUnits.Count)
        {
            if (_pendingSlotUnits.Count > 0 && HasAvailableVisualSlot())
            {
                yield return SeatQueuedUnitsIntoAvailableSlots();
                continue;
            }

            yield return null;
        }

        EndInternalAnimation(matchedUnits.Count);
        _reservedMatchSlotIndices.Clear();

        bool repositionDone = false;
        RepositionSlotUnits(0, null, () => repositionDone = true);

        while (!repositionDone)
        {
            yield return null;
        }
    }

    private void RepositionSlotUnits(int startIndex, Unit incomingUnit, Action onComplete)
    {
        if (_slotUnits.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        int firstIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, _slotUnits.Count - 1));
        int unitsToMove = _slotUnits.Count - firstIndex;

        if (unitsToMove <= 0)
        {
            onComplete?.Invoke();
            return;
        }

        BeginInternalAnimation(unitsToMove);
        int pendingMoves = unitsToMove;

        for (int i = firstIndex; i < _slotUnits.Count; i++)
        {
            Unit unit = _slotUnits[i];
            int visualSlotIndex = GetVisualSlotIndex(i);
            Vector3 target = _slotAnchors[visualSlotIndex].position;
            Vector3 start = unit.transform.position;
            _unitSlotIndex.TryGetValue(unit, out int previousIndex);
            bool slotIndexChanged = !_unitSlotIndex.ContainsKey(unit) || previousIndex != visualSlotIndex;
            _unitSlotIndex[unit] = visualSlotIndex;

            bool isIncomingUnit = incomingUnit != null && unit == incomingUnit;

            if (isIncomingUnit)
            {
                MoveIncomingUnit(unit, start, target, () => OnRepositionMoveComplete(ref pendingMoves, onComplete));
            }
            else if (slotIndexChanged)
            {
                unit.PlayJumpAnimation();
                UnitAnimationHelper.MoveUnitWithHop(
                    this,
                    unit,
                    start,
                    target,
                    _normalShiftHopHeight,
                    _slotMoveDuration,
                    () => OnRepositionMoveComplete(ref pendingMoves, onComplete));
            }
            else if ((start - target).sqrMagnitude > 0.0001f)
            {
                UnitAnimationHelper.MoveTo(this, unit, target, _slotMoveDuration, () => OnRepositionMoveComplete(ref pendingMoves, onComplete));
            }
            else
            {
                unit.transform.position = target;
                OnRepositionMoveComplete(ref pendingMoves, onComplete);
            }
        }
    }

    private void MoveIncomingUnit(Unit unit, Vector3 start, Vector3 target, Action onComplete)
    {
        UnitAnimationHelper.MoveTo(this, unit, target, _slotMoveDuration, onComplete);
    }

    private bool HasAvailableVisualSlot()
    {
        return _slotUnits.Count + _reservedMatchSlotIndices.Count < SlotCount;
    }

    private bool IsWaitingForSlotSpace()
    {
        return _isResolvingMatches || _internalAnimations > 0 || _reservedMatchSlotIndices.Count > 0;
    }

    private int GetVisualSlotIndex(int logicalIndex)
    {
        if (_reservedMatchSlotIndices.Count == 0)
        {
            return logicalIndex;
        }

        int availableIndex = 0;
        for (int slotIndex = 0; slotIndex < SlotCount; slotIndex++)
        {
            if (_reservedMatchSlotIndices.Contains(slotIndex))
            {
                continue;
            }

            if (availableIndex == logicalIndex)
            {
                return slotIndex;
            }

            availableIndex++;
        }

        return Mathf.Clamp(logicalIndex, 0, SlotCount - 1);
    }

    private IEnumerator RaiseUnitsMatchedAfterDelay(Vector3 position)
    {
        if (_matchVfxDelay > 0f)
        {
            yield return new WaitForSeconds(_matchVfxDelay);
        }

        GameplayEvents.RaiseMatchVfxRequested(position);
    }

    private void OnRepositionMoveComplete(ref int pendingMoves, Action onComplete)
    {
        pendingMoves--;
        EndInternalAnimation();

        if (pendingMoves <= 0)
        {
            onComplete?.Invoke();
        }
    }

    private void EvaluateFailCondition()
    {
        if (!ShouldTriggerFailNow())
        {
            CancelDelayedFailCheck();
            return;
        }

        if (_delayedFailCheckRoutine == null)
        {
            _delayedFailCheckRoutine = StartCoroutine(DelayedFailCheckRoutine());
        }
    }

    private IEnumerator DelayedFailCheckRoutine()
    {
        yield return new WaitForSeconds(_failCheckDelay);
        _delayedFailCheckRoutine = null;

        if (ShouldTriggerFailNow())
        {
            GameplayEvents.RaiseSlotsFullNoMatch();
        }
    }

    private bool ShouldTriggerFailNow()
    {
        if (HasPendingWork || _slotUnits.Count < SlotCount || GameplayEvents.HasUnitsInFlight())
        {
            return false;
        }

        if (HasMatchPossible())
        {
            return false;
        }

        foreach (Unit queued in _pendingSlotUnits)
        {
            if (queued != null && WouldCreateMatchIfInserted(queued))
            {
                return false;
            }
        }

        return true;
    }

    private void CancelDelayedFailCheck()
    {
        if (_delayedFailCheckRoutine == null)
        {
            return;
        }

        StopCoroutine(_delayedFailCheckRoutine);
        _delayedFailCheckRoutine = null;
    }

    private void BeginInternalAnimation(int count = 1)
    {
        _internalAnimations += count;
    }

    private void EndInternalAnimation(int count = 1)
    {
        _internalAnimations = Mathf.Max(0, _internalAnimations - count);
    }

    private void BeginSlotResolutionEvent()
    {
        if (_slotResolutionEventActive)
        {
            return;
        }

        _slotResolutionEventActive = true;
        GameplayEvents.RaiseSlotResolutionStarted();
    }

    private void EndSlotResolutionEvent()
    {
        if (!_slotResolutionEventActive)
        {
            return;
        }

        _slotResolutionEventActive = false;
        GameplayEvents.RaiseSlotResolutionCompleted();
    }

    private void LogSlot(string message)
    {
        if (_debugSlotLogs)
        {
            Debug.Log(message);
        }
    }
}
