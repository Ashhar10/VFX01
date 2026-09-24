using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SetupMiasmaScene
{
    [MenuItem("Tools/VFX Setup/Configure Miasma Smoke in Scene")]
    public static void ExecuteSetup()
    {
        GameObject smokeGO = GameObject.Find("Necromantic_Rotating_Smoke");
        if (smokeGO == null)
        {
            GameObject vfxRoot = GameObject.Find("VFX");
            if (vfxRoot != null)
            {
                smokeGO = new GameObject("Necromantic_Rotating_Smoke");
                smokeGO.transform.SetParent(vfxRoot.transform, false);
                smokeGO.transform.localPosition = Vector3.zero;
                smokeGO.transform.localRotation = Quaternion.identity;
                smokeGO.transform.localScale = Vector3.one;
            }
        }

        if (smokeGO != null)
        {
            RotatingSmokeVFX smoke = smokeGO.GetComponent<RotatingSmokeVFX>();
            if (smoke == null)
            {
                smoke = smokeGO.AddComponent<RotatingSmokeVFX>();
            }

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

            so.FindProperty("enableBackdropFog").boolValue = true;
            so.FindProperty("enableVortexWisps").boolValue = true;
            so.FindProperty("enableGroundMist").boolValue = true;
            so.FindProperty("enableSoulOrbs").boolValue = true;
            so.FindProperty("enableEnergyMotes").boolValue = true;

            Material plumeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticSmokeMat.mat");
            Material wispMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticWispMat.mat");
            Material orbMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSoulOrbMat.mat");
            Material moteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaEnergyMoteMat.mat");

            so.FindProperty("smokePlumeMaterial").objectReferenceValue = plumeMat;
            so.FindProperty("smokeWispMaterial").objectReferenceValue = wispMat;
            so.FindProperty("soulOrbMaterial").objectReferenceValue = orbMat;
            so.FindProperty("energyMoteMaterial").objectReferenceValue = moteMat;

            so.ApplyModifiedProperties();

            smoke.InitializeEffect();
            smoke.Play();

            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(smokeGO);
                EditorSceneManager.MarkSceneDirty(smokeGO.scene);
                EditorSceneManager.SaveScene(smokeGO.scene);
            }
            Debug.Log("<color=#74EBD5>[MiasmaVFX] Successfully configured and saved Miasma Smoke in Scene!</color>");
        }
    }
}
