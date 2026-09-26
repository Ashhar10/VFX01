using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// ArthurVFXPrefabBuilder - Editor utility to build and save prefabs for:
/// 1. King_Arthur_Enemy_Contact_VFX
/// 2. King_Arthur_Enemy_Mark_VFX
/// 3. King_Arthur_Ally_Shield_VFX
/// Access via menu: Tools > King Arthur VFX > Build All Prefabs
/// </summary>
public static class ArthurVFXPrefabBuilder
{
    private const string PREFAB_DIR = "Assets/VFX/King Arthur_VFX/Prefabs";

    [MenuItem("Tools/King Arthur VFX/Build All Prefabs")]
    public static void BuildAllPrefabs()
    {
        EnsurePrefabDirectory();

        BuildEnemyContactPrefab();
        BuildEnemyMarkPrefab();
        BuildAllyShieldPrefab();
        BuildDefensiveStancePrefab();
        BuildKingdomClaimPrefab();
        ApplySwordBloomSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Arthur VFX] All King Arthur VFX prefabs created successfully in " + PREFAB_DIR);
    }

    private static void EnsurePrefabDirectory()
    {
        if (!AssetDatabase.IsValidFolder("Assets/VFX/King Arthur_VFX/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets/VFX/King Arthur_VFX", "Prefabs");
        }
    }

    [MenuItem("Tools/King Arthur VFX/Build Enemy Contact VFX Prefab")]
    public static void BuildEnemyContactPrefab()
    {
        EnsurePrefabDirectory();
        string path = Path.Combine(PREFAB_DIR, "King_Arthur_Enemy_Contact_VFX.prefab").Replace("\\", "/");

        GameObject root = new GameObject("King_Arthur_Enemy_Contact_VFX");
        ArthurEnemyContactVFX comp = root.AddComponent<ArthurEnemyContactVFX>();
        comp.ApplySettings();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("[Arthur VFX] Saved prefab: " + path);
    }

    [MenuItem("Tools/King Arthur VFX/Build Enemy Mark VFX Prefab")]
    public static void BuildEnemyMarkPrefab()
    {
        EnsurePrefabDirectory();
        string path = Path.Combine(PREFAB_DIR, "King_Arthur_Enemy_Mark_VFX.prefab").Replace("\\", "/");

        GameObject root = new GameObject("King_Arthur_Enemy_Mark_VFX");
        ArthurEnemyMarkVFX comp = root.AddComponent<ArthurEnemyMarkVFX>();
        comp.ApplySettings();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("[Arthur VFX] Saved prefab: " + path);
    }

    [MenuItem("Tools/King Arthur VFX/Build Ally Shield VFX Prefab")]
    public static void BuildAllyShieldPrefab()
    {
        EnsurePrefabDirectory();
        string path = Path.Combine(PREFAB_DIR, "King_Arthur_Ally_Shield_VFX.prefab").Replace("\\", "/");

        GameObject root = new GameObject("King_Arthur_Ally_Shield_VFX");
        ArthurAllyShieldVFX comp = root.AddComponent<ArthurAllyShieldVFX>();
        comp.ApplySettings();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("[Arthur VFX] Saved prefab: " + path);
    }

    [MenuItem("Tools/King Arthur VFX/Build Defensive Stance VFX Prefab")]
    public static void BuildDefensiveStancePrefab()
    {
        EnsurePrefabDirectory();
        string path = Path.Combine(PREFAB_DIR, "King_Arthur_Defensive_Stance_VFX.prefab").Replace("\\", "/");

        GameObject root = new GameObject("King_Arthur_Defensive_Stance_VFX");
        ArthurDefensiveStanceVFX comp = root.AddComponent<ArthurDefensiveStanceVFX>();
        comp.InitializeVFX();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("[Arthur VFX] Saved prefab: " + path);
    }

    [MenuItem("Tools/King Arthur VFX/Build Kingdom Claim (Needles) VFX Prefab")]
    public static void BuildKingdomClaimPrefab()
    {
        EnsurePrefabDirectory();
        string path = Path.Combine(PREFAB_DIR, "King_Arthur_Kingdom_Claim_VFX.prefab").Replace("\\", "/");

        GameObject root = new GameObject("King_Arthur_Kingdom_Claim_VFX");
        ArthurKingdomClaimVFX comp = root.AddComponent<ArthurKingdomClaimVFX>();
        comp.InitializeVFX();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("[Arthur VFX] Saved prefab: " + path);
    }

    [MenuItem("Tools/King Arthur VFX/Apply Sword Bloom Settings")]
    public static void ApplySwordBloomSettings()
    {
        string path = Path.Combine(PREFAB_DIR, "sword.prefab").Replace("\\", "/");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            Transform vfxT = prefab.transform.Find("VFX");
            if (vfxT != null)
            {
                ArthurSwordBloomVFX bloom = vfxT.GetComponent<ArthurSwordBloomVFX>();
                if (bloom == null) bloom = vfxT.gameObject.AddComponent<ArthurSwordBloomVFX>();
                bloom.AutoLinkComponents();
                bloom.ApplyBloomSettings();
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                Debug.Log("[Arthur VFX] Sword Bloom VFX applied successfully to " + path);
            }
        }
    }
}
