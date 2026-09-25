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
        EditorGUILayout.LabelField("💨 Stage [4] Miasma Fog Diffusion & Board Return VFX", titleStyle);
        EditorGUILayout.LabelField("Image 4: After 3 turns the fog dissipates. Particles fade. The board returns to normal.", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);

        // Transport Controls
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("▶ Play", GUILayout.Height(30)))
        {
            dissipation.Play();
        }

        GUI.backgroundColor = new Color(0.9f, 0.5f, 0.3f);
        if (GUILayout.Button("⏸ Stop", GUILayout.Height(30)))
        {
            dissipation.Stop();
        }

        GUI.backgroundColor = new Color(0.5f, 0.7f, 1.0f);
        if (GUILayout.Button("🔄 Rebuild Layers", GUILayout.Height(30)))
        {
            dissipation.InitializeEffect();
            dissipation.Play();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // 3-Turn Dissipation Controls
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎲 3-Turn Dissipation Step Controller (Image 4)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Step through the turns or animate continuously:\n" +
            "• Turn 1: Fog lowers and pools toward the floor.\n" +
            "• Turn 2: Mist crawls radially across grid tiles (Ground Diffusion).\n" +
            "• Turn 3: Particles fade out into thin air, returning board to normal.",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = dissipation.CurrentTurn == 1 ? new Color(0.6f, 0.85f, 1.0f) : Color.white;
        if (GUILayout.Button("Turn 1 (Thinning)", GUILayout.Height(28)))
        {
            Undo.RecordObject(dissipation, "Set Turn 1");
            dissipation.CurrentTurn = 1;
        }

        GUI.backgroundColor = dissipation.CurrentTurn == 2 ? new Color(0.8f, 0.7f, 1.0f) : Color.white;
        if (GUILayout.Button("Turn 2 (Floor Crawl)", GUILayout.Height(28)))
        {
            Undo.RecordObject(dissipation, "Set Turn 2");
            dissipation.CurrentTurn = 2;
        }

        GUI.backgroundColor = dissipation.CurrentTurn == 3 ? new Color(0.6f, 0.95f, 0.6f) : Color.white;
        if (GUILayout.Button("Turn 3 (Board Clear)", GUILayout.Height(28)))
        {
            Undo.RecordObject(dissipation, "Set Turn 3");
            dissipation.CurrentTurn = 3;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.5f, 0.85f, 1.0f);
        if (GUILayout.Button("💨 Trigger Smooth 3-Turn Dissipation", GUILayout.Height(30)))
        {
            dissipation.TriggerDissipation();
        }

        GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
        if (GUILayout.Button("↺ Reset Board (Full Fog)", GUILayout.Height(30)))
        {
            Undo.RecordObject(dissipation, "Reset Board");
            dissipation.ResetBoard();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // Standard fields
        DrawDefaultInspector();

        // Z-Axis Directional Sweep & Tail Deformation Section
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🏹 Z-Axis Directional Sweep & Tail Deformation (Image 4)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "When dissipation triggers, particles move along the Z axis (following the red arrow), " +
            "deform into aerodynamic elongated smoke tails, disperse laterally, and vanish completely:\n" +
            "• Direction Mode: Diagonal_Z_RedArrow (+Z / +X grid diagonal), Forward_Z (+Z), or Custom.\n" +
            "• Tail Deformation: Dynamically stretches billboard puffs into elongated trailing wisps.\n" +
            "• Complete Vanish: All clouds, wisps, and motes fade out and vanish, clearing the board.",
            MessageType.Info
        );

        EditorGUI.BeginChangeCheck();
        MiasmaDissipationVFX.DissipationDirectionMode newMode = (MiasmaDissipationVFX.DissipationDirectionMode)EditorGUILayout.EnumPopup("Direction Mode", dissipation.DirectionMode);
        Vector3 newCustomDir = dissipation.CustomDirection;
        if (newMode == MiasmaDissipationVFX.DissipationDirectionMode.Custom_Direction)
        {
            newCustomDir = EditorGUILayout.Vector3Field("Custom Direction Vector", dissipation.CustomDirection);
        }

        float newSweepSpeed = EditorGUILayout.Slider("Z-Axis Sweep Speed", dissipation.ZSweepSpeed, 0f, 15f);
        bool newTailDeform = EditorGUILayout.Toggle("Enable Tail Deformation", dissipation.EnableTailDeformation);
        float newTailScale = EditorGUILayout.Slider("Tail Stretch (Velocity Scale)", dissipation.TailDeformationScale, 0.5f, 6.0f);
        float newTailLength = EditorGUILayout.Slider("Tail Length (Length Scale)", dissipation.TailLengthScale, 0.5f, 5.0f);
        float newDispersion = EditorGUILayout.Slider("Lateral Dispersion", dissipation.LateralDispersion, 0f, 5.0f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(dissipation, "Change Z-Axis Sweep Settings");
            dissipation.DirectionMode = newMode;
            dissipation.CustomDirection = newCustomDir;
            dissipation.ZSweepSpeed = newSweepSpeed;
            dissipation.EnableTailDeformation = newTailDeform;
            dissipation.TailDeformationScale = newTailScale;
            dissipation.TailLengthScale = newTailLength;
            dissipation.LateralDispersion = newDispersion;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.85f, 1.0f);
        if (GUILayout.Button("💨 Trigger Z-Axis Tail Dissipation", GUILayout.Height(32)))
        {
            dissipation.TriggerDissipation();
        }

        GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
        if (GUILayout.Button("↺ Reset Board (Full Fog)", GUILayout.Height(32)))
        {
            Undo.RecordObject(dissipation, "Reset Board");
            dissipation.ResetBoard();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("✨ Subtle Lingering Sparkles", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Slight ambient star motes that fade as the fog disperses. User-friendly single sliders.", MessageType.None);

        EditorGUI.BeginChangeCheck();
        float newQty = EditorGUILayout.Slider("Sparkle Quantity (0 = OFF)", dissipation.SparkleQuantity, 0f, 10f);
        float newSize = EditorGUILayout.Slider("Sparkle Size", dissipation.SparkleSize, 0.01f, 0.06f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(dissipation, "Change Sparkles");
            dissipation.SparkleQuantity = newQty;
            dissipation.SparkleSize = newSize;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🌸 Reset to Subtle Motes (Default)"))
        {
            Undo.RecordObject(dissipation, "Set Default Sparkles");
            dissipation.SparkleQuantity = 3.0f;
            dissipation.SparkleSize = 0.028f;
        }
        if (GUILayout.Button("⭕ Turn Off Sparkles"))
        {
            Undo.RecordObject(dissipation, "Turn Off Sparkles");
            dissipation.SparkleQuantity = 0f;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }
}
