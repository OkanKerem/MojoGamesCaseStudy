using System.Collections.Generic;
using UnityEngine;

public class UnitPool : MonoBehaviour
{
    [SerializeField] private Unit unitPrefab;
    [SerializeField] private Transform poolRoot;
    [Tooltip("Parent of your 16 scene units. If empty, uses Pool Root.")]
    [SerializeField] private Transform sceneUnitsParent;
    [Tooltip("Register existing Unit objects from the scene at startup.")]
    [SerializeField] private bool collectSceneUnits = true;
    [Tooltip("Never Instantiate on startup — only reuse scene units.")]
    [SerializeField] private bool sceneInstancesOnly = true;
    [Tooltip("Expected number of pre-placed units in the scene.")]
    [SerializeField] private int defaultCapacity = 16;
    [Tooltip("Allow creating extra units at runtime only if the pool runs out.")]
    [SerializeField] private bool allowRuntimeExpand = false;

    private readonly Queue<Unit> _available = new Queue<Unit>();
    private readonly HashSet<Unit> _availableSet = new HashSet<Unit>();
    private readonly HashSet<Unit> _active = new HashSet<Unit>();
    private readonly HashSet<Unit> _allInstances = new HashSet<Unit>();
    private bool _initialized;

    public int AvailableCount => _available.Count;
    public int ActiveCount => _active.Count;
    public int TotalCount => _allInstances.Count;

    public void Initialize()
    {
        if (poolRoot == null)
        {
            poolRoot = transform;
        }

        if (collectSceneUnits)
        {
            CollectSceneUnits();
        }

        if (sceneInstancesOnly)
        {
            if (_allInstances.Count == 0)
            {
                Debug.LogError(
                    "UnitPool: No scene units found. Place 16 Unit objects under Pool Root " +
                    "(or assign Scene Units Parent) and enable Collect Scene Units.");
            }
            else if (_allInstances.Count < defaultCapacity)
            {
                Debug.LogWarning(
                    $"UnitPool: Found {_allInstances.Count} scene units, expected {defaultCapacity}. " +
                    "Some levels may not have enough units.");
            }
        }
        else
        {
            EnsureCapacity(defaultCapacity);
        }

        _initialized = true;
    }

    public void PrepareForLevel(LevelData levelData)
    {
        if (!_initialized || levelData == null)
        {
            return;
        }

        int levelUnits = CountUnitsInLevel(levelData);
        int required = levelUnits + SlotManager.SlotCount;
        if (TotalCount < required)
        {
            Debug.LogWarning(
                $"UnitPool: Level needs up to {required} units but only {TotalCount} are in the pool.");
        }
    }

    public Unit GetUnit()
    {
        if (!_initialized)
        {
            Debug.LogError("UnitPool: Initialize must be called before GetUnit.");
            return null;
        }

        if (_available.Count == 0)
        {
            if (allowRuntimeExpand && unitPrefab != null)
            {
                Debug.LogWarning("UnitPool: Pool exhausted, creating one extra instance.");
                RegisterCreatedUnit(CreateUnitInstance());
            }
            else
            {
                Debug.LogError("UnitPool: No available units. All scene instances are in use.");
                return null;
            }
        }

        Unit unit = _available.Dequeue();
        _availableSet.Remove(unit);
        unit.PrepareFromPool();
        _active.Add(unit);
        return unit;
    }

    public void ReturnUnit(Unit unit)
    {
        if (unit == null || !_allInstances.Contains(unit))
        {
            return;
        }

        _active.Remove(unit);

        if (_availableSet.Contains(unit))
        {
            return;
        }

        unit.ResetForPool();
        unit.transform.SetParent(poolRoot, false);
        unit.gameObject.SetActive(false);
        _availableSet.Add(unit);
        _available.Enqueue(unit);
    }

    public void ReturnAll(IEnumerable<Unit> units)
    {
        if (units == null)
        {
            return;
        }

        var unitsToReturn = new List<Unit>();
        foreach (Unit unit in units)
        {
            if (unit != null && _active.Contains(unit))
            {
                unitsToReturn.Add(unit);
            }
        }

        for (int i = 0; i < unitsToReturn.Count; i++)
        {
            ReturnUnit(unitsToReturn[i]);
        }
    }

    public void ReturnAllActiveUnits()
    {
        ReturnAll(_active);
    }

    private void CollectSceneUnits()
    {
        Transform source = sceneUnitsParent != null ? sceneUnitsParent : poolRoot;
        if (source == null)
        {
            return;
        }

        Unit[] sceneUnits = source.GetComponentsInChildren<Unit>(true);
        for (int i = 0; i < sceneUnits.Length; i++)
        {
            RegisterSceneUnit(sceneUnits[i]);
        }
    }

    private void RegisterSceneUnit(Unit unit)
    {
        if (unit == null || !_allInstances.Add(unit))
        {
            return;
        }

        unit.ResetForPool();
        unit.transform.SetParent(poolRoot, false);
        unit.gameObject.SetActive(false);
        _availableSet.Add(unit);
        _available.Enqueue(unit);
    }

    private void EnsureCapacity(int targetCount)
    {
        if (unitPrefab == null)
        {
            Debug.LogError("UnitPool: Unit prefab is not assigned.");
            return;
        }

        while (_allInstances.Count < targetCount)
        {
            RegisterCreatedUnit(CreateUnitInstance());
        }
    }

    private void RegisterCreatedUnit(Unit unit)
    {
        if (unit == null || !_allInstances.Add(unit))
        {
            return;
        }

        unit.ResetForPool();
        unit.transform.SetParent(poolRoot, false);
        unit.gameObject.SetActive(false);
        _availableSet.Add(unit);
        _available.Enqueue(unit);
    }

    private Unit CreateUnitInstance()
    {
        Unit unit = Instantiate(unitPrefab, poolRoot);
        unit.name = $"{unitPrefab.name}_{_allInstances.Count}";
        return unit;
    }

    private static int CountUnitsInLevel(LevelData levelData)
    {
        int count = 0;
        foreach (CellData cell in levelData.GetContentCells())
        {
            if (cell != null && cell.hasUnit)
            {
                count++;
            }
        }

        return count;
    }
}
