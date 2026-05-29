using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class LevelValidator
{
    public struct ValidationResult
    {
        public bool IsValid;
        public List<string> Errors;

        public string GetMessage()
        {
            if (Errors == null || Errors.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Level is invalid:");
            for (int i = 0; i < Errors.Count; i++)
            {
                builder.Append("- ");
                builder.AppendLine(Errors[i]);
            }

            return builder.ToString().TrimEnd();
        }
    }

    public static ValidationResult Validate(LevelData level)
    {
        var result = new ValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };

        if (level == null)
        {
            result.IsValid = false;
            result.Errors.Add("Level data is null.");
            return result;
        }

        level.RecalculateBounds();

        int tileCount = 0;
        int unitCount = 0;
        var unitTypeCounts = new Dictionary<UnitTypeData, int>();

        if (level.cells == null || level.cells.Count == 0)
        {
            result.Errors.Add("Level has no cells. Paint at least one tile in the level editor.");
        }
        else
        {
            var seen = new HashSet<(int, int)>();
            for (int i = 0; i < level.cells.Count; i++)
            {
                CellData cell = level.cells[i];
                if (cell == null)
                {
                    continue;
                }

                if (!LevelData.HasContent(cell))
                {
                    continue;
                }

                if (!seen.Add((cell.x, cell.y)))
                {
                    result.Errors.Add($"Duplicate cell at ({cell.x}, {cell.y}).");
                    continue;
                }

                if (cell.hasTile)
                {
                    tileCount++;
                }

                if (cell.hasUnit && !cell.hasTile)
                {
                    result.Errors.Add($"Cell ({cell.x}, {cell.y}) has a unit but no tile.");
                }

                if (cell.hasBarrier && !cell.hasTile)
                {
                    result.Errors.Add($"Cell ({cell.x}, {cell.y}) has a barrier but no tile.");
                }

                if (cell.hasUnit && cell.hasBarrier)
                {
                    result.Errors.Add($"Cell ({cell.x}, {cell.y}) has both a unit and a barrier.");
                }

                if (cell.hasBox && !cell.hasUnit)
                {
                    result.Errors.Add($"Cell ({cell.x}, {cell.y}) has a box but no unit.");
                }

                if (cell.hasBox && !cell.hasTile)
                {
                    result.Errors.Add($"Cell ({cell.x}, {cell.y}) has a box but no tile.");
                }

                if (cell.hasBox && cell.hasBarrier)
                {
                    result.Errors.Add($"Cell ({cell.x}, {cell.y}) has both a box and a barrier.");
                }

                if (cell.hasBox && cell.unitType == null)
                {
                    result.Errors.Add($"Cell ({cell.x}, {cell.y}) has a box but no UnitTypeData.");
                }

                if (cell.hasUnit)
                {
                    unitCount++;
                    if (cell.unitType == null)
                    {
                        result.Errors.Add($"Cell ({cell.x}, {cell.y}) has a unit without UnitTypeData.");
                    }
                    else
                    {
                        unitTypeCounts.TryGetValue(cell.unitType, out int count);
                        unitTypeCounts[cell.unitType] = count + 1;
                    }
                }
            }
        }

        if (!level.HasBounds)
        {
            result.Errors.Add("Level has no tiles. Paint tiles in the scene editor to define the level shape.");
        }
        else if (tileCount == 0)
        {
            result.Errors.Add("Level must have at least one tile.");
        }

        if (unitCount == 0)
        {
            result.Errors.Add("Level must have at least one unit.");
        }

        foreach (KeyValuePair<UnitTypeData, int> pair in unitTypeCounts)
        {
            if (pair.Key == null)
            {
                continue;
            }

            if (pair.Value % 3 != 0)
            {
                string typeName = string.IsNullOrEmpty(pair.Key.DisplayName) ? pair.Key.Id : pair.Key.DisplayName;
                result.Errors.Add($"Unit type {typeName} has {pair.Value} units. Count must be divisible by 3.");
            }
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }
}
