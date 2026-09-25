using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utility to construct and save the standalone Grave_Cloudy_Skull_VFX prefab,
/// and optionally instantiate it onto target enemy in the scene.
/// </summary>
public static class CloudySkullPrefabBuilder
{
    public const string PrefabPath = "Assets/VFX/Alura_VFX/Alura_Grave_Bolt/Prefabs/Grave_Cloudy_Skull_VFX.prefab";

    [InitializeOnLoadMethod]
    private static void OnEditorLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying) return;
            if (!System.IO.File.Exists(PrefabPath))
            {
                BuildCloudySkullPrefab();
            }
        };
    }

    [MenuItem("Tools/VFX/Build Cloudy Light Skull Prefab")]
    public static void BuildCloudySkullPrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
        {
            Debug.LogWarning("[CloudySkullVFX] Cannot build prefab during Play Mode.");
            return;
        }

        string directory = System.IO.Path.GetDirectoryName(PrefabPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        // Create temporary GameObject in scene to assemble
        GameObject vfxGO = new GameObject("Grave_Cloudy_Skull_VFX");
        vfxGO.transform.position = Vector3.zero;
        vfxGO.transform.rotation = Quaternion.identity;

        CloudySkullVFX vfx = vfxGO.AddComponent<CloudySkullVFX>();
        vfx.InitializeComponents();
        vfx.ApplySettingsToAllSystems();

        // Check if Miasma_Affected_Enemy exists to set as default reference if present
        GameObject enemy = GameObject.Find("Miasma_Affected_Enemy");
        if (enemy != null)
        {
            vfx.Target = enemy.transform;
            vfxGO.transform.position = enemy.transform.position;
        }

        // Save as prefab
        PrefabUtility.SaveAsPrefabAssetAndConnect(vfxGO, PrefabPath, InteractionMode.AutomatedAction);
        Debug.Log($"[CloudySkullVFX] Successfully created standalone prefab at: {PrefabPath}");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("GameObject/Effects/Grave VFX/Spawn Cloudy Light Skull VFX", false, 10)]
    public static void SpawnInScene(MenuCommand menuCommand)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            BuildCloudySkullPrefab();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            GameObject parent = menuCommand.context as GameObject;
            if (parent != null)
            {
                instance.transform.SetParent(parent.transform, false);
            }
            else
            {
                // Try to find Miasma_Affected_Enemy
                GameObject enemy = GameObject.Find("Miasma_Affected_Enemy");
                if (enemy != null)
                {
                    instance.transform.position = enemy.transform.position;
                    CloudySkullVFX comp = instance.GetComponent<CloudySkullVFX>();
                    if (comp != null) comp.Target = enemy.transform;
                }
            }

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Cloudy Light Skull VFX");
            Selection.activeGameObject = instance;
        }
    }
}
