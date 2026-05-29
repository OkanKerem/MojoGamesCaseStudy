#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class LevelEditorState
{
    public static bool IsActive;
    public static LevelData Level;
    public static LevelEditTool Tool = LevelEditTool.PaintTile;
    public static UnitTypeData SelectedUnitType;
    public static LevelEditorAnchor Anchor;

    static LevelEditorState()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.playModeStateChanged += _ => RepaintSceneViews();
    }

    public static void SetActive(bool active)
    {
        IsActive = active;
        if (active)
        {
            EnsureAnchor();
        }

        RepaintSceneViews();
    }

    public static void EnsureAnchor()
    {
        if (Anchor != null)
        {
            return;
        }

        Anchor = Object.FindFirstObjectByType<LevelEditorAnchor>();
        if (Anchor != null)
        {
            return;
        }

        GameObject anchorObject = new GameObject("LevelEditorAnchor");
        Anchor = anchorObject.AddComponent<LevelEditorAnchor>();
        Anchor.transform.position = new Vector3(-2.2f, 0f, -2.2f);
        Undo.RegisterCreatedObjectUndo(anchorObject, "Create Level Editor Anchor");
        Selection.activeGameObject = anchorObject;
    }

    public static void FrameSceneView2D()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            return;
        }

        EnsureAnchor();
        LevelData level = Level;
        if (level != null)
        {
            level.RecalculateBounds();
        }

        float cellSize = Anchor != null ? Anchor.CellSize : 1.1f;
        Vector3 center = Anchor != null ? Anchor.GridToWorldPosition(0, 0) + new Vector3(cellSize * 0.5f, 0f, cellSize * 0.5f) : Vector3.zero;

        if (level != null && level.HasBounds)
        {
            Vector3 minCorner = Anchor.GridToWorldPosition(level.boundsMinX, level.boundsMinY);
            Vector3 maxCorner = Anchor.GridToWorldPosition(level.BoundsMaxX + 1, level.BoundsMaxY + 1);
            center = (minCorner + maxCorner) * 0.5f;
        }

        sceneView.orthographic = true;
        sceneView.rotation = Quaternion.Euler(90f, 0f, 0f);
        sceneView.pivot = center + Vector3.up * 2f;
        float span = level != null && level.HasBounds ? Mathf.Max(level.width, level.height) : 5f;
        sceneView.size = span * cellSize * 0.85f + 2f;
        sceneView.Repaint();
    }

    private static void OnSelectionChanged()
    {
        LevelEditorAnchor selected = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<LevelEditorAnchor>()
            : null;

        if (selected != null)
        {
            Anchor = selected;
            RepaintSceneViews();
        }
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!IsActive || Level == null || Anchor == null)
        {
            return;
        }

        LevelSceneEditorDrawer.Draw(sceneView);
    }

    public static void RepaintSceneViews()
    {
        SceneView.RepaintAll();
    }
}
#endif
