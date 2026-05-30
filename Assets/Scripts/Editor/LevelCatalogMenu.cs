#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LevelCatalogMenu
{
    private const string CatalogPath = "Assets/Data/Levels/LevelCatalog.asset";
    private const string LevelsFolder = "Assets/Data/Levels";

    [MenuItem("Puzzle/Level Catalog/Create Or Update From Levels Folder")]
    public static void CreateOrUpdateCatalog()
    {
        LevelCatalogConfig catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<LevelCatalogConfig>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { LevelsFolder });
        var levels = new List<LevelData>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.EndsWith("LevelCatalog.asset"))
            {
                continue;
            }

            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (level != null)
            {
                levels.Add(level);
            }
        }

        levels.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

        SerializedObject serializedCatalog = new SerializedObject(catalog);
        SerializedProperty entriesProperty = serializedCatalog.FindProperty("_entries");
        entriesProperty.ClearArray();

        for (int i = 0; i < levels.Count; i++)
        {
            entriesProperty.InsertArrayElementAtIndex(i);
            SerializedProperty element = entriesProperty.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("levelNumber").intValue = i + 1;
            element.FindPropertyRelative("levelData").objectReferenceValue = levels[i];
            element.FindPropertyRelative("displayTextOverride").stringValue = string.Empty;
            levels[i].levelNumber = i + 1;
            EditorUtility.SetDirty(levels[i]);
        }

        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        catalog.ApplyAutoNumbering();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        Selection.activeObject = catalog;
        Debug.Log($"Level catalog updated with {levels.Count} level(s) at {CatalogPath}");
    }
}
#endif
