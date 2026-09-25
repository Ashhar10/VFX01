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
            so.FindProperty("enableSparkles").boolValue = true;
            so.FindProperty("enableDiamondSparkles").boolValue = true;
            so.FindProperty("enableCrossSparkles").boolValue = true;
            so.FindProperty("sparkleQuantity").floatValue = 5.0f;
            so.FindProperty("sparkleSize").floatValue = 0.032f;
            so.FindProperty("sparkleBloomIntensity").floatValue = 2.5f;

            Material plumeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticSmokeMat.mat");
            Material wispMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticWispMat.mat");
            Material orbMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSoulOrbMat.mat");
            Material diamondMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSparkleDiamondMat.mat");
            Material crossMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSparkleCrossMat.mat");

            so.FindProperty("smokePlumeMaterial").objectReferenceValue = plumeMat;
            so.FindProperty("smokeWispMaterial").objectReferenceValue = wispMat;
            so.FindProperty("soulOrbMaterial").objectReferenceValue = orbMat;
            so.FindProperty("diamondSparkleMaterial").objectReferenceValue = diamondMat;
            so.FindProperty("crossSparkleMaterial").objectReferenceValue = crossMat;
            so.FindProperty("energyMoteMaterial").objectReferenceValue = diamondMat;

            so.ApplyModifiedProperties();

            smoke.InitializeEffect();
            smoke.Play();

            if (!Application.isPlaying)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                PrefabUtility.SaveAsPrefabAssetAndConnect(smokeGO, "Assets/Prefabs/Necromantic_Rotating_Smoke.prefab", InteractionMode.AutomatedAction);
                EditorUtility.SetDirty(smokeGO);
                EditorSceneManager.MarkSceneDirty(smokeGO.scene);
                EditorSceneManager.SaveScene(smokeGO.scene);
            }
            Debug.Log("<color=#74EBD5>[MiasmaVFX] Successfully configured and saved Miasma Smoke & Prefab in Scene with subtle sparkles!</color>");
        }
    }

    [MenuItem("Tools/VFX Setup/Spawn Affected Enemy VFX in Scene")]
    public static void ExecuteAffectedEnemySetup()
    {
        GameObject enemyGO = GameObject.Find("Miasma_Affected_Enemy");
        if (enemyGO == null)
        {
            GameObject vfxRoot = GameObject.Find("VFX");
            enemyGO = new GameObject("Miasma_Affected_Enemy");
            if (vfxRoot != null)
            {
                enemyGO.transform.SetParent(vfxRoot.transform, false);
            }
            enemyGO.transform.localPosition = new Vector3(2.5f, 0f, 0f); // Offset so it's clearly visible beside Alura
            enemyGO.transform.localRotation = Quaternion.identity;
            enemyGO.transform.localScale = Vector3.one;
        }

        MiasmaEnemyEffectVFX effect = enemyGO.GetComponent<MiasmaEnemyEffectVFX>();
        if (effect == null)
        {
            effect = enemyGO.AddComponent<MiasmaEnemyEffectVFX>();
        }

        SerializedObject so = new SerializedObject(effect);
        so.FindProperty("bodyRadius").floatValue = 0.65f;
        so.FindProperty("bodyHeight").floatValue = 1.8f;
        so.FindProperty("swirlSpeed").floatValue = 50f;
        so.FindProperty("upwardSpeed").floatValue = 0.35f;
        so.FindProperty("enableSkullMask").boolValue = true;
        so.FindProperty("skullHeightOffset").floatValue = 1.35f;
        so.FindProperty("skullScale").floatValue = 0.65f;
        so.FindProperty("skullPulseSpeed").floatValue = 2.5f;
        so.FindProperty("skullColor").colorValue = new Color(0.96f, 0.98f, 0.92f, 0.95f);
        so.FindProperty("enableBodyMist").boolValue = true;
        so.FindProperty("mistDensity").floatValue = 1.0f;
        so.FindProperty("mistParticleScale").floatValue = 0.9f;
        so.FindProperty("enableWaistRing").boolValue = true;
        so.FindProperty("waistRingHeight").floatValue = 0.85f;
        so.FindProperty("waistRingRadius").floatValue = 0.75f;
        so.FindProperty("enableSparkles").boolValue = true;
        so.FindProperty("enableDiamondSparkles").boolValue = true;
        so.FindProperty("enableCrossSparkles").boolValue = true;
        so.FindProperty("sparkleQuantity").floatValue = 4.0f;
        so.FindProperty("sparkleSize").floatValue = 0.028f;
        so.FindProperty("skullBloomIntensity").floatValue = 3.5f;
        so.FindProperty("sparkleBloomIntensity").floatValue = 2.5f;
        so.FindProperty("boneWhiteColor").colorValue = new Color(0.96f, 0.93f, 0.88f, 1.0f);
        so.FindProperty("lavenderColor").colorValue = new Color(0.75f, 0.60f, 0.90f, 1.0f);
        so.FindProperty("spectralTint").colorValue = new Color(0.78f, 0.95f, 0.82f, 1.0f);

        Material skullMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSpectralSkullMat.mat");
        Material plumeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticSmokeMat.mat");
        Material wispMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticWispMat.mat");
        Material diamondMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSparkleDiamondMat.mat");
        Material crossMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSparkleCrossMat.mat");

        so.FindProperty("skullMaterial").objectReferenceValue = skullMat;
        so.FindProperty("smokePlumeMaterial").objectReferenceValue = plumeMat;
        so.FindProperty("smokeWispMaterial").objectReferenceValue = wispMat;
        so.FindProperty("diamondSparkleMaterial").objectReferenceValue = diamondMat;
        so.FindProperty("crossSparkleMaterial").objectReferenceValue = crossMat;
        so.FindProperty("energyMoteMaterial").objectReferenceValue = diamondMat;
        so.ApplyModifiedProperties();

        effect.InitializeEffect();
        effect.Play();

        if (!Application.isPlaying)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            PrefabUtility.SaveAsPrefabAssetAndConnect(enemyGO, "Assets/Prefabs/Miasma_Affected_Enemy.prefab", InteractionMode.AutomatedAction);
            EditorUtility.SetDirty(enemyGO);
            EditorSceneManager.MarkSceneDirty(enemyGO.scene);
            EditorSceneManager.SaveScene(enemyGO.scene);
        }
        Selection.activeGameObject = enemyGO;
        Debug.Log("<color=#74EBD5>[MiasmaVFX] Successfully spawned and configured Miasma Affected Enemy VFX & Prefab in Scene with subtle sparkles!</color>");
    }

    [MenuItem("Tools/VFX Setup/Spawn Stage 4 Fog Diffusion VFX in Scene")]
    public static void ExecuteFogDiffusionSetup()
    {
        GameObject diffusionGO = GameObject.Find("Miasma_Fog_Dissipation");
        if (diffusionGO == null)
        {
            GameObject vfxRoot = GameObject.Find("VFX");
            diffusionGO = new GameObject("Miasma_Fog_Dissipation");
            if (vfxRoot != null)
            {
                diffusionGO.transform.SetParent(vfxRoot.transform, false);
            }
            diffusionGO.transform.localPosition = Vector3.zero;
            diffusionGO.transform.localRotation = Quaternion.identity;
            diffusionGO.transform.localScale = Vector3.one;
        }

        MiasmaDissipationVFX dissipation = diffusionGO.GetComponent<MiasmaDissipationVFX>();
        if (dissipation == null)
        {
            dissipation = diffusionGO.AddComponent<MiasmaDissipationVFX>();
        }

        SerializedObject so = new SerializedObject(dissipation);
        so.FindProperty("dissipationProgress").floatValue = 0f;
        so.FindProperty("currentTurn").intValue = 1;
        so.FindProperty("dissipationDuration").floatValue = 3.0f;
        so.FindProperty("diffusionSpreadRadius").floatValue = 4.5f;
        so.FindProperty("startRadius").floatValue = 1.7f;
        so.FindProperty("enableGroundDiffusion").boolValue = true;
        so.FindProperty("enableEvaporationWisps").boolValue = true;
        so.FindProperty("enableCleansingRipple").boolValue = true;
        so.FindProperty("enableLingeringSparkles").boolValue = true;
        so.FindProperty("sparkleQuantity").floatValue = 3.0f;
        so.FindProperty("sparkleSize").floatValue = 0.028f;
        so.FindProperty("sparkleBloomIntensity").floatValue = 2.5f;

        // Auto-link to existing rotating smoke if available
        GameObject smokeGO = GameObject.Find("Necromantic_Rotating_Smoke");
        if (smokeGO != null)
        {
            so.FindProperty("targetMiasmaFog").objectReferenceValue = smokeGO.GetComponent<RotatingSmokeVFX>();
        }

        Material plumeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticSmokeMat.mat");
        Material wispMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticWispMat.mat");
        Material diamondMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSparkleDiamondMat.mat");
        Material crossMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSparkleCrossMat.mat");

        so.FindProperty("smokePlumeMaterial").objectReferenceValue = plumeMat;
        so.FindProperty("smokeWispMaterial").objectReferenceValue = wispMat;
        so.FindProperty("diamondSparkleMaterial").objectReferenceValue = diamondMat;
        so.FindProperty("crossSparkleMaterial").objectReferenceValue = crossMat;
        so.ApplyModifiedProperties();

        dissipation.InitializeEffect();
        dissipation.Play();

        if (!Application.isPlaying)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            PrefabUtility.SaveAsPrefabAssetAndConnect(diffusionGO, "Assets/Prefabs/Miasma_Fog_Dissipation.prefab", InteractionMode.AutomatedAction);
            EditorUtility.SetDirty(diffusionGO);
            EditorSceneManager.MarkSceneDirty(diffusionGO.scene);
            EditorSceneManager.SaveScene(diffusionGO.scene);
        }
        Selection.activeGameObject = diffusionGO;
        Debug.Log("<color=#74EBD5>[MiasmaVFX] Successfully spawned and configured Stage 4 Fog Diffusion VFX & Prefab in Scene!</color>");
    }

    [MenuItem("Tools/VFX Setup/Rebuild All Miasma VFX in Scene")]
    public static void RebuildAllVFX()
    {
        ExecuteSetup();
        ExecuteAffectedEnemySetup();
        ExecuteFogDiffusionSetup();
        Debug.Log("<color=#74EBD5>[MiasmaVFX] Successfully rebuilt all Miasma VFX layers (Smoke + Affected Enemy + Stage 4 Fog Diffusion with subtle sparkles)!</color>");
    }
}
