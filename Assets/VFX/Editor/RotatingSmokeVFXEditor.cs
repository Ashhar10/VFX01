using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RotatingSmokeVFX))]
public class RotatingSmokeVFXEditor : Editor
{
    public override void OnInspectorGUI()
    {
        RotatingSmokeVFX smoke = (RotatingSmokeVFX)target;

        // Custom Header
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("🔮 Alura Graves - Miasma Smoke VFX", titleStyle);
        EditorGUILayout.LabelField("Authentic Stage [1] Wind-up Necromantic Fog & Soul Motes", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);

        // Transport Controls
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("▶ Play Miasma", GUILayout.Height(30)))
        {
            smoke.Play();
        }

        GUI.backgroundColor = new Color(0.9f, 0.5f, 0.3f);
        if (GUILayout.Button("⏸ Stop", GUILayout.Height(30)))
        {
            smoke.Stop();
        }

        GUI.backgroundColor = new Color(0.5f, 0.7f, 1.0f);
        if (GUILayout.Button("🔄 Rebuild All Layers", GUILayout.Height(30)))
        {
            smoke.InitializeEffect();
            smoke.Play();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // Draw standard fields
        DrawDefaultInspector();

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("💨 Stage [4] Fog Dissipation & Cloud Elimination (Image 4)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Controls dynamic fog dissipation & cloud elimination along the specified axis (Image 4).\n" +
            "• Dissipation Axis: Combined_Z_Sweep_And_Radial (sweeps +Z forward, deforms into tails, disperses and vanishes), " +
            "Z_Axis_Forward, Vertical_Y (height cutoff), Radial_XZ (ground spread fade), or Both_Y_and_XZ.\n" +
            "• Eliminate Clouds: Instant boolean toggle to eliminate tall volumetric clouds & wisps, leaving ground mist.\n" +
            "• Progress Slider: 0 = full miasma vortex, 1 = fog dissipated / board returned to normal.",
            MessageType.Info
        );

        EditorGUI.BeginChangeCheck();
        float newSweepSpeed = EditorGUILayout.Slider("Z-Axis Sweep Speed", smoke.ZSweepSpeed, 0f, 15f);
        bool newTailDeform = EditorGUILayout.Toggle("Enable Tail Deformation", smoke.EnableTailDeformation);
        float newTailScale = EditorGUILayout.Slider("Tail Stretch (Velocity Scale)", smoke.TailDeformationScale, 0.5f, 6.0f);
        float newTailLength = EditorGUILayout.Slider("Tail Length (Length Scale)", smoke.TailLengthScale, 0.5f, 5.0f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(smoke, "Change Dissipation Settings");
            smoke.ZSweepSpeed = newSweepSpeed;
            smoke.EnableTailDeformation = newTailDeform;
            smoke.TailDeformationScale = newTailScale;
            smoke.TailLengthScale = newTailLength;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        string cloudToggleText = smoke.EliminateClouds ? "☁️ Restore Clouds" : "❌ Eliminate Clouds (Instant)";
        GUI.backgroundColor = smoke.EliminateClouds ? new Color(0.6f, 0.9f, 0.6f) : new Color(0.95f, 0.6f, 0.6f);
        if (GUILayout.Button(cloudToggleText, GUILayout.Height(30)))
        {
            Undo.RecordObject(smoke, "Toggle Eliminate Clouds");
            smoke.EliminateClouds = !smoke.EliminateClouds;
        }

        GUI.backgroundColor = new Color(0.4f, 0.85f, 1.0f);
        if (GUILayout.Button("💨 Trigger Z-Sweep Tail Dissipation", GUILayout.Height(30)))
        {
            smoke.TriggerDissipation();
        }

        GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
        if (GUILayout.Button("↺ Reset Full Fog", GUILayout.Height(30)))
        {
            Undo.RecordObject(smoke, "Reset Fog Dissipation");
            smoke.ResetDissipation();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("✨ Layer 5: Sparkle Controls (User Friendly)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Controls subtle energy motes and star twinkles (Images 2 & 3).\n" +
            "• Sparkle Quantity: 0 = OFF, 5 = Delicate Ambient Twinkles (controls Diamond ◆ & Cross + together).\n" +
            "• Sparkle Size: controls the size of the twinkling star motes.",
            MessageType.None
        );

        EditorGUI.BeginChangeCheck();
        float newQty = EditorGUILayout.Slider("Sparkle Quantity (0 = OFF)", smoke.SparkleQuantity, 0f, 20f);
        float newSize = EditorGUILayout.Slider("Sparkle Size", smoke.SparkleSize, 0.01f, 0.08f);
        float newBloom = EditorGUILayout.Slider("Sparkle Glow / Bloom", smoke.SparkleBloomIntensity, 0f, 6.0f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(smoke, "Change Sparkle Settings");
            smoke.SparkleQuantity = newQty;
            smoke.SparkleSize = newSize;
            smoke.SparkleBloomIntensity = newBloom;
        }

        EditorGUILayout.BeginHorizontal();
        string sparkleToggleText = smoke.EnableSparkles ? "✨ Sparkles: ACTIVE" : "⭕ Sparkles: DISABLED";
        GUI.backgroundColor = smoke.EnableSparkles ? new Color(0.6f, 0.9f, 0.6f) : new Color(0.9f, 0.6f, 0.6f);
        if (GUILayout.Button(sparkleToggleText, GUILayout.Height(28)))
        {
            Undo.RecordObject(smoke, "Toggle Sparkles");
            smoke.EnableSparkles = !smoke.EnableSparkles;
        }

        GUI.backgroundColor = new Color(0.85f, 0.75f, 1.0f);
        if (GUILayout.Button("🌸 Reset to Delicate Twinkles (Default)", GUILayout.Height(28)))
        {
            Undo.RecordObject(smoke, "Apply Delicate Sparkles");
            smoke.SparkleQuantity = 5.0f;
            smoke.SparkleSize = 0.032f;
            smoke.SparkleBloomIntensity = 2.5f;
            smoke.EnableSparkles = true;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("⚡ Quick Presets (Design Guide)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("☁️ Cloudy Power (Bone White & Lavender)"))
        {
            Undo.RecordObject(smoke, "Apply Cloudy Power Preset");
            SerializedObject so = new SerializedObject(smoke);
            so.FindProperty("vortexRadius").floatValue = 1.7f;
            so.FindProperty("vortexHeight").floatValue = 2.4f;
            so.FindProperty("rotationSpeed").floatValue = 60f;
            so.FindProperty("upwardSpeed").floatValue = 0.40f;
            so.FindProperty("particleScale").floatValue = 1.0f;
            so.FindProperty("densityMultiplier").floatValue = 1.0f;
            so.FindProperty("boneWhiteColor").colorValue = new Color(0.96f, 0.93f, 0.88f, 1.0f);
            so.FindProperty("midtoneViolet").colorValue = new Color(0.75f, 0.60f, 0.90f, 1.0f);
            so.FindProperty("deepCharcoalPurple").colorValue = new Color(0.85f, 0.78f, 0.92f, 1.0f);
            so.FindProperty("rimHighlightLilac").colorValue = new Color(0.92f, 0.86f, 0.98f, 1.0f);
            so.FindProperty("groundMistColor").colorValue = new Color(0.90f, 0.87f, 0.85f, 1.0f);
            so.FindProperty("necroticGreenColor").colorValue = new Color(0.98f, 0.95f, 0.90f, 1.0f);
            so.FindProperty("enableSparkles").boolValue = true;
            so.FindProperty("enableDiamondSparkles").boolValue = true;
            so.FindProperty("enableCrossSparkles").boolValue = true;
            so.FindProperty("sparkleQuantity").floatValue = 5.0f;
            so.FindProperty("sparkleSize").floatValue = 0.032f;
            so.FindProperty("sparkleBloomIntensity").floatValue = 2.5f;
            so.FindProperty("eliminateClouds").boolValue = false;
            so.FindProperty("enableDissipation").boolValue = false;
            so.FindProperty("dissipationProgress").floatValue = 0f;
            so.FindProperty("cloudHeightCutoff").floatValue = 2.4f;
            so.FindProperty("dissipationRadialSpread").floatValue = 4.5f;
            so.FindProperty("dissipationDuration").floatValue = 3.0f;
            so.ApplyModifiedProperties();
            smoke.InitializeEffect();
            smoke.Play();
        }
        if (GUILayout.Button("Subtle Wispy Fog"))
        {
            Undo.RecordObject(smoke, "Apply Subtle Wispy Preset");
            SerializedObject so = new SerializedObject(smoke);
            so.FindProperty("vortexRadius").floatValue = 1.5f;
            so.FindProperty("vortexHeight").floatValue = 2.0f;
            so.FindProperty("rotationSpeed").floatValue = 45f;
            so.FindProperty("upwardSpeed").floatValue = 0.30f;
            so.FindProperty("particleScale").floatValue = 0.85f;
            so.FindProperty("densityMultiplier").floatValue = 0.75f;
            so.FindProperty("boneWhiteColor").colorValue = new Color(0.96f, 0.93f, 0.88f, 1.0f);
            so.FindProperty("midtoneViolet").colorValue = new Color(0.75f, 0.60f, 0.90f, 1.0f);
            so.FindProperty("deepCharcoalPurple").colorValue = new Color(0.85f, 0.78f, 0.92f, 1.0f);
            so.FindProperty("rimHighlightLilac").colorValue = new Color(0.92f, 0.86f, 0.98f, 1.0f);
            so.FindProperty("groundMistColor").colorValue = new Color(0.90f, 0.87f, 0.85f, 1.0f);
            so.FindProperty("necroticGreenColor").colorValue = new Color(0.98f, 0.95f, 0.90f, 1.0f);
            so.FindProperty("enableSparkles").boolValue = true;
            so.FindProperty("enableDiamondSparkles").boolValue = true;
            so.FindProperty("enableCrossSparkles").boolValue = true;
            so.FindProperty("sparkleQuantity").floatValue = 3.0f;
            so.FindProperty("sparkleSize").floatValue = 0.025f;
            so.FindProperty("sparkleBloomIntensity").floatValue = 2.0f;
            so.FindProperty("eliminateClouds").boolValue = false;
            so.FindProperty("enableDissipation").boolValue = false;
            so.FindProperty("dissipationProgress").floatValue = 0f;
            so.FindProperty("cloudHeightCutoff").floatValue = 2.0f;
            so.FindProperty("dissipationRadialSpread").floatValue = 4.0f;
            so.FindProperty("dissipationDuration").floatValue = 3.0f;
            so.ApplyModifiedProperties();
            smoke.InitializeEffect();
            smoke.Play();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    [MenuItem("GameObject/Effects/Necromantic Miasma Smoke", false, 10)]
    public static void CreateNecromanticSmoke(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("Necromantic_Miasma_Smoke");
        RotatingSmokeVFX smoke = go.AddComponent<RotatingSmokeVFX>();
        smoke.InitializeEffect();
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create Necromantic Miasma Smoke");
        Selection.activeObject = go;
    }
}
