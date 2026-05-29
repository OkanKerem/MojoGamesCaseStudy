using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Puzzle/Level Catalog Config")]
public class LevelCatalogConfig : ScriptableObject
{
    [Tooltip("Default HUD text. {0} = level number from each entry.")]
    [SerializeField] private string _defaultLevelTextFormat = "Level {0}";

    [Tooltip("When enabled, entry level numbers are set to 1, 2, 3... in list order on validate.")]
    [SerializeField] private bool _autoNumberFromListOrder = true;

    [SerializeField] private List<LevelCatalogEntry> _entries = new List<LevelCatalogEntry>();

    public int LevelCount => _entries != null ? _entries.Count : 0;
    public string DefaultLevelTextFormat => _defaultLevelTextFormat;
    public IReadOnlyList<LevelCatalogEntry> Entries => _entries;

    public LevelCatalogEntry GetEntry(int index)
    {
        if (_entries == null || _entries.Count == 0)
        {
            return null;
        }

        int clampedIndex = Mathf.Clamp(index, 0, _entries.Count - 1);
        return _entries[clampedIndex];
    }

    public LevelData GetLevelData(int index)
    {
        return GetEntry(index)?.levelData;
    }

    public string GetDisplayText(int index)
    {
        LevelCatalogEntry entry = GetEntry(index);
        if (entry == null)
        {
            return string.Empty;
        }

        return entry.GetDisplayText(_defaultLevelTextFormat);
    }

    public int GetLevelNumber(int index)
    {
        LevelCatalogEntry entry = GetEntry(index);
        return entry != null ? entry.levelNumber : index + 1;
    }

    public int FindIndexByLevelData(LevelData levelData)
    {
        if (levelData == null || _entries == null)
        {
            return -1;
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i] != null && _entries[i].levelData == levelData)
            {
                return i;
            }
        }

        return -1;
    }

    public void ApplyAutoNumbering()
    {
        if (_entries == null)
        {
            return;
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            LevelCatalogEntry entry = _entries[i];
            if (entry == null)
            {
                continue;
            }

            entry.levelNumber = i + 1;

#if UNITY_EDITOR
            if (entry.levelData != null)
            {
                entry.levelData.levelNumber = entry.levelNumber;
                UnityEditor.EditorUtility.SetDirty(entry.levelData);
            }
#endif
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_autoNumberFromListOrder)
        {
            ApplyAutoNumbering();
        }
    }
#endif
}
