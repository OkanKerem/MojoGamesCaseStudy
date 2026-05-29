#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class LevelEditorWindow : EditorWindow
{
    private string _newLevelPath = "Assets/Data/Levels/Level_New.asset";
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Block Jam/Level Editor")]
    public static void OpenWindow()
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>("Block Jam Level Editor");
        window.Show();
        LevelEditorState.SetActive(true);
        LevelEditorState.FrameSceneView2D();
    }

    private void OnEnable()
    {
        LevelEditorState.SetActive(true);
        LevelEditorState.EnsureAnchor();
    }

    private void OnDisable()
    {
        LevelEditorState.SetActive(false);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Block Jam Level Editor (2D Scene)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Paint in the Scene view on the XZ grid. Use the LevelEditorAnchor object for grid origin.\n" +
            "LMB = paint  |  Shift+LMB = erase  |  Drag to paint multiple cells.",
            MessageType.Info);

        DrawLevelAssetSection();
        EditorGUILayout.Space(8f);

        if (LevelEditorState.Level == null)
        {
            EditorGUILayout.HelpBox("Assign or create a LevelData asset to begin editing.", MessageType.Info);
            return;
        }

        DrawLevelSettings();
        EditorGUILayout.Space(8f);
        DrawTools();
        EditorGUILayout.Space(8f);
        DrawUtilityButtons();
        EditorGUILayout.Space(8f);
        DrawSceneControls();
    }

    private void DrawLevelAssetSection()
    {
        EditorGUI.BeginChangeCheck();
        LevelEditorState.Level = (LevelData)EditorGUILayout.ObjectField("Level Asset", LevelEditorState.Level, typeof(LevelData), false);
        if (EditorGUI.EndChangeCheck() && LevelEditorState.Level != null)
        {
            LevelEditorState.Level.RecalculateBounds();
            LevelEditorState.RepaintSceneViews();
        }

        EditorGUILayout.BeginHorizontal();
        _newLevelPath = EditorGUILayout.TextField("New Level Path", _newLevelPath);
        if (GUILayout.Button("Create New", GUILayout.Width(100f)))
        {
            LevelData created = LevelEditorOperations.CreateNewLevelAsset(_newLevelPath);
            LevelEditorState.Level = created;
            EditorGUIUtility.PingObject(created);
            LevelEditorState.FrameSceneView2D();
        }
        EditorGUILayout.EndHorizontal();

        LevelEditorState.Anchor = (LevelEditorAnchor)EditorGUILayout.ObjectField(
            "Grid Anchor",
            LevelEditorState.Anchor,
            typeof(LevelEditorAnchor),
            true);
    }

    private void DrawLevelSettings()
    {
        LevelData level = LevelEditorState.Level;
        level.RecalculateBounds();

        EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
        level.levelNumber = EditorGUILayout.IntField("Level Number", level.levelNumber);

        EditorGUILayout.LabelField("Grid Bounds (auto)", EditorStyles.boldLabel);
        if (level.HasBounds)
        {
            EditorGUILayout.LabelField("Size", $"{level.width} x {level.height}");
            EditorGUILayout.LabelField("From", $"({level.boundsMinX}, {level.boundsMinY})");
            EditorGUILayout.LabelField("To", $"({level.BoundsMaxX}, {level.BoundsMaxY})");
            EditorGUILayout.LabelField("Center", $"({level.BoundsCenterX}, {level.BoundsCenterY})");
        }
        else
        {
            EditorGUILayout.HelpBox("No tiles yet. Paint in the Scene view to grow the level.", MessageType.Info);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Center Level at (0,0)"))
        {
            Undo.RecordObject(level, "Center Level");
            level.CenterBoundsAtOrigin();
            EditorUtility.SetDirty(level);
            LevelEditorState.FrameSceneView2D();
            LevelEditorState.RepaintSceneViews();
        }

        if (GUILayout.Button("Top-Left to (0,0)"))
        {
            Undo.RecordObject(level, "Normalize Grid");
            level.NormalizeBoundsToOrigin();
            EditorUtility.SetDirty(level);
            LevelEditorState.FrameSceneView2D();
            LevelEditorState.RepaintSceneViews();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTools()
    {
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
        LevelEditorState.Tool = (LevelEditTool)EditorGUILayout.EnumPopup("Active Tool", LevelEditorState.Tool);
        LevelEditorState.SelectedUnitType = (UnitTypeData)EditorGUILayout.ObjectField(
            "Unit Type",
            LevelEditorState.SelectedUnitType,
            typeof(UnitTypeData),
            false);

        if ((LevelEditorState.Tool == LevelEditTool.PaintUnit || LevelEditorState.Tool == LevelEditTool.PaintBoxedUnit) && LevelEditorState.SelectedUnitType == null)
        {
            EditorGUILayout.HelpBox("Select a UnitTypeData asset to paint units.", MessageType.Warning);
        }
    }

    private void DrawUtilityButtons()
    {
        LevelData level = LevelEditorState.Level;
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill Rectangle"))
        {
            Undo.RecordObject(level, "Fill Rectangle");
            level.FillAllTilesInBounds();
            EditorUtility.SetDirty(level);
            LevelEditorState.RepaintSceneViews();
        }

        if (GUILayout.Button("Clear Units"))
        {
            Undo.RecordObject(level, "Clear Units");
            level.ClearAllUnits();
            EditorUtility.SetDirty(level);
            LevelEditorState.RepaintSceneViews();
        }

        if (GUILayout.Button("Clear Barriers"))
        {
            Undo.RecordObject(level, "Clear Barriers");
            level.ClearAllBarriers();
            EditorUtility.SetDirty(level);
            LevelEditorState.RepaintSceneViews();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Level"))
        {
            if (EditorUtility.DisplayDialog("Clear Level", "Remove all cell data?", "Clear", "Cancel"))
            {
                Undo.RecordObject(level, "Clear Level");
                level.ClearLevelContents();
                EditorUtility.SetDirty(level);
                LevelEditorState.RepaintSceneViews();
            }
        }

        if (GUILayout.Button("Validate"))
        {
            LevelEditorOperations.ShowValidationResult(LevelValidator.Validate(level), false);
        }

        if (GUILayout.Button("Save"))
        {
            LevelEditorOperations.SaveLevel(level);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSceneControls()
    {
        EditorGUILayout.LabelField("Scene View", EditorStyles.boldLabel);

        if (LevelEditorState.Anchor != null)
        {
            EditorGUILayout.LabelField("Cell Size", LevelEditorState.Anchor.CellSize.ToString("0.##"));
            EditorGUILayout.LabelField("Grid Origin", LevelEditorState.Anchor.transform.position.ToString("F2"));
        }

        if (GUILayout.Button("Frame 2D Top-Down View"))
        {
            LevelEditorState.FrameSceneView2D();
        }

        if (GUILayout.Button("Select Grid Anchor"))
        {
            LevelEditorState.EnsureAnchor();
            Selection.activeGameObject = LevelEditorState.Anchor.gameObject;
        }
    }
}
#endif