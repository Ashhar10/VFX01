using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// ArthurPrefabAutoLinker - Runs inside Unity Editor to guarantee that
/// all GroundSlash materials, shaders, textures, and bloom settings are
/// permanently serialized on King Arthur.prefab so Unity's "Export Package"
/// dependency collector detects and includes 100% of required assets automatically.
/// </summary>
[InitializeOnLoad]
public static class ArthurPrefabAutoLinker
{
    private const string PREFAB_PATH = "Assets/Prefabs/King Arthur.prefab";
    private const string MAT_DIR = "Assets/VFX/King Arthur_VFX/Materials/";
    private const string TEX_DIR = "Assets/VFX/King Arthur_VFX/Textures/";

    static ArthurPrefabAutoLinker()
    {
        EditorApplication.delayCall += LinkAllKingArthurDependencies;
    }

    [MenuItem("Tools/King Arthur VFX/Link All Dependencies on King Arthur Prefab")]
    public static void LinkAllKingArthurDependencies()
    {
        if (!File.Exists(PREFAB_PATH))
        {
            Debug.LogWarning("[Arthur VFX] Prefab not found at " + PREFAB_PATH);
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        if (root == null) return;

        bool isDirty = false;

        // Find all GroundSlashVFX components (especially on GroundLevel)
        GroundSlashVFX[] groundSlashes = root.GetComponentsInChildren<GroundSlashVFX>(true);
        foreach (var gs in groundSlashes)
        {
            SerializedObject so = new SerializedObject(gs);

            Material vertBladeMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "KingArthur_GroundSlash_VerticalBlade_Mat.mat");
            Material horizWaveMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "KingArthur_GroundSlash_HorizontalWave_Mat.mat");
            Material fissureMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "KingArthur_GroundSlash_Fissure_Mat.mat");
            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "KingArthur_GroundSlash_Rock_Mat.mat");
            Material sparkMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "KingArthur_GroundSlash_Sparks_Mat.mat");
            Material vaporMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "KingArthur_GroundSlash_Vapor_Mat.mat");
            Material distortMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "KingArthur_GroundSlash_DistortionRing_Mat.mat");

            Texture2D crescentTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + "CrescentSlash.png");
            Texture2D sparkTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + "Spark.png");
            Texture2D vaporTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + "SoftCircle.png");

            SetObjectRef(so, "verticalBladeMat", vertBladeMat, ref isDirty);
            SetObjectRef(so, "horizontalWaveMat", horizWaveMat, ref isDirty);
            SetObjectRef(so, "groundFissureMat", fissureMat, ref isDirty);
            SetObjectRef(so, "rockLitMat", rockMat, ref isDirty);
            SetObjectRef(so, "sparkMat", sparkMat, ref isDirty);
            SetObjectRef(so, "vaporMat", vaporMat, ref isDirty);
            SetObjectRef(so, "distortionRingMat", distortMat, ref isDirty);

            SetObjectRef(so, "crescentTexture", crescentTex, ref isDirty);
            SetObjectRef(so, "sparkTexture", sparkTex, ref isDirty);
            SetObjectRef(so, "vaporTexture", vaporTex, ref isDirty);

            // Turn off dynamic ground path lights to eliminate URP Deferred tile artifact
            SetBool(so, "enableGroundPathLights", false, ref isDirty);
            SetBool(so, "controlSceneGlobalBloom", true, ref isDirty);
            SetFloat(so, "slashBloomIntensity", 0.65f, ref isDirty);
            SetFloat(so, "sceneBloomIntensity", 0.65f, ref isDirty);
            SetFloat(so, "sceneBloomScatter", 0.65f, ref isDirty);

            if (isDirty)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        if (isDirty)
        {
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log("[Arthur VFX] Successfully hard-linked all GroundSlash materials, shaders, and textures to King Arthur.prefab!");
        }

        PrefabUtility.UnloadPrefabContents(root);
    }

    [MenuItem("Tools/King Arthur VFX/Export Complete King Arthur Package (.unitypackage)")]
    public static void ExportCompletePackage()
    {
        LinkAllKingArthurDependencies();

        string[] exportPaths = new string[]
        {
            "Assets/Prefabs/King Arthur.prefab",
            "Assets/VFX/King Arthur_VFX",
            "Assets/Animation/King Arthur",
            "Assets/FBX/sword.obj",
            "Assets/FBX/SwordTex",
            "Assets/Shaders/SlashShader_Additive.shadergraph",
            "Assets/Shaders/SlashShader_Alpha.shadergraph",
            "Assets/Visual Graph/VFXgraph_King_Arthur_Divine_Right_Animation.vfx",
            "Assets/Visual Graph/VFXgraph_King_Arthur_Peasant_Correction.vfx"
        };

        string exportFile = EditorUtility.SaveFilePanel("Export King Arthur Package", "", "KingArthur_Complete_VFX.unitypackage", "unitypackage");
        if (!string.IsNullOrEmpty(exportFile))
        {
            AssetDatabase.ExportPackage(exportPaths, exportFile, ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
            EditorUtility.DisplayDialog("Export Successful", "King Arthur package exported successfully to:\n" + exportFile, "OK");
        }
    }

    private static void SetObjectRef(SerializedObject so, string propName, Object obj, ref bool isDirty)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null && p.objectReferenceValue != obj)
        {
            p.objectReferenceValue = obj;
            isDirty = true;
        }
    }

    private static void SetBool(SerializedObject so, string propName, bool val, ref bool isDirty)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null && p.boolValue != val)
        {
            p.boolValue = val;
            isDirty = true;
        }
    }

    private static void SetFloat(SerializedObject so, string propName, float val, ref bool isDirty)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null && !Mathf.Approximately(p.floatValue, val))
        {
            p.floatValue = val;
            isDirty = true;
        }
    }
}
