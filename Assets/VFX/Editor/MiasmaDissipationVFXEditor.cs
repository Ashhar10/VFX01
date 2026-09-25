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
        EditorGUILayout.LabelField("Traffic Circle Roundabout +Z Blowout & Aerodynamic Tail Dissipation", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);

        // Prominent Trigger Button (Exact match for user's screenshot Image 1: media_1790340821190.png)
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = new Color(0.18f, 0.38f, 0.44f); // Rich dark teal matching user's Image 1 screenshot
        GUIStyle triggerButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        if (GUILayout.Button("💨  Trigger Z-Sweep Tail Dissipation", triggerButtonStyle, GUILayout.Height(36)))
        {
            dissipation.TriggerDissipation();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(2);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.85f, 0.85f, 0.95f);
        if (GUILayout.Button("🔄 Sync From Rotating Smoke", GUILayout.Height(28)))
        {
            Undo.RecordObject(dissipation, "Sync From Rotating Smoke");
            dissipation.SyncFromRotatingSmoke();
        }

        GUI.backgroundColor = new Color(0.95f, 0.85f, 0.85f);
        if (GUILayout.Button("↺ Reset & Restore Smoke", GUILayout.Height(28)))
        {
            Undo.RecordObject(dissipation, "Reset Effect");
            dissipation.ResetEffect();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // Transport Controls
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("▶ Play", GUILayout.Height(28)))
        {
            dissipation.Play();
        }

        GUI.backgroundColor = new Color(0.9f, 0.5f, 0.3f);
        if (GUILayout.Button("⏸ Stop", GUILayout.Height(28)))
        {
            dissipation.Stop();
        }

        GUI.backgroundColor = new Color(0.5f, 0.7f, 1.0f);
        if (GUILayout.Button("🔄 Rebuild Layers", GUILayout.Height(28)))
        {
            dissipation.InitializeEffect();
            dissipation.Play();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // Traffic Circle & +Z Blowout Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🚗 Traffic Circle Roundabout +Z Blowout (User Specification)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Traffic Circle Mechanics:\n" +
            "• Concentric Rows: Particles on any radial row circle around the roundabout island (Orbital Y swirl).\n" +
            "• +Z Highway Exit: Particles smoothly funnel out down the Z-axis in a fast, hot blowout stream.\n" +
            "• Aerodynamic Tails: Stretch Render Mode deforms particles into sleek smoke tails along velocity.\n" +
            "• Non-Destructive: Necromantic_Rotating_Smoke remains untouched and resumes on Reset/Disable.",
            MessageType.Info
        );

        EditorGUI.BeginChangeCheck();
        float newSpeed = EditorGUILayout.Slider("Z Blowout Speed (m/s)", dissipation.ZBlowoutSpeed, 1.0f, 20.0f);
        float newSwirl = EditorGUILayout.Slider("Roundabout Swirl Speed (°/s)", dissipation.RoundaboutSwirlSpeed, 10f, 300f);
        MiasmaDissipationVFX.BlowoutDirectionMode newMode = (MiasmaDissipationVFX.BlowoutDirectionMode)EditorGUILayout.EnumPopup("Direction Mode", dissipation.DirectionMode);
        Vector3 newCustomDir = dissipation.CustomDirection;
        if (newMode == MiasmaDissipationVFX.BlowoutDirectionMode.Custom_Direction)
        {
            newCustomDir = EditorGUILayout.Vector3Field("Custom Direction", dissipation.CustomDirection);
        }

        bool newTailDeform = EditorGUILayout.Toggle("Enable Tail Deformation", dissipation.EnableTailDeformation);
        float newTailScale = EditorGUILayout.Slider("Tail Stretch (Velocity Scale)", dissipation.TailDeformationScale, 0.5f, 5.0f);
        float newTailLength = EditorGUILayout.Slider("Tail Length Scale", dissipation.TailLengthScale, 0.5f, 4.0f);
        float newDispersion = EditorGUILayout.Slider("Lateral Dispersion", dissipation.LateralDispersion, 0f, 3.0f);
        bool newLoop = EditorGUILayout.Toggle("Loop Blowout (Continuous)", dissipation.LoopBlowout);
        float newDuration = EditorGUILayout.Slider("Blowout Duration (sec)", dissipation.DissipationDuration, 0.5f, 8.0f);
        float newProgress = EditorGUILayout.Slider("Dissipation Progress", dissipation.DissipationProgress, 0f, 1f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(dissipation, "Change Blowout Settings");
            dissipation.ZBlowoutSpeed = newSpeed;
            dissipation.RoundaboutSwirlSpeed = newSwirl;
            dissipation.DirectionMode = newMode;
            dissipation.CustomDirection = newCustomDir;
            dissipation.EnableTailDeformation = newTailDeform;
            dissipation.TailDeformationScale = newTailScale;
            dissipation.TailLengthScale = newTailLength;
            dissipation.LateralDispersion = newDispersion;
            dissipation.LoopBlowout = newLoop;
            dissipation.DissipationDuration = newDuration;
            dissipation.DissipationProgress = newProgress;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // Draw standard fields
        DrawDefaultInspector();

        EditorGUILayout.Space(6);
    }
}
