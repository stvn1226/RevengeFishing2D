#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class TimeScaleTool : EditorWindow
{
    private float timeScale = 1f;

    [MenuItem("Tools/Testing Tools/Time Scale Tool")]
    public static void ShowWindow()
    {
        GetWindow<TimeScaleTool>("Time Scale Tool");
    }

    void OnGUI()
    {
        GUILayout.Label("Time Scale Testing Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Show current time
        EditorGUILayout.LabelField("Current:", Time.timeScale.ToString("F1") + "x");
        EditorGUILayout.Space();

        // Slider for more accuracy
        timeScale = EditorGUILayout.Slider("Time Scale", timeScale, 0.1f, 10f);

        if (GUILayout.Button("Apply"))
        {
            Time.timeScale = timeScale;
        }

        EditorGUILayout.Space();

        // Fast buttons
        GUILayout.Label("Quick Settings:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("0.5x")) SetTimeScale(0.5f);
        if (GUILayout.Button("1x")) SetTimeScale(1f);
        if (GUILayout.Button("2x")) SetTimeScale(2f);
        if (GUILayout.Button("5x")) SetTimeScale(5f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("10x")) SetTimeScale(10f);
        if (GUILayout.Button("PAUSE")) SetTimeScale(0f);
        EditorGUILayout.EndHorizontal();
    }

    void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        timeScale = scale;
        Debug.Log($"Time Scale: {scale}x");
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }
}
#endif
