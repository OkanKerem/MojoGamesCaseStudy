using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class LevelManager : MonoBehaviour
{
    private static readonly Vector2Int[] OrthogonalDirections =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    [FormerlySerializedAs("gridManager")]
    [SerializeField] private GridManager _gridManager;
    [FormerlySerializedAs("unitPool")]
    [SerializeField] private UnitPool _unitPool;
    [FormerlySerializedAs("unitRoot")]
    [SerializeField] private Transform _unitRoot;
    [FormerlySerializedAs("tileRoot")]
    [SerializeField] private Transform _tileRoot;
    [FormerlySerializedAs("barrierRoot")]
    [SerializeField] private Transform _barrierRoot;
    [FormerlySerializedAs("boxRoot")]
    [SerializeField] private Transform _boxRoot;
    [FormerlySerializedAs("tilePrefab")]
    [SerializeField] private GameObject _tilePrefab;
    [FormerlySerializedAs("barrierPrefab")]
    [SerializeField] private GameObject _barrierPrefab;
    [FormerlySerializedAs("boxPrefab")]
    [SerializeField] private GameObject _boxPrefab;
    [FormerlySerializedAs("tileColor")]
    [SerializeField] private Color _tileColor = new Color(0.35f, 0.38f, 0.42f, 1f);
    [FormerlySerializedAs("barrierColor")]
    [SerializeField] private Color _barrierColor = new Color(0.12f, 0.12f, 0.14f, 1f);
    [FormerlySerializedAs("barrierHeight")]
    [SerializeField] private float _barrierHeight = 1.1f;
    [FormerlySerializedAs("boxSpawnY")]
    [SerializeField] private float _boxSpawnY = 0f;
    [FormerlySerializedAs("gridStepMoveDuration")]
    [SerializeField] private float _gridStepMoveDuration = 0.12f;
    [FormerlySerializedAs("debugInputLogs")]
    [SerializeField] private bool _debugInputLogs;

    private LevelData _currentLevel;
    private int _remainingUnitCount;
    private VisualObjectPool _tilePool;
    private VisualObjectPool _barrierPool;
    private VisualObjectPool _boxPool;
    private readonly Dictionary<Unit, BoxView> _boxByUnit = new Dictionary<Unit, BoxView>();

    public int RemainingUnitCount => _remainingUnitCount;
    public int RemainingGridUnits => _remainingUnitCount;
    public bool IsGridEmpty => _remainingUnitCount <= 0;
    public LevelData CurrentLevel => _currentLevel;
    public GridManager Grid => _gridManager;

    public void Initialize()
    {
        if (_unitRoot == null)
        {
            _unitRoot = transform;
        }

        if (_tileRoot == null)
        {
            GameObject tileRootObject = new GameObject("TileRoot");
            tileRootObject.transform.SetParent(transform, false);
            _tileRoot = tileRootObject.transform;
        }

        if (_barrierRoot == null)
        {
            GameObject barrierRootObject = new GameObject("BarrierRoot");
            barrierRootObject.transform.SetParent(transform, false);
            _barrierRoot = barrierRootObject.transform;
        }

        if (_boxRoot == null)
        {
            GameObject boxRootObject = new GameObject("BoxRoot");
            boxRootObject.transform.SetParent(transform, false);
            _boxRoot = boxRootObject.transform;
        }

        _tilePool = new VisualObjectPool(_tilePrefab, _tileRoot, "Tile");
        _barrierPool = new VisualObjectPool(_barrierPrefab, _barrierRoot, "Barrier");
        _boxPool = new VisualObjectPool(_boxPrefab, _boxRoot, "Box");
    }

    public void LoadLevel(LevelData levelData)
    {
        ClearLevel();
        _currentLevel = levelData;

        if (levelData == null)
        {
            Debug.LogError("LevelManager: Level data is null.");
            return;
        }

        levelData.RecalculateBounds();
        if (!levelData.HasBounds)
        {
            Debug.LogWarning("LevelManager: Level has no tiles.");
            return;
        }

        if (_unitPool != null)
        {
            _unitPool.PrepareForLevel(levelData);
        }

        _gridManager.InitializeFromLevel(levelData);
        BuildTiles(levelData);
        BuildBarriers(levelData);
        BuildBoxes(levelData);
        SpawnUnits(levelData);
        RecalculateSelectableUnits(true);
    }

    private void BuildTiles(LevelData levelData)
    {
        _tilePool.ReturnAll();
        Vector3 tileScale = GetCellScale(0.95f);

        foreach (CellData cellData in levelData.GetContentCells())
        {
            if (!cellData.hasTile)
            {
                continue;
            }

            GameObject tile = _tilePool.Get(_gridManager.GridToWorldPosition(cellData.x, cellData.y), tileScale);
            TileView tileView = tile.GetComponent<TileView>();
            if (tileView != null)
            {
                tileView.SetColor(_tileColor);
            }
            else
            {
                Renderer renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = _tileColor;
                }
            }
        }
    }

    private void BuildBarriers(LevelData levelData)
    {
        _barrierPool.ReturnAll();
        Vector3 tileScale = GetCellScale(0.92f);
        Vector3 barrierScale = new Vector3(tileScale.x, _barrierHeight, tileScale.z);

        foreach (CellData cellData in levelData.GetContentCells())
        {
            if (!cellData.hasBarrier)
            {
                continue;
            }

            if (!cellData.hasTile)
            {
                Debug.LogWarning($"LevelManager: Barrier at ({cellData.x}, {cellData.y}) has no tile. Skipping.");
                continue;
            }

            Vector3 position = _gridManager.GridToWorldPosition(cellData.x, cellData.y);
            position.y = 0f;
            GameObject barrier = _barrierPool.Get(position, barrierScale);
            BarrierView barrierView = barrier.GetComponent<BarrierView>();
            if (barrierView != null)
            {
                barrierView.SetColor(_barrierColor);
            }
            else
            {
                Renderer renderer = barrier.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = _barrierColor;
                }
            }
        }
    }

    private void BuildBoxes(LevelData levelData)
    {
        _boxPool.ReturnAll();
        _boxByUnit.Clear();
    }

    private void SpawnUnits(LevelData levelData)
    {
        _remainingUnitCount = 0;

        foreach (CellData cellData in levelData.GetContentCells())
        {
            if (!cellData.hasUnit)
            {
                continue;
            }

            if (!cellData.hasTile)
            {
                Debug.LogWarning($"LevelManager: Unit at ({cellData.x}, {cellData.y}) has no tile. Skipping.");
                continue;
            }

            if (cellData.hasBarrier)
            {
                Debug.LogWarning($"LevelManager: Unit at ({cellData.x}, {cellData.y}) overlaps a barrier. Skipping.");
                continue;
            }

            if (cellData.unitType == null)
            {
                Debug.LogWarning($"LevelManager: Unit at ({cellData.x}, {cellData.y}) has no UnitTypeData. Skipping.");
                continue;
            }

            if (!_gridManager.CanPlaceUnit(cellData.x, cellData.y))
            {
                continue;
            }

            Unit unit = _unitPool.GetUnit();
            if (unit == null)
            {
                continue;
            }

            unit.Configure(cellData.unitType, new Vector2Int(cellData.x, cellData.y));
            _gridManager.PlaceUnit(unit, cellData.x, cellData.y, _unitRoot);

            if (cellData.hasBox)
            {
                unit.HideInsideBox();
                SpawnBoxForUnit(unit, cellData.x, cellData.y);
            }

            _remainingUnitCount++;
        }
    }

    public void RequestSelectUnit(Unit unit, Action onComplete = null)
    {
        if (unit == null)
        {
            LogRejectedTap("unit is null");
            onComplete?.Invoke();
            return;
        }

        if (unit.IsBoxed)
        {
            LogRejectedTap("unit is boxed");
            onComplete?.Invoke();
            return;
        }

        if (unit.State != UnitState.OnGrid)
        {
            LogRejectedTap($"unit state is {unit.State}");
            onComplete?.Invoke();
            return;
        }

        if (!unit.IsSelectable)
        {
            LogRejectedTap("unit is not selectable");
            onComplete?.Invoke();
            return;
        }

        if (!GameplayEvents.CanAcceptIncomingUnit(unit))
        {
            LogRejectedTap("slot is full and this move cannot match");
            onComplete?.Invoke();
            return;
        }

        if (!CanUnitReachExit(unit))
        {
            LogRejectedTap("no path to exit");
            onComplete?.Invoke();
            return;
        }

        List<Vector2Int> exitPath = FindShortestExitPath(unit, out Vector2Int exitDirection);
        if (exitDirection == Vector2Int.zero && exitPath.Count == 0 && !HasDirectExitNeighbor(unit))
        {
            LogRejectedTap("path result invalid");
            onComplete?.Invoke();
            return;
        }

        Vector2Int coord = unit.GridCoordinate;
        _gridManager.ClearUnit(coord.x, coord.y);
        _remainingUnitCount = Mathf.Max(0, _remainingUnitCount - 1);
        if (_remainingUnitCount <= 0)
        {
            GameplayEvents.RaiseGridCleared();
        }

        GameplayEvents.RaiseUnitSelected();

        bool brokeAnyBox = BreakAdjacentBoxes(coord);
        if (brokeAnyBox)
        {
            GameplayEvents.RaiseBoxesBroken();
        }

        RecalculateSelectableUnits(false);
        unit.SetState(UnitState.MovingToExit);

        if (_debugInputLogs)
        {
            Debug.Log("Move accepted");
        }

        StartCoroutine(MoveUnitAlongExitPathThenToSlot(unit, exitPath, exitDirection, coord, onComplete));
    }

    public void RecalculateSelectableUnits(bool isInitialSetup = false)
    {
        List<Unit> units = new List<Unit>(_gridManager.GetAllUnitsOnGrid());
        for (int i = 0; i < units.Count; i++)
        {
            Unit unit = units[i];
            if (unit == null)
            {
                continue;
            }

            if (unit.IsBoxed)
            {
                unit.SetSelectable(false, isInitialSetup);
                continue;
            }

            bool selectable = CanUnitReachExit(unit);
            unit.SetSelectable(selectable, isInitialSetup);
        }
    }

    public void ClearLevel()
    {
        GameplayEvents.RaiseSlotClearRequested();

        if (_gridManager != null)
        {
            _unitPool.ReturnAll(_gridManager.GetAllUnitsOnGrid());
            _gridManager.Clear();
        }

        _tilePool?.ReturnAll();
        _barrierPool?.ReturnAll();
        _boxPool?.ReturnAll();
        _boxByUnit.Clear();
        _remainingUnitCount = 0;
        _currentLevel = null;
    }

    private bool CanUnitReachExit(Unit unit)
    {
        return GridPathUtility.CanUnitReachExit(_gridManager, unit);
    }

    private List<Vector2Int> FindShortestExitPath(Unit unit, out Vector2Int exitDirection)
    {
        return GridPathUtility.FindShortestExitPath(_gridManager, unit, out exitDirection);
    }

    private bool HasDirectExitNeighbor(Unit unit)
    {
        Vector2Int unitPosition = unit.GridCoordinate;
        return GridPathUtility.IsOutsideGrid(_gridManager, unitPosition + Vector2Int.right)
            || GridPathUtility.IsOutsideGrid(_gridManager, unitPosition + Vector2Int.left)
            || GridPathUtility.IsOutsideGrid(_gridManager, unitPosition + Vector2Int.up)
            || GridPathUtility.IsOutsideGrid(_gridManager, unitPosition + Vector2Int.down);
    }

    private IEnumerator MoveUnitAlongExitPathThenToSlot(
        Unit unit,
        List<Vector2Int> path,
        Vector2Int exitDirection,
        Vector2Int originalCoord,
        Action onComplete)
    {
        yield return MoveUnitAlongExitPath(unit, path, exitDirection);

        unit.SetState(UnitState.MovingToSlot);
        bool slotPlacementFinished = false;
        bool slotSuccess = false;

        GameplayEvents.RaiseSlotInsertionRequested(unit, success =>
        {
            slotSuccess = success;
            slotPlacementFinished = true;
        });

        while (!slotPlacementFinished)
        {
            yield return null;
        }

        if (!slotSuccess)
        {
            _gridManager.PlaceUnit(unit, originalCoord.x, originalCoord.y, _unitRoot);
            _remainingUnitCount++;
            unit.SetState(UnitState.OnGrid);
            RecalculateSelectableUnits(false);
        }

        RecalculateSelectableUnits(false);
        onComplete?.Invoke();
    }

    private bool BreakAdjacentBoxes(Vector2Int clickedUnitGridPosition)
    {
        bool brokeAny = false;

        for (int i = 0; i < OrthogonalDirections.Length; i++)
        {
            Vector2Int neighbor = clickedUnitGridPosition + OrthogonalDirections[i];
            if (TryBreakBoxAtPosition(neighbor))
            {
                brokeAny = true;
            }
        }

        return brokeAny;
    }

    private IEnumerator MoveUnitAlongExitPath(Unit unit, List<Vector2Int> path, Vector2Int exitDirection)
    {
        unit.PlayRunAnimation();

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int cell = path[i];
            Vector3 target = _gridManager.GridToWorldPosition(cell.x, cell.y);
            yield return MoveUnitToPosition(unit, target);
        }

        Vector3 lastPosition;
        if (path.Count > 0)
        {
            Vector2Int lastCell = path[path.Count - 1];
            lastPosition = _gridManager.GridToWorldPosition(lastCell.x, lastCell.y);
        }
        else
        {
            lastPosition = unit.transform.position;
        }

        Vector3 outsideTarget = lastPosition + GridPathUtility.GridDirectionToWorldOffset(exitDirection, _gridManager.CellSize);
        yield return MoveUnitToPosition(unit, outsideTarget);
    }

    private IEnumerator MoveUnitToPosition(Unit unit, Vector3 target)
    {
        bool done = false;
        UnitAnimationHelper.MoveTo(this, unit, target, _gridStepMoveDuration, () => done = true);

        while (!done)
        {
            yield return null;
        }

        unit.transform.position = target;
    }

    private Vector3 GetCellScale(float multiplier)
    {
        float size = _gridManager.CellSize * multiplier;
        return new Vector3(size, 0.15f, size);
    }

    private void SpawnBoxForUnit(Unit unit, int x, int y)
    {
        if (_boxPool == null || unit == null)
        {
            return;
        }

        float size = _gridManager.CellSize * 0.9f;
        Vector3 scale = new Vector3(size, size, size);
        Vector3 position = _gridManager.GridToWorldPosition(x, y);
        position.y = _boxSpawnY;
        GameObject boxObject = _boxPool.Get(position, scale);
        Vector3 finalPosition = boxObject.transform.position;
        finalPosition.y = _boxSpawnY;
        boxObject.transform.position = finalPosition;
        BoxView boxView = boxObject.GetComponent<BoxView>();
        if (boxView == null)
        {
            boxView = boxObject.AddComponent<BoxView>();
        }

        boxView.Initialize(new Vector2Int(x, y), unit);
        _boxByUnit[unit] = boxView;
    }

    private bool TryBreakBoxForUnit(Unit unit)
    {
        if (unit == null || !_boxByUnit.TryGetValue(unit, out BoxView boxView) || boxView == null)
        {
            return false;
        }

        if (!boxView.BreakBox())
        {
            if (_debugInputLogs)
            {
                Debug.Log("Rejected box break: already broken");
            }
            return false;
        }

        unit.RevealFromBox();
        _boxByUnit.Remove(unit);
        return true;
    }

    private bool TryBreakBoxAtPosition(Vector2Int gridPosition)
    {
        Unit hiddenUnit = _gridManager.GetUnitAt(gridPosition.x, gridPosition.y);
        if (hiddenUnit == null || !hiddenUnit.IsBoxed)
        {
            return false;
        }

        return TryBreakBoxForUnit(hiddenUnit);
    }

    private void LogRejectedTap(string reason)
    {
        if (!_debugInputLogs)
        {
            return;
        }

        Debug.Log($"Rejected tap: {reason}");
    }
}
