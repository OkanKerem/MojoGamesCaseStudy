using System;
using UnityEngine;

[Serializable]
public class CellData
{
    public int x;
    public int y;
    public bool hasTile;
    public bool hasUnit;
    public bool hasBarrier;
    public bool hasBox;
    public UnitTypeData unitType;

    public void ClearUnit()
    {
        hasUnit = false;
        hasBox = false;
        unitType = null;
    }

    public void ClearBarrier()
    {
        hasBarrier = false;
    }

    public void ClearBox()
    {
        hasBox = false;
    }

    public void ClearTileAndContents()
    {
        hasTile = false;
        ClearUnit();
        ClearBarrier();
    }
}
