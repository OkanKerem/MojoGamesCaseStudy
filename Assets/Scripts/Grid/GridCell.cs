using UnityEngine;

public class GridCell
{
    public Vector2Int Coordinate { get; }
    public bool HasTile { get; private set; }
    public bool HasBarrier { get; private set; }
    public Unit OccupyingUnit { get; private set; }

    public bool HasUnit => OccupyingUnit != null;
    public bool BlocksSide => HasBarrier || HasUnit;

    public GridCell(int x, int y)
    {
        Coordinate = new Vector2Int(x, y);
    }

    public void SetTile(bool hasTile)
    {
        HasTile = hasTile;
        if (!hasTile)
        {
            HasBarrier = false;
            OccupyingUnit = null;
        }
    }

    public void SetBarrier(bool hasBarrier)
    {
        HasBarrier = hasBarrier && HasTile;
        if (HasBarrier)
        {
            OccupyingUnit = null;
        }
    }

    public void SetUnit(Unit unit)
    {
        if (!HasTile || HasBarrier)
        {
            return;
        }

        OccupyingUnit = unit;
    }

    public void ClearUnit()
    {
        OccupyingUnit = null;
    }
}
