#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class LevelSceneEditorDrawer
{
    private static readonly int ControlId = "LevelSceneEditor".GetHashCode();

    public static void Draw(SceneView sceneView)
    {
        LevelData level = LevelEditorState.Level;
        LevelEditorAnchor anchor = LevelEditorState.Anchor;
        if (level == null || anchor == null)
        {
            return;
        }

        level.RecalculateBounds();
        float cellSize = anchor.CellSize;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        if (level.HasBounds)
        {
            DrawOriginMarker(anchor, cellSize);
            DrawGridBackground(level, anchor, cellSize);
            DrawCells(level, anchor, cellSize);
        }

        DrawInput(level, anchor, cellSize);
        DrawSceneToolbar();

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(12f, 12f, 360f, 110f), GUI.skin.box);
        GUILayout.Label("2D Scene Level Editor", EditorStyles.boldLabel);
        GUILayout.Label($"Tool: {LevelEditorState.Tool}");
        GUILayout.Label("Click anywhere to paint tiles — grid grows automatically.");
        GUILayout.Label("LMB paint  |  Shift+LMB erase  |  Drag to paint");
        if (level.HasBounds)
        {
            string centeredLabel = level.IsCenteredAtOrigin ? "centered at (0,0)" : $"center ({level.BoundsCenterX},{level.BoundsCenterY})";
            GUILayout.Label($"Bounds: {level.width} x {level.height}  ({centeredLabel})");
            GUILayout.Label($"Range: ({level.boundsMinX},{level.boundsMinY}) to ({level.BoundsMaxX},{level.BoundsMaxY})");
        }
        else
        {
            GUILayout.Label("No tiles yet — paint your first tile in the scene.");
        }
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private static void DrawOriginMarker(LevelEditorAnchor anchor, float cellSize)
    {
        Vector3 corner = anchor.GridToWorldPosition(0, 0);
        Vector3 center = corner + new Vector3(cellSize * 0.5f, 0.02f, cellSize * 0.5f);
        Handles.color = new Color(1f, 0.85f, 0.1f, 0.95f);
        Handles.DrawWireCube(center, new Vector3(cellSize * 0.35f, 0.05f, cellSize * 0.35f));
        Handles.Label(center + Vector3.up * 0.08f, "0,0");
    }

    private static void DrawGridBackground(LevelData level, LevelEditorAnchor anchor, float cellSize)
    {
        Vector3 origin = anchor.GridToWorldPosition(level.boundsMinX, level.boundsMinY);
        Vector3 size = new Vector3(level.width * cellSize, 0.01f, level.height * cellSize);
        Vector3 center = origin + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);

        Handles.DrawSolidRectangleWithOutline(
            GetRectCorners(center - new Vector3(size.x * 0.5f, 0f, size.z * 0.5f), size.x, size.z),
            new Color(0.05f, 0.05f, 0.05f, 0.2f),
            new Color(1f, 1f, 1f, 0.35f));
    }

    private static void DrawCells(LevelData level, LevelEditorAnchor anchor, float cellSize)
    {
        for (int x = level.boundsMinX; x <= level.BoundsMaxX; x++)
        {
            for (int y = level.boundsMinY; y <= level.BoundsMaxY; y++)
            {
                CellData cell = level.GetCell(x, y);
                Vector3 corner = anchor.GridToWorldPosition(x, y);
                Color fill = cell != null && LevelData.HasContent(cell)
                    ? LevelEditorOperations.GetCellColor(cell)
                    : LevelEditorOperations.EmptyColor;

                Handles.DrawSolidRectangleWithOutline(
                    GetRectCorners(corner, cellSize, cellSize),
                    fill,
                    new Color(0f, 0f, 0f, 0.35f));

                if (cell != null)
                {
                    string label = LevelEditorOperations.GetCellLabel(cell);
                    if (!string.IsNullOrEmpty(label))
                    {
                        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = Color.white }
                        };
                        Handles.Label(corner + new Vector3(cellSize * 0.5f, 0.05f, cellSize * 0.5f), label, style);
                    }
                }
            }
        }
    }

    private static void DrawInput(LevelData level, LevelEditorAnchor anchor, float cellSize)
    {
        Event currentEvent = Event.current;
        int controlId = GUIUtility.GetControlID(ControlId, FocusType.Passive);

        if (currentEvent.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(controlId);
        }

        if (!TryGetGridCellFromMouse(anchor, currentEvent.mousePosition, out int x, out int y))
        {
            return;
        }

        if (currentEvent.type == EventType.MouseMove || currentEvent.type == EventType.MouseDrag)
        {
            SceneView.RepaintAll();
        }

        bool isErase = currentEvent.shift;
        bool isPaintEvent = currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag;

        if (isPaintEvent && currentEvent.button == 0)
        {
            LevelEditTool tool = isErase
                ? LevelEditorOperations.GetEraseVariant(LevelEditorState.Tool)
                : LevelEditorState.Tool;

            CellData cell = level.GetOrCreateCell(x, y);
            LevelEditorOperations.ApplyTool(level, cell, tool, LevelEditorState.SelectedUnitType);
            level.RecalculateBounds();
            currentEvent.Use();
            GUI.changed = true;
        }
    }

    private static void DrawSceneToolbar()
    {
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(12f, 130f, 160f, 28f));
        if (GUILayout.Button("Frame 2D View", GUILayout.Height(24f)))
        {
            LevelEditorState.FrameSceneView2D();
        }
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private static bool TryGetGridCellFromMouse(LevelEditorAnchor anchor, Vector2 mousePosition, out int x, out int y)
    {
        x = 0;
        y = 0;

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        Plane plane = new Plane(Vector3.up, anchor.transform.position);

        if (!plane.Raycast(ray, out float distance))
        {
            return false;
        }

        Vector3 hit = ray.GetPoint(distance);
        return anchor.TryWorldToGrid(hit, out x, out y);
    }

    private static Vector3[] GetRectCorners(Vector3 corner, float width, float depth)
    {
        return new[]
        {
            corner,
            corner + new Vector3(width, 0f, 0f),
            corner + new Vector3(width, 0f, depth),
            corner + new Vector3(0f, 0f, depth)
        };
    }
}
#endif
