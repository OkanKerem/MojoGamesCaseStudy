using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private float cellSize = 1.1f;
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;

    private GridCell[,] _cells;
    private int _width;
    private int _height;
    private int _boundsMinX;
    private int _boundsMinY;

    public int Width => _width;
    public int Height => _height;
    public int BoundsMinX => _boundsMinX;
    public int BoundsMinY => _boundsMinY;
    public float CellSize => cellSize;
    public Vector3 GridOrigin => gridOrigin;

    public void InitializeFromLevel(LevelData levelData)
    {
        if (levelData == null)
        {
            Clear();
            return;
        }

        levelData.RecalculateBounds();
        if (!levelData.HasBounds)
        {
            Clear();
            return;
        }

        _boundsMinX = levelData.boundsMinX;
        _boundsMinY = levelData.boundsMinY;
        _width = levelData.width;
        _height = levelData.height;
        _cells = new GridCell[_width, _height];

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _cells[x, y] = new GridCell(_boundsMinX + x, _boundsMinY + y);
            }
        }

        ApplyLevelCells(levelData.cells);
    }

    public void ApplyLevelCells(List<CellData> levelCells)
    {
        if (_cells == null || levelCells == null)
        {
            return;
        }

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _cells[x, y].SetTile(false);
            }
        }

        for (int i = 0; i < levelCells.Count; i++)
        {
            CellData data = levelCells[i];
            if (data == null || !IsInside(data.x, data.y))
            {
                continue;
            }

            GridCell cell = GetCell(data.x, data.y);
            if (cell == null)
            {
                continue;
            }

            cell.SetTile(data.hasTile);
            cell.SetBarrier(data.hasBarrier);
        }
    }

    public void Clear()
    {
        _cells = null;
        _width = 0;
        _height = 0;
        _boundsMinX = 0;
        _boundsMinY = 0;
    }

    public bool IsInside(int x, int y)
    {
        if (_cells == null || _width <= 0 || _height <= 0)
        {
            return false;
        }

        return x >= _boundsMinX && y >= _boundsMinY && x < _boundsMinX + _width && y < _boundsMinY + _height;
    }

    public bool HasTile(int x, int y)
    {
        GridCell cell = GetCell(x, y);
        return cell != null && cell.HasTile;
    }

    public bool HasBarrier(int x, int y)
    {
        GridCell cell = GetCell(x, y);
        return cell != null && cell.HasBarrier;
    }

    public bool HasUnit(int x, int y)
    {
        GridCell cell = GetCell(x, y);
        return cell != null && cell.HasUnit;
    }

    public GridCell GetCell(int x, int y)
    {
        if (!IsInside(x, y))
        {
            return null;
        }

        return _cells[x - _boundsMinX, y - _boundsMinY];
    }

    public Unit GetUnitAt(int x, int y)
    {
        GridCell cell = GetCell(x, y);
        return cell != null ? cell.OccupyingUnit : null;
    }

    public bool CanPlaceUnit(int x, int y)
    {
        GridCell cell = GetCell(x, y);
        return cell != null && cell.HasTile && !cell.HasBarrier && !cell.HasUnit;
    }

    public void PlaceUnit(Unit unit, int x, int y, Transform parent)
    {
        GridCell cell = GetCell(x, y);
        if (cell == null || unit == null || !CanPlaceUnit(x, y))
        {
            return;
        }

        cell.SetUnit(unit);
        unit.SetGridCoordinate(new Vector2Int(x, y));
        unit.transform.SetParent(parent, true);
        unit.transform.position = GridToWorldPosition(x, y);
    }

    public void ClearUnit(int x, int y)
    {
        GetCell(x, y)?.ClearUnit();
    }

    public Vector3 GridToWorldPosition(int x, int y)
    {
        return gridOrigin + new Vector3(x * cellSize, 0f, y * cellSize);
    }

    public IEnumerable<Unit> GetAllUnitsOnGrid()
    {
        if (_cells == null)
        {
            yield break;
        }

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Unit unit = _cells[x, y].OccupyingUnit;
                if (unit != null && unit.State != UnitState.Pooled && unit.State != UnitState.Matched)
                {
                    yield return unit;
                }
            }
        }
    }

}
