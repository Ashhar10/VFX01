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

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("⚡ Quick Presets (Design Guide)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Miasma Wind-Up (Official)"))
        {
            Undo.RecordObject(smoke, "Apply Miasma Wind-Up Preset");
            SerializedObject so = new SerializedObject(smoke);
            so.FindProperty("vortexRadius").floatValue = 1.7f;
            so.FindProperty("vortexHeight").floatValue = 2.2f;
            so.FindProperty("rotationSpeed").floatValue = 55f;
            so.FindProperty("upwardSpeed").floatValue = 0.35f;
            so.FindProperty("particleScale").floatValue = 1.0f;
            so.FindProperty("densityMultiplier").floatValue = 1.0f;
            so.FindProperty("deepCharcoalPurple").colorValue = new Color(0.12f, 0.05f, 0.18f, 0.35f);
            so.FindProperty("midtoneViolet").colorValue = new Color(0.42f, 0.18f, 0.58f, 0.45f);
            so.FindProperty("rimHighlightLilac").colorValue = new Color(0.68f, 0.35f, 0.88f, 0.60f);
            so.FindProperty("necroticGreenColor").colorValue = new Color(0.35f, 1.0f, 0.70f, 1.0f);
            so.FindProperty("boneWhiteColor").colorValue = new Color(0.92f, 1.0f, 0.98f, 1.0f);
            so.ApplyModifiedProperties();
            smoke.InitializeEffect();
            smoke.Play();
        }
        if (GUILayout.Button("Subtle Wispy Fog"))
        {
            Undo.RecordObject(smoke, "Apply Subtle Wispy Preset");
            SerializedObject so = new SerializedObject(smoke);
            so.FindProperty("vortexRadius").floatValue = 1.5f;
            so.FindProperty("vortexHeight").floatValue = 1.9f;
            so.FindProperty("rotationSpeed").floatValue = 40f;
            so.FindProperty("upwardSpeed").floatValue = 0.25f;
            so.FindProperty("particleScale").floatValue = 0.85f;
            so.FindProperty("densityMultiplier").floatValue = 0.75f;
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
