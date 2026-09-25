using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MiasmaEnemyEffectVFX))]
public class MiasmaEnemyEffectVFXEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MiasmaEnemyEffectVFX effect = (MiasmaEnemyEffectVFX)target;

        // Custom Header
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("💀 Miasma Affected Enemy Status VFX", titleStyle);
        EditorGUILayout.LabelField("Images 4 & 5: Spectral Skull Mask, Torso Mist & Orbiting Sparkles", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);

        // Transport Controls
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("▶ Play Effect", GUILayout.Height(30)))
        {
            effect.Play();
        }

        GUI.backgroundColor = new Color(0.9f, 0.5f, 0.3f);
        if (GUILayout.Button("⏸ Stop", GUILayout.Height(30)))
        {
            effect.Stop();
        }

        GUI.backgroundColor = new Color(0.5f, 0.7f, 1.0f);
        if (GUILayout.Button("🔄 Rebuild Layers", GUILayout.Height(30)))
        {
            effect.InitializeEffect();
            effect.Play();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // Standard fields
        DrawDefaultInspector();

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("✨ Layer 4: Enemy Sparkles (User Friendly)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Controls subtle status sparkles around the enemy (Images 2 & 3).\n" +
            "• Sparkle Quantity: 0 = OFF, 4 = Delicate Subtle Twinkles (controls Diamond ◆ & Cross + together).\n" +
            "• Sparkle Size: controls the size of the twinkling star motes.",
            MessageType.None
        );

        EditorGUI.BeginChangeCheck();
        float newQty = EditorGUILayout.Slider("Sparkle Quantity (0 = OFF)", effect.SparkleQuantity, 0f, 15f);
        float newSize = EditorGUILayout.Slider("Sparkle Size", effect.SparkleSize, 0.01f, 0.06f);
        float newBloom = EditorGUILayout.Slider("Sparkle Glow / Bloom", effect.SparkleBloomIntensity, 0f, 6.0f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(effect, "Change Enemy Sparkle Settings");
            effect.SparkleQuantity = newQty;
            effect.SparkleSize = newSize;
            effect.SparkleBloomIntensity = newBloom;
        }

        EditorGUILayout.BeginHorizontal();
        string sparkleToggleText = effect.EnableSparkles ? "✨ Sparkles: ACTIVE" : "⭕ Sparkles: DISABLED";
        GUI.backgroundColor = effect.EnableSparkles ? new Color(0.6f, 0.9f, 0.6f) : new Color(0.9f, 0.6f, 0.6f);
        if (GUILayout.Button(sparkleToggleText, GUILayout.Height(28)))
        {
            Undo.RecordObject(effect, "Toggle Enemy Sparkles");
            effect.EnableSparkles = !effect.EnableSparkles;
        }

        GUI.backgroundColor = new Color(0.85f, 0.75f, 1.0f);
        if (GUILayout.Button("🌸 Reset to Delicate Twinkles (Default)", GUILayout.Height(28)))
        {
            Undo.RecordObject(effect, "Apply Delicate Sparkles");
            effect.SparkleQuantity = 4.0f;
            effect.SparkleSize = 0.028f;
            effect.SparkleBloomIntensity = 2.5f;
            effect.EnableSparkles = true;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("⚡ Quick Presets (Design Guide)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("💀 Standard Curse (Images 4 & 5)"))
        {
            Undo.RecordObject(effect, "Apply Standard Curse Preset");
            SerializedObject so = new SerializedObject(effect);
            so.FindProperty("bodyRadius").floatValue = 0.65f;
            so.FindProperty("bodyHeight").floatValue = 1.8f;
            so.FindProperty("swirlSpeed").floatValue = 50f;
            so.FindProperty("upwardSpeed").floatValue = 0.35f;
            so.FindProperty("enableSkullMask").boolValue = true;
            so.FindProperty("skullHeightOffset").floatValue = 1.35f;
            so.FindProperty("skullScale").floatValue = 0.65f;
            so.FindProperty("enableBodyMist").boolValue = true;
            so.FindProperty("enableWaistRing").boolValue = true;
            so.FindProperty("enableSparkles").boolValue = true;
            so.FindProperty("enableDiamondSparkles").boolValue = true;
            so.FindProperty("enableCrossSparkles").boolValue = true;
            so.FindProperty("sparkleQuantity").floatValue = 4.0f;
            so.FindProperty("sparkleSize").floatValue = 0.028f;
            so.FindProperty("skullBloomIntensity").floatValue = 3.5f;
            so.FindProperty("sparkleBloomIntensity").floatValue = 2.5f;
            so.ApplyModifiedProperties();
            effect.InitializeEffect();
            effect.Play();
        }
        if (GUILayout.Button("Heavy Necrotic Cloud"))
        {
            Undo.RecordObject(effect, "Apply Heavy Cloud Preset");
            SerializedObject so = new SerializedObject(effect);
            so.FindProperty("bodyRadius").floatValue = 0.85f;
            so.FindProperty("bodyHeight").floatValue = 2.1f;
            so.FindProperty("swirlSpeed").floatValue = 75f;
            so.FindProperty("upwardSpeed").floatValue = 0.50f;
            so.FindProperty("enableSkullMask").boolValue = true;
            so.FindProperty("skullHeightOffset").floatValue = 1.45f;
            so.FindProperty("skullScale").floatValue = 0.85f;
            so.FindProperty("enableBodyMist").boolValue = true;
            so.FindProperty("enableWaistRing").boolValue = true;
            so.FindProperty("enableSparkles").boolValue = true;
            so.FindProperty("enableDiamondSparkles").boolValue = true;
            so.FindProperty("enableCrossSparkles").boolValue = true;
            so.FindProperty("sparkleQuantity").floatValue = 6.0f;
            so.FindProperty("sparkleSize").floatValue = 0.035f;
            so.FindProperty("skullBloomIntensity").floatValue = 4.0f;
            so.FindProperty("sparkleBloomIntensity").floatValue = 3.0f;
            so.ApplyModifiedProperties();
            effect.InitializeEffect();
            effect.Play();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    [MenuItem("GameObject/Effects/Miasma Affected Enemy VFX", false, 11)]
    public static void CreateAffectedEnemyVFX(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("Miasma_Affected_Enemy");
        MiasmaEnemyEffectVFX effect = go.AddComponent<MiasmaEnemyEffectVFX>();
        effect.InitializeEffect();
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create Miasma Affected Enemy VFX");
        Selection.activeObject = go;
    }
}
