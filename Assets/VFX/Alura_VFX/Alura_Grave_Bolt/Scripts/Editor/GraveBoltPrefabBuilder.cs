using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utility to construct the complete Grave_Bolt_Ability prefab,
/// wire up identical Start and End blasts, straight arrow shaft, and rotating helix spark rope.
/// </summary>
public static class GraveBoltPrefabBuilder
{
    private const string PrefabPath = "Assets/VFX/Alura_VFX/Alura_Grave_Bolt/Prefabs/Grave_Bolt_Ability.prefab";

    [InitializeOnLoadMethod]
    private static void OnEditorLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying) return;
            bool needsRebuild = !System.IO.File.Exists(PrefabPath);
            if (!needsRebuild)
            {
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (asset == null ||
                    asset.transform.Find("1_Cast_Hand_Anchor/Start_Asterisk_Blast") == null ||
                    asset.transform.Find("1_Cast_Hand_Anchor/Start_Glow_Ball") == null ||
                    asset.transform.Find("1_Cast_Hand_Anchor/Start_Gathering_Sparkles") == null ||
                    asset.transform.Find("1_Cast_Hand_Anchor/Start_Sparkle_Motes") == null ||
                    asset.transform.Find("1_Cast_Hand_Anchor/Start_Skull_Motes") != null ||
                    asset.transform.Find("2_Jagged_Bolt_Beam/Snake_Rope_1") == null ||
                    asset.transform.Find("2_Jagged_Bolt_Beam/Snake_Rope_2") == null ||
                    asset.transform.Find("2_Jagged_Bolt_Beam/Snake_Rope_3") == null ||
                    asset.transform.Find("3_Target_Impact_Anchor/End_Asterisk_Blast") == null ||
                    asset.transform.Find("3_Target_Impact_Anchor/End_Glow_Ball") == null ||
                    asset.transform.Find("3_Target_Impact_Anchor/End_Skull_Motes") == null ||
                    asset.transform.Find("3_Target_Impact_Anchor/End_Sparkle_Motes") == null ||
                    asset.transform.Find("2_Jagged_Bolt_Beam/Rotating_Helix_Rope") != null ||
                    asset.transform.Find("3_Target_Impact_Anchor/Impact_Jagged_Arcs") != null)
                {
                    needsRebuild = true;
                }
            }

            if (needsRebuild)
            {
                BuildGraveBoltPrefabAndSetupScene();
            }
        };
    }

    [MenuItem("Tools/VFX/Build Grave Bolt Prefab & Scene Setup")]
    public static void BuildGraveBoltPrefabAndSetupScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
        {
            Debug.LogWarning("[Grave Bolt VFX] Cannot build prefab or modify scene during Play Mode.");
            return;
        }

        string directory = System.IO.Path.GetDirectoryName(PrefabPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        GameObject abilityGO = GameObject.Find("Grave_Bolt_Ability");
        if (abilityGO != null && PrefabUtility.IsPartOfPrefabInstance(abilityGO))
        {
            PrefabUtility.UnpackPrefabInstance(abilityGO, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        if (abilityGO == null)
        {
            abilityGO = new GameObject("Grave_Bolt_Ability");
            abilityGO.transform.position = Vector3.zero;
            abilityGO.transform.rotation = Quaternion.identity;
        }

        // Remove any legacy objects directly from scene object
        string[] badNames = new string[] {
            "Start_Skull_Motes",
            "Impact_Jagged_Arcs",
            "Impact_Ground_Crawlers",
            "Impact_Stylized_Burst",
            "Leading_Edge_Head",
            "Palm_Core_Glow",
            "Impact_Mist",
            "Escort_Skull_Motes",
            "Electric_Branch_Arc",
            "Electric_Spearhead",
            "Muzzle_Flash",
            "Impact_Flash",
            "Escort_Skulls",
            "Impact_Skulls",
            "Traveling_Flame",
            "Flame_Power_Trail",
            "Escort_Sparkles",
            "Charge_Inward_Suction",
            "Charge_Sparkles",
            "Hand_Flame",
            "Impact_Flame",
            "Impact_Sparks",
            "Rotating_Helix_Rope"
        };

        foreach (var t in abilityGO.GetComponentsInChildren<Transform>(true))
        {
            if (t == null || t.gameObject == abilityGO) continue;
            for (int i = 0; i < badNames.Length; i++)
            {
                if (t.name.Equals(badNames[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    Object.DestroyImmediate(t.gameObject);
                    break;
                }
            }
        }

        GraveBoltVFX vfx = abilityGO.GetComponent<GraveBoltVFX>();
        if (vfx == null) vfx = abilityGO.AddComponent<GraveBoltVFX>();

        vfx.AutoFindReferences(true);
        vfx.InitializeComponents();
        vfx.ConfigureAllParticleSystems();

        CleanDuplicatesUnderAnchor(abilityGO.transform.Find("1_Cast_Hand_Anchor"));
        CleanDuplicatesUnderAnchor(abilityGO.transform.Find("2_Jagged_Bolt_Beam"));
        CleanDuplicatesUnderAnchor(abilityGO.transform.Find("3_Target_Impact_Anchor"));

        if (vfx.LightningBolt != null)
        {
            vfx.LightningBolt.CleanupStrayChildren();
            if (vfx.LightningBolt.Line != null)
            {
                vfx.LightningBolt.Line.enabled = false;
                if (vfx.LightningBolt.Line.positionCount == 0)
                {
                    vfx.LightningBolt.Line.positionCount = 2;
                    vfx.LightningBolt.Line.SetPositions(new Vector3[] { abilityGO.transform.position, abilityGO.transform.position });
                }
            }
        }

        // Save as Prefab
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(abilityGO, PrefabPath, InteractionMode.AutomatedAction);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = abilityGO;

        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.isLoaded && !Application.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log($"<color=#40FF73><b>[Grave Bolt VFX]</b> Successfully built straight arrow, rotating helix rope, and identical Start & End blasts to {PrefabPath}!</color>");
    }

    private static void CleanDuplicatesUnderAnchor(Transform parent)
    {
        if (parent == null) return;
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child == null) continue;
            if (seen.Contains(child.name))
            {
                Object.DestroyImmediate(child.gameObject);
            }
            else
            {
                seen.Add(child.name);
            }
        }
    }
}
