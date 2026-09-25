using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MiasmaDissipationVFX))]
public class MiasmaDissipationVFXEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MiasmaDissipationVFX dissipation = (MiasmaDissipationVFX)target;

        // Custom Header
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("💨 Stage [4] Miasma Fog Dissipation VFX", titleStyle);
        EditorGUILayout.LabelField("Smooth Opacity Timeline Dissolve (x → 0)", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);

        // Main Action Box
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("✨ Timeline Dissolve Controller", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "When this VFX is enabled, it smoothly fades Necromantic_Rotating_Smoke opacity from x down to 0 over the timeline duration.\n" +
            "No axis movement, no spiky streaks, no needle stretching — pure soft cloud dissolve.",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.85f, 1.0f);
        if (GUILayout.Button("💨 Trigger Fade to 0 (Disappear)", GUILayout.Height(34)))
        {
            dissipation.TriggerTimelineFade();
        }

        GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
        if (GUILayout.Button("↺ Reset to Full Opacity", GUILayout.Height(34)))
        {
            Undo.RecordObject(dissipation, "Reset Opacity");
            dissipation.ResetOpacity();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUI.BeginChangeCheck();
        float newDuration = EditorGUILayout.Slider("Fade Duration (seconds)", dissipation.FadeDuration, 0.5f, 6.0f);
        float newProgress = EditorGUILayout.Slider("Timeline Progress (0 → 1)", dissipation.TimelineProgress, 0f, 1f);
        float newStartOp = EditorGUILayout.Slider("Start Opacity (x)", dissipation.StartOpacity, 0.1f, 1f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(dissipation, "Change Timeline Settings");
            dissipation.FadeDuration = newDuration;
            dissipation.StartOpacity = newStartOp;
            dissipation.TimelineProgress = newProgress;
        }

        // Live status bar
        EditorGUILayout.Space(2);
        Rect rect = EditorGUILayout.GetControlRect(false, 20);
        float liveOp = dissipation.CurrentOpacity;
        string statusText = dissipation.IsDisappeared ? "Smoke Disappeared (Opacity: 0.0)" : $"Live Smoke Opacity: {liveOp:F2}";
        EditorGUI.ProgressBar(rect, liveOp, statusText);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // Draw standard fields
        DrawDefaultInspector();

        EditorGUILayout.Space(4);
    }
}
