using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector and Scene View GUI for CloudySkullVFX.
/// Provides live preview, layer controls, and quick-setup actions.
/// </summary>
[CustomEditor(typeof(CloudySkullVFX))]
public class CloudySkullVFXEditor : Editor
{
    private CloudySkullVFX vfx;

    private void OnEnable()
    {
        vfx = (CloudySkullVFX)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawBanner();
        DrawPlaybackControls();

        EditorGUILayout.Space(8);
        DrawPropertiesExcluding(serializedObject, "m_Script");

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            vfx.ApplySettingsToAllSystems();
            EditorUtility.SetDirty(vfx);
        }
    }

    private void DrawBanner()
    {
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.35f, 1.0f, 0.65f) }
        };

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("✦ CLOUDY LIGHT & SKULL VFX ✦", titleStyle);
        EditorGUILayout.LabelField("Volumetric Cloudy Light • Floating Spectral Skulls • Ethereal Motes", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawPlaybackControls()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Interactive Playback Preview", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
        if (GUILayout.Button("► Play", GUILayout.Height(28)))
        {
            vfx.Play();
        }

        GUI.backgroundColor = new Color(1.0f, 0.4f, 0.3f);
        if (GUILayout.Button("■ Stop", GUILayout.Height(28)))
        {
            vfx.Stop();
        }

        GUI.backgroundColor = new Color(0.4f, 0.8f, 1.0f);
        if (GUILayout.Button("↺ Restart", GUILayout.Height(28)))
        {
            vfx.Restart();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fade Out Smoothly"))
        {
            vfx.FadeOut();
        }

        if (GUILayout.Button("Find Target in Scene"))
        {
            GameObject enemy = GameObject.Find("Miasma_Affected_Enemy");
            if (enemy != null)
            {
                Undo.RecordObject(vfx, "Assign Target");
                vfx.Target = enemy.transform;
                EditorUtility.SetDirty(vfx);
                Debug.Log("[CloudySkullVFX] Assigned target to 'Miasma_Affected_Enemy'.");
            }
            else
            {
                Debug.LogWarning("[CloudySkullVFX] 'Miasma_Affected_Enemy' not found in active scene.");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void OnSceneGUI()
    {
        if (vfx == null) return;

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(12, 12, 220, 110), EditorStyles.helpBox);
        GUILayout.Label("Cloudy Light & Skull VFX", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
        if (GUILayout.Button("► Play", GUILayout.Height(24)))
        {
            vfx.Play();
        }

        GUI.backgroundColor = new Color(1.0f, 0.4f, 0.3f);
        if (GUILayout.Button("■ Stop", GUILayout.Height(24)))
        {
            vfx.Stop();
        }

        GUI.backgroundColor = new Color(0.4f, 0.8f, 1.0f);
        if (GUILayout.Button("↺ Restart", GUILayout.Height(24)))
        {
            vfx.Restart();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Fade Out"))
        {
            vfx.FadeOut();
        }

        GUILayout.EndArea();
        Handles.EndGUI();
    }
}
