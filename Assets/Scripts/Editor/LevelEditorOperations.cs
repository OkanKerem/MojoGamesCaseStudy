#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public enum LevelEditTool
{
    PaintTile,
    EraseTile,
    PaintUnit,
    PaintBoxedUnit,
    EraseUnit,
    PaintBarrier,
    EraseBarrier,
    EraseBox
}

public static class LevelEditorOperations
{
    public static readonly Color EmptyColor = new Color(0.12f, 0.12f, 0.12f, 0.35f);
    public static readonly Color TileColor = new Color(0.35f, 0.38f, 0.42f, 0.85f);
    public static readonly Color UnitFallbackColor = new Color(0.2f, 0.55f, 0.95f, 0.95f);
    public static readonly Color BarrierColor = new Color(0.55f, 0.15f, 0.15f, 0.95f);
    public static readonly Color BoxColor = new Color(0.45f, 0.3f, 0.18f, 0.95f);

    public static Color GetCellColor(CellData cell)
    {
        if (cell == null)
        {
            return EmptyColor;
        }

        if (cell.hasBox)
        {
            return BoxColor;
        }

        if (cell.hasUnit)
        {
            if (cell.unitType != null)
            {
                return cell.unitType.VisualColor;
            }

            return UnitFallbackColor;
        }

        if (cell.hasBarrier)
        {
            return BarrierColor;
        }

        if (cell.hasTile)
        {
            return TileColor;
        }

        return EmptyColor;
    }

    public static string GetCellLabel(CellData cell)
    {
        if (cell == null)
        {
            return string.Empty;
        }

        if (cell.hasBox)
        {
            if (cell.unitType != null && !string.IsNullOrEmpty(cell.unitType.DisplayName))
            {
                return $"B:{cell.unitType.DisplayName}";
            }

            return "Box";
        }

        if (cell.hasUnit)
        {
            if (cell.unitType != null)
            {
                if (!string.IsNullOrEmpty(cell.unitType.DisplayName))
                {
                    return cell.unitType.DisplayName;
                }

                if (!string.IsNullOrEmpty(cell.unitType.Id))
                {
                    return cell.unitType.Id;
                }
            }

            return "Unit";
        }

        if (cell.hasBarrier)
        {
            return "B";
        }

        return string.Empty;
    }

    public static void ApplyTool(LevelData level, CellData cell, LevelEditTool tool, UnitTypeData unitType)
    {
        if (level == null || cell == null)
        {
            return;
        }

        Undo.RecordObject(level, "Edit Level Cell");

        switch (tool)
        {
            case LevelEditTool.PaintTile:
                cell.hasTile = true;
                break;
            case LevelEditTool.EraseTile:
                cell.ClearTileAndContents();
                break;
            case LevelEditTool.PaintUnit:
                if (!cell.hasTile)
                {
                    cell.hasTile = true;
                }

                if (cell.hasBarrier)
                {
                    cell.ClearBarrier();
                }

                if (unitType != null)
                {
                    cell.hasUnit = true;
                    cell.hasBox = false;
                    cell.unitType = unitType;
                }
                break;
            case LevelEditTool.PaintBoxedUnit:
                if (!cell.hasTile)
                {
                    cell.hasTile = true;
                }

                if (cell.hasBarrier)
                {
                    cell.ClearBarrier();
                }

                if (unitType != null)
                {
                    cell.hasUnit = true;
                    cell.hasBox = true;
                    cell.unitType = unitType;
                }
                break;
            case LevelEditTool.EraseUnit:
                cell.ClearUnit();
                break;
            case LevelEditTool.PaintBarrier:
                if (!cell.hasTile)
                {
                    cell.hasTile = true;
                }

                if (cell.hasUnit)
                {
                    cell.ClearUnit();
                }

                cell.hasBarrier = true;
                break;
            case LevelEditTool.EraseBarrier:
                cell.ClearBarrier();
                break;
            case LevelEditTool.EraseBox:
                cell.ClearBox();
                break;
        }

        level.PruneEmptyCells();
        level.RecalculateBounds();
        EditorUtility.SetDirty(level);
    }

    public static LevelEditTool GetEraseVariant(LevelEditTool tool)
    {
        switch (tool)
        {
            case LevelEditTool.PaintTile: return LevelEditTool.EraseTile;
            case LevelEditTool.PaintUnit: return LevelEditTool.EraseUnit;
            case LevelEditTool.PaintBoxedUnit: return LevelEditTool.EraseBox;
            case LevelEditTool.PaintBarrier: return LevelEditTool.EraseBarrier;
            default: return tool;
        }
    }

    public static void SaveLevel(LevelData level)
    {
        if (level == null)
        {
            return;
        }

        level.PruneEmptyCells();
        level.RecalculateBounds();

        LevelValidator.ValidationResult result = LevelValidator.Validate(level);
        if (!result.IsValid)
        {
            ShowValidationResult(result, true);
            return;
        }

        EditorUtility.SetDirty(level);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Save Successful", "Level saved successfully.", "OK");
    }

    public static void ShowValidationResult(LevelValidator.ValidationResult result, bool asDialog)
    {
        string message = result.IsValid ? "Level is valid." : result.GetMessage();
        if (result.IsValid)
        {
            Debug.Log(message);
        }
        else
        {
            Debug.LogError(message);
        }

        if (asDialog)
        {
            EditorUtility.DisplayDialog(result.IsValid ? "Validation Passed" : "Validation Failed", message, "OK");
        }
    }

    public static LevelData CreateNewLevelAsset(string path)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
        {
            string[] parts = directory.Replace('\\', '/').Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        LevelData newLevel = ScriptableObject.CreateInstance<LevelData>();
        newLevel.levelNumber = 1;
        newLevel.cells = new System.Collections.Generic.List<CellData>();
        newLevel.RecalculateBounds();

        AssetDatabase.CreateAsset(newLevel, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return newLevel;
    }
}
#endif
