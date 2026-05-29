using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Puzzle/Level Data")]
public class LevelData : ScriptableObject
{
    public int levelNumber = 1;
    [Tooltip("Auto-calculated from placed tiles/units/barriers. Do not set manually.")]
    public int boundsMinX;
    [Tooltip("Auto-calculated from placed tiles/units/barriers. Do not set manually.")]
    public int boundsMinY;
    [Tooltip("Auto-calculated grid width (inclusive bounds).")]
    public int width;
    [Tooltip("Auto-calculated grid height (inclusive bounds).")]
    public int height;
    public List<CellData> cells = new List<CellData>();

    public bool HasBounds => width > 0 && height > 0;

    public int BoundsMaxX => boundsMinX + width - 1;
    public int BoundsMaxY => boundsMinY + height - 1;

    public int BoundsCenterX => HasBounds ? (boundsMinX + BoundsMaxX) / 2 : 0;
    public int BoundsCenterY => HasBounds ? (boundsMinY + BoundsMaxY) / 2 : 0;
    public bool IsCenteredAtOrigin => !HasBounds || (BoundsCenterX == 0 && BoundsCenterY == 0);

    public CellData GetCell(int x, int y)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            CellData cell = cells[i];
            if (cell != null && cell.x == x && cell.y == y)
            {
                return cell;
            }
        }

        return null;
    }

    public CellData GetOrCreateCell(int x, int y)
    {
        CellData cell = GetCell(x, y);
        if (cell != null)
        {
            return cell;
        }

        cell = new CellData { x = x, y = y };
        cells.Add(cell);
        return cell;
    }

    public static bool HasContent(CellData cell)
    {
        return cell != null && (cell.hasTile || cell.hasUnit || cell.hasBarrier || cell.hasBox);
    }

    public void RecalculateBounds()
    {
        PruneEmptyCells();

        boundsMinX = 0;
        boundsMinY = 0;
        width = 0;
        height = 0;

        if (cells == null || cells.Count == 0)
        {
            return;
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        bool hasAny = false;

        for (int i = 0; i < cells.Count; i++)
        {
            CellData cell = cells[i];
            if (!HasContent(cell))
            {
                continue;
            }

            hasAny = true;
            minX = Mathf.Min(minX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxX = Mathf.Max(maxX, cell.x);
            maxY = Mathf.Max(maxY, cell.y);
        }

        if (!hasAny)
        {
            return;
        }

        boundsMinX = minX;
        boundsMinY = minY;
        width = maxX - minX + 1;
        height = maxY - minY + 1;
    }

    public void PruneEmptyCells()
    {
        if (cells == null)
        {
            return;
        }

        for (int i = cells.Count - 1; i >= 0; i--)
        {
            if (!HasContent(cells[i]))
            {
                cells.RemoveAt(i);
            }
        }
    }

    public void NormalizeBoundsToOrigin()
    {
        RecalculateBounds();
        if (!HasBounds)
        {
            return;
        }

        if (boundsMinX == 0 && boundsMinY == 0)
        {
            return;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            CellData cell = cells[i];
            if (cell == null)
            {
                continue;
            }

            cell.x -= boundsMinX;
            cell.y -= boundsMinY;
        }

        RecalculateBounds();
    }

    public void CenterBoundsAtOrigin()
    {
        RecalculateBounds();
        if (!HasBounds)
        {
            return;
        }

        int centerX = BoundsCenterX;
        int centerY = BoundsCenterY;

        if (centerX == 0 && centerY == 0)
        {
            return;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            CellData cell = cells[i];
            if (cell == null)
            {
                continue;
            }

            cell.x -= centerX;
            cell.y -= centerY;
        }

        RecalculateBounds();
    }

    public void FillAllTilesInBounds()
    {
        RecalculateBounds();
        if (!HasBounds)
        {
            return;
        }

        for (int x = boundsMinX; x <= BoundsMaxX; x++)
        {
            for (int y = boundsMinY; y <= BoundsMaxY; y++)
            {
                GetOrCreateCell(x, y).hasTile = true;
            }
        }

        RecalculateBounds();
    }

    public void ClearAllUnits()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i]?.ClearUnit();
        }

        PruneEmptyCells();
        RecalculateBounds();
    }

    public void ClearAllBarriers()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i]?.ClearBarrier();
        }

        PruneEmptyCells();
        RecalculateBounds();
    }

    public void ClearLevelContents()
    {
        cells.Clear();
        RecalculateBounds();
    }

    public IEnumerable<CellData> GetContentCells()
    {
        if (cells == null)
        {
            yield break;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            CellData cell = cells[i];
            if (HasContent(cell))
            {
                yield return cell;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (cells == null)
        {
            return;
        }

        var seen = new HashSet<(int, int)>();
        for (int i = 0; i < cells.Count; i++)
        {
            CellData cell = cells[i];
            if (cell == null)
            {
                continue;
            }

            if (cell.hasUnit && !cell.hasTile)
            {
                cell.hasTile = true;
            }

            if (cell.hasBox && !cell.hasTile)
            {
                cell.hasTile = true;
            }

            if (cell.hasBarrier && !cell.hasTile)
            {
                cell.hasTile = true;
            }

            if (cell.hasUnit && cell.hasBarrier)
            {
                cell.hasBarrier = false;
            }

            if (cell.hasBox && !cell.hasUnit)
            {
                cell.hasUnit = true;
            }

            if (cell.hasBox && cell.hasBarrier)
            {
                cell.hasBarrier = false;
            }

            if (!cell.hasBox && cell.hasUnit && cell.unitType == null)
            {
                // leave for validator, but keep data consistent by allowing null only transiently
            }

            if (!cell.hasUnit)
            {
                cell.hasBox = false;
                cell.unitType = null;
            }

            var key = (cell.x, cell.y);
            if (!seen.Add(key))
            {
                Debug.LogWarning($"LevelData '{name}': duplicate cell at ({cell.x}, {cell.y}).", this);
            }
        }

        RecalculateBounds();
    }
#endif
}
