#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelCatalogConfig))]
public class LevelCatalogConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelCatalogConfig catalog = (LevelCatalogConfig)target;
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Level overview", EditorStyles.boldLabel);

        if (catalog.LevelCount == 0)
        {
            EditorGUILayout.HelpBox("Add entries to the list above. Each row needs a Level Data asset.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            for (int i = 0; i < catalog.LevelCount; i++)
            {
                LevelCatalogEntry entry = catalog.GetEntry(i);
                if (entry == null)
                {
                    EditorGUILayout.LabelField($"[{i}] (null entry)");
                    continue;
                }

                string levelAssetName = entry.levelData != null ? entry.levelData.name : "(missing)";
                string hudPreview = entry.GetDisplayText(catalog.DefaultLevelTextFormat);
                EditorGUILayout.LabelField(
                    $"#{entry.levelNumber}  |  index {i}  |  {levelAssetName}",
                    hudPreview,
                    EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(4f);
        if (GUILayout.Button("Renumber all entries (1, 2, 3...)"))
        {
            catalog.ApplyAutoNumbering();
            EditorUtility.SetDirty(catalog);
        }
    }
}
#endif
