#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelEditorAnchor))]
public class LevelEditorAnchorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Open Level Editor"))
        {
            LevelEditorWindow.OpenWindow();
        }

        if (GUILayout.Button("Frame 2D Scene View"))
        {
            LevelEditorState.FrameSceneView2D();
        }
    }
}
#endif
