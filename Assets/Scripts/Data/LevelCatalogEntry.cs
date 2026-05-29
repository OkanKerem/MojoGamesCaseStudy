using System;
using UnityEngine;

[Serializable]
public class LevelCatalogEntry
{
    [Tooltip("Shown in HUD (e.g. Level 3). Auto-filled from list order when auto-numbering is enabled.")]
    public int levelNumber = 1;

    [Tooltip("Level layout asset to load.")]
    public LevelData levelData;

    [Tooltip("Optional HUD override. Leave empty to use the catalog format (e.g. Level 3).")]
    public string displayTextOverride;

    public bool IsValid => levelData != null;

    public string GetDisplayText(string format)
    {
        if (!string.IsNullOrWhiteSpace(displayTextOverride))
        {
            return displayTextOverride.Trim();
        }

        if (string.IsNullOrEmpty(format))
        {
            return $"Level {levelNumber}";
        }

        return format.Contains("{0}")
            ? string.Format(format, levelNumber)
            : $"{format} {levelNumber}";
    }
}
