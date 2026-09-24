using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to generate procedural textures and materials for the sword VFX.
/// Access via menu: Tools > VFX Setup > Generate All Assets.
/// </summary>
[InitializeOnLoad]
public class VFXSetupWizard : EditorWindow
{
    private static readonly string VFX_FOLDER = "Assets/VFX";
    private static readonly string TEX_FOLDER = "Assets/VFX/Textures";
    private static readonly string MAT_FOLDER = "Assets/VFX/Materials";

    static VFXSetupWizard()
    {
        EditorApplication.delayCall += AutoGenerateIfMissing;
    }

    private static void AutoGenerateIfMissing()
    {
        if (!File.Exists(Path.Combine(Application.dataPath, "VFX/Textures/Spark.png")) ||
            !AssetDatabase.IsValidFolder(MAT_FOLDER))
        {
            GenerateAll();
        }
    }

    [MenuItem("Tools/VFX Setup/Generate All Assets")]
    public static void GenerateAll()
    {
        EnsureDirectories();
        GenerateTextures();
        GenerateMaterials();
        AssetDatabase.Refresh();
        Debug.Log("[VFX Setup] All textures and materials generated successfully!");
    }

    [MenuItem("Tools/VFX Setup/Generate Textures Only")]
    public static void GenerateTexturesMenu()
    {
        EnsureDirectories();
        GenerateTextures();
        AssetDatabase.Refresh();
        Debug.Log("[VFX Setup] Textures generated.");
    }

    [MenuItem("Tools/VFX Setup/Generate Materials Only")]
    public static void GenerateMaterialsMenu()
    {
        EnsureDirectories();
        GenerateMaterials();
        AssetDatabase.Refresh();
        Debug.Log("[VFX Setup] Materials generated.");
    }

    // ==========================================
    //  DIRECTORY SETUP
    // ==========================================
    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder(VFX_FOLDER))
            AssetDatabase.CreateFolder("Assets", "VFX");
        if (!AssetDatabase.IsValidFolder(TEX_FOLDER))
            AssetDatabase.CreateFolder(VFX_FOLDER, "Textures");
        if (!AssetDatabase.IsValidFolder(MAT_FOLDER))
            AssetDatabase.CreateFolder(VFX_FOLDER, "Materials");
    }

    // ==========================================
    //  TEXTURE GENERATION
    // ==========================================
    private static void GenerateTextures()
    {
        // 1. Trail Gradient Texture
        GenerateTrailGradient(256, 64, $"{TEX_FOLDER}/TrailGradient.png");

        // 2. Crescent Slash Texture (shockwave arc)
        GenerateCrescentTexture(256, $"{TEX_FOLDER}/CrescentSlash.png");

        // 3. Noise Texture (multi-octave Perlin)
        GenerateNoiseTexture(256, $"{TEX_FOLDER}/VFXNoise.png");

        // 4. Radial Gradient (for scorch/glow)
        GenerateRadialGradient(256, $"{TEX_FOLDER}/RadialGradient.png");

        // 5. Soft Particle Texture (circle with soft edge)
        GenerateSoftCircle(128, $"{TEX_FOLDER}/SoftCircle.png");

        // 6. Spark Texture (4-point diamond star)
        GenerateSparkTexture(128, $"{TEX_FOLDER}/Spark.png");
    }

    private static void GenerateCrescentTexture(int resolution, string path)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(0.5f, 0.5f);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 uv = new Vector2((float)x / resolution, (float)y / resolution);
                Vector2 dir = uv - center;
                float r = dir.magnitude * 2f;
                float angle = Mathf.Atan2(dir.y, dir.x);

                float angleFade = Mathf.Cos(angle * 1.5f);
                angleFade = Mathf.Clamp01(angleFade);

                float ringDist = Mathf.Abs(r - 0.7f);
                float ringFade = 1f - Mathf.SmoothStep(0f, 0.25f, ringDist);

                float sharpEdge = Mathf.Pow(Mathf.Clamp01(1f - (0.95f - r) * 3f), 2f);
                float alpha = ringFade * angleFade;

                Color c = Color.Lerp(Color.white, new Color(1f, 0.85f, 1f, alpha), sharpEdge);
                c.a = Mathf.Clamp01(alpha);

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        SaveTexture(tex, path);
        Object.DestroyImmediate(tex);
    }

    private static void GenerateTrailGradient(int width, int height, string path)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / (height - 1);
            // Bright center, fade to edges
            float centerDist = Mathf.Abs(v - 0.5f) * 2f;
            float edgeFade = 1f - Mathf.Pow(centerDist, 1.5f);

            for (int x = 0; x < width; x++)
            {
                float u = (float)x / (width - 1);
                // Fade along length
                float lengthFade = 1f - Mathf.Pow(u, 2f);
                float alpha = edgeFade * lengthFade;

                // Core is bright white, edges slightly tinted
                float core = Mathf.Pow(edgeFade, 3f);
                Color color = Color.Lerp(new Color(0.8f, 0.8f, 1f, alpha), Color.white, core);
                color.a = Mathf.Clamp01(alpha);

                tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        SaveTexture(tex, path);
        Object.DestroyImmediate(tex);
    }

    private static void GenerateNoiseTexture(int resolution, string path)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

        float offsetX = Random.Range(0f, 1000f);
        float offsetY = Random.Range(0f, 1000f);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float xCoord = (float)x / resolution;
                float yCoord = (float)y / resolution;

                // Multi-octave Perlin noise
                float noise = 0f;
                noise += Mathf.PerlinNoise((xCoord * 4f) + offsetX, (yCoord * 4f) + offsetY) * 1.0f;
                noise += Mathf.PerlinNoise((xCoord * 8f) + offsetX + 13.7f, (yCoord * 8f) + offsetY + 7.3f) * 0.5f;
                noise += Mathf.PerlinNoise((xCoord * 16f) + offsetX + 23.1f, (yCoord * 16f) + offsetY + 19.8f) * 0.25f;
                noise /= 1.75f;

                tex.SetPixel(x, y, new Color(noise, noise, noise, 1f));
            }
        }

        tex.Apply();
        SaveTexture(tex, path);
        Object.DestroyImmediate(tex);
    }

    private static void GenerateRadialGradient(int resolution, string path)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(0.5f, 0.5f);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 uv = new Vector2((float)x / resolution, (float)y / resolution);
                float dist = Vector2.Distance(uv, center) * 2f;
                float value = 1f - Mathf.Clamp01(dist);
                value = Mathf.Pow(value, 1.5f); // Smooth falloff

                tex.SetPixel(x, y, new Color(value, value, value, value));
            }
        }

        tex.Apply();
        SaveTexture(tex, path);
        Object.DestroyImmediate(tex);
    }

    private static void GenerateSoftCircle(int resolution, string path)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(0.5f, 0.5f);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 uv = new Vector2((float)x / resolution, (float)y / resolution);
                float dist = Vector2.Distance(uv, center) * 2f;
                float alpha = 1f - Mathf.SmoothStep(0.3f, 1f, dist);

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        SaveTexture(tex, path);
        Object.DestroyImmediate(tex);
    }

    private static void GenerateSparkTexture(int resolution, string path)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(0.5f, 0.5f);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 uv = new Vector2((float)x / resolution, (float)y / resolution);
                Vector2 p = (uv - center) * 2f;
                float r = p.magnitude;

                // 4-point diamond star
                float rayH = Mathf.Clamp01(1f - Mathf.Abs(p.y) * 6f) * Mathf.Clamp01(1f - Mathf.Abs(p.x));
                float rayV = Mathf.Clamp01(1f - Mathf.Abs(p.x) * 6f) * Mathf.Clamp01(1f - Mathf.Abs(p.y));
                float star = Mathf.Max(rayH, rayV);

                // Soft radial core
                float core = Mathf.Pow(Mathf.Clamp01(1f - r * 3f), 2f);
                float glow = Mathf.Pow(Mathf.Clamp01(1f - r), 2.5f);

                float value = Mathf.Clamp01(star * 1.5f + core * 2f + glow * 0.4f);

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, value));
            }
        }

        tex.Apply();
        SaveTexture(tex, path);
        Object.DestroyImmediate(tex);
    }

    private static void SaveTexture(Texture2D tex, string assetPath)
    {
        byte[] pngData = tex.EncodeToPNG();
        string fullPath = Path.Combine(Application.dataPath, "..", assetPath);
        fullPath = Path.GetFullPath(fullPath);
        File.WriteAllBytes(fullPath, pngData);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        // Set texture import settings
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
    }

    // ==========================================
    //  MATERIAL GENERATION
    // ==========================================
    private static void GenerateMaterials()
    {
        // 1. Sword Trail Material
        Shader trailShader = Shader.Find("VFX/SwordSlashTrail");
        if (trailShader != null)
        {
            Material trailMat = new Material(trailShader);
            trailMat.SetColor("_Color", new Color(3f, 0.6f, 6f, 1f)); // Deep violet HDR
            trailMat.SetColor("_EdgeColor", new Color(8f, 3f, 12f, 1f)); // Bright rim
            trailMat.SetColor("_CoreColor", new Color(10f, 8f, 12f, 1f)); // White-hot core
            trailMat.SetFloat("_Brightness", 2.5f);
            trailMat.SetFloat("_NoiseStrength", 0.12f);
            trailMat.SetFloat("_NoiseSpeed", 3.0f);
            trailMat.SetFloat("_SharpEdgePower", 3.0f);
            trailMat.SetFloat("_TrailingFadePower", 1.8f);

            AssignTextureIfExists(trailMat, "_MainTex", $"{TEX_FOLDER}/TrailGradient.png");
            AssignTextureIfExists(trailMat, "_NoiseTex", $"{TEX_FOLDER}/VFXNoise.png");

            AssetDatabase.CreateAsset(trailMat, $"{MAT_FOLDER}/SwordTrailMat.mat");
        }
        else
        {
            Debug.LogWarning("[VFX Setup] Could not find VFX/SwordSlashTrail shader. Make sure shaders are compiled.");
        }

        // 2. Additive Particle Material (sparks/embers)
        Shader particleShader = Shader.Find("VFX/AdditiveParticle");
        if (particleShader != null)
        {
            // Ember material
            Material emberMat = new Material(particleShader);
            emberMat.SetColor("_Color", new Color(12f, 6f, 1f, 1f)); // Orange/fire HDR
            emberMat.SetFloat("_Brightness", 4f);
            AssignTextureIfExists(emberMat, "_MainTex", $"{TEX_FOLDER}/Spark.png");
            AssetDatabase.CreateAsset(emberMat, $"{MAT_FOLDER}/EmberMat.mat");

            // Spark material (brighter, whiter)
            Material sparkMat = new Material(particleShader);
            sparkMat.SetColor("_Color", new Color(8f, 4f, 10f, 1f)); // Purple spark HDR
            sparkMat.SetFloat("_Brightness", 5f);
            AssignTextureIfExists(sparkMat, "_MainTex", $"{TEX_FOLDER}/Spark.png");
            AssetDatabase.CreateAsset(sparkMat, $"{MAT_FOLDER}/SparkMat.mat");
        }

        // 3. Ground Decal Material
        Shader decalShader = Shader.Find("VFX/GroundDecal");
        if (decalShader != null)
        {
            Material scorchMat = new Material(decalShader);
            scorchMat.SetColor("_Color", new Color(8f, 3f, 0.5f, 1f));
            scorchMat.SetColor("_EdgeColor", new Color(15f, 5f, 1f, 1f));
            scorchMat.SetFloat("_DissolveAmount", 0f);
            scorchMat.SetFloat("_EdgeWidth", 0.08f);
            scorchMat.SetFloat("_Brightness", 3f);
            AssignTextureIfExists(scorchMat, "_MainTex", $"{TEX_FOLDER}/RadialGradient.png");
            AssignTextureIfExists(scorchMat, "_NoiseTex", $"{TEX_FOLDER}/VFXNoise.png");
            AssetDatabase.CreateAsset(scorchMat, $"{MAT_FOLDER}/GroundScorchMat.mat");
        }

        // 4. Heat Smoke / Vapor material (Fail-proof pure additive / soft blending)
        Shader vaporShader = Shader.Find("VFX/SoftHeatSmoke");
        if (vaporShader == null) vaporShader = Shader.Find("VFX/AdditiveParticle");
        if (vaporShader != null)
        {
            Material vaporMat = new Material(vaporShader);
            vaporMat.SetColor("_Color", new Color(1.5f, 1.0f, 0.4f, 0.25f));
            vaporMat.SetFloat("_CoreBrightness", 1.8f);
            vaporMat.SetFloat("_Softness", 0.6f);
            AssetDatabase.CreateAsset(vaporMat, $"{MAT_FOLDER}/DustMat.mat");
        }
    }

    private static void AssignTextureIfExists(Material mat, string propertyName, string texturePath)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex != null)
        {
            mat.SetTexture(propertyName, tex);
        }
    }
}
