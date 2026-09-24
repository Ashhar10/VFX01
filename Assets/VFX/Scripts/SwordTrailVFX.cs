using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Professional 2-Point Sword Slash Trail & Stardust VFX Controller.
/// Generates a smooth sweeping energy blade between the sword tip and hilt (Hovl Studio style),
/// accompanied by delicate, tight stardust motes along the cutting arc (no scattering, no huge blocks).
/// All variables exposed for complete customization in Inspector.
/// </summary>
[DisallowMultipleComponent]
public class SwordTrailVFX : MonoBehaviour
{
    // ==========================================
    //  BLADE POINTS
    // ==========================================
    [Header("=== Blade Tracking Points ===")]
    [Tooltip("Transform at the tip of the sword blade.")]
    [SerializeField] private Transform swordTip;

    [Tooltip("Transform at the base/guard of the sword blade.")]
    [SerializeField] private Transform swordBase;

    // ==========================================
    //  SLASH ARC (2-POINT BLADE MESH)
    // ==========================================
    [Header("=== Slash Arc Appearance (Legendary Blade Sweep) ===")]
    [Tooltip("How long the slash arc persists in seconds.")]
    [SerializeField, Range(0.05f, 1f)] private float trailLifetime = 0.22f;

    [Tooltip("Smoothness of the arc mesh (subdivisions between frames). Higher = smoother curve.")]
    [SerializeField, Range(1, 6)] private int arcSmoothness = 3;

    [Tooltip("Minimum sword tip movement distance to record a new segment.")]
    [SerializeField, Range(0.001f, 0.1f)] private float minVertexDistance = 0.01f;

    [Tooltip("Minimum tip swing speed (m/s) required to generate trail. Prevents tangled idle ribbons.")]
    [SerializeField, Range(0.2f, 5f)] private float minSwingSpeed = 1.2f;

    // ==========================================
    //  COLORS & GLOW (HDR FOR BLOOM)
    // ==========================================
    [Header("=== Colors (Golden Knight / Divine Right) ===")]
    [Tooltip("Main body color of the energy arc. Use HDR intensity > 1 for bloom.")]
    [SerializeField, ColorUsage(true, true)] private Color energyColor = new Color(10f, 6f, 1f, 1f);

    [Tooltip("Bright cutting rim/tip color.")]
    [SerializeField, ColorUsage(true, true)] private Color rimColor = new Color(15f, 10f, 3f, 1f);

    [Tooltip("Blinding white-hot core color.")]
    [SerializeField, ColorUsage(true, true)] private Color coreColor = new Color(18f, 16f, 10f, 1f);

    [Tooltip("Overall brightness multiplier.")]
    [SerializeField, Range(0.5f, 10f)] private float brightness = 2.8f;

    // ==========================================
    //  SHADER DISTORTION
    // ==========================================
    [Header("=== Energy Distortion ===")]
    [Tooltip("Noise texture for energy rippling. Auto-generated if null.")]
    [SerializeField] private Texture2D noiseTexture;

    [Tooltip("Intensity of noise rippling along the arc.")]
    [SerializeField, Range(0f, 0.5f)] private float distortionStrength = 0.12f;

    [Tooltip("Speed of noise scrolling.")]
    [SerializeField, Range(0f, 10f)] private float distortionSpeed = 3f;

    [Tooltip("Razor sharpness of the cutting edge.")]
    [SerializeField, Range(1f, 10f)] private float cuttingEdgeSharpness = 3.5f;

    // ==========================================
    //  LEGENDARY STARDUST MOTES (TIGHT, NOT SCATTERED)
    // ==========================================
    [Header("=== Legendary Stardust Motes ===")]
    [Tooltip("Enable delicate glowing motes that trace the blade's cutting edge.")]
    [SerializeField] private bool enableStardust = true;

    [Tooltip("Size of stardust motes (kept small for legendary, non-blocky look).")]
    [SerializeField, Range(0.003f, 0.05f)] private float stardustSize = 0.015f;

    [Tooltip("Mote lifetime in seconds.")]
    [SerializeField, Range(0.1f, 1.5f)] private float stardustLifetime = 0.35f;

    [Tooltip("Number of motes emitted per second during swing.")]
    [SerializeField, Range(5, 60)] private int stardustEmissionRate = 25;

    [Tooltip("Drift speed of motes (0 = motes stay perfectly suspended in the blade's wake).")]
    [SerializeField, Range(0f, 0.5f)] private float stardustDriftSpeed = 0.05f;

    [Tooltip("Stardust color (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color stardustColor = new Color(12f, 8f, 2f, 1f);

    // ==========================================
    //  DYNAMIC LIGHT
    // ==========================================
    [Header("=== Dynamic Slash Light ===")]
    [Tooltip("Enable dynamic illumination along the slash.")]
    [SerializeField] private bool enableLight = true;

    [Tooltip("Slash light color.")]
    [SerializeField] private Color lightColor = new Color(1f, 0.8f, 0.3f, 1f);

    [Tooltip("Light intensity.")]
    [SerializeField, Range(0f, 20f)] private float lightIntensity = 5f;

    [Tooltip("Light range.")]
    [SerializeField, Range(0f, 15f)] private float lightRange = 4f;

    // ==========================================
    //  INTERNAL MESH DATA STRUCTURE
    // ==========================================
    private class TrailPoint
    {
        public Vector3 tip;
        public Vector3 bse;
        public float time;

        public TrailPoint(Vector3 t, Vector3 b, float tm)
        {
            tip = t;
            bse = b;
            time = tm;
        }
    }

    private List<TrailPoint> points = new List<TrailPoint>();
    private Mesh arcMesh;
    private MeshFilter arcMeshFilter;
    private MeshRenderer arcMeshRenderer;
    private Material arcMaterial;

    private ParticleSystem stardustSystem;
    private Light vfxLight;
    private bool isPlaying;

    private List<Vector3> meshVertices = new List<Vector3>();
    private List<int> meshTriangles = new List<int>();
    private List<Vector2> meshUVs = new List<Vector2>();
    private List<Color> meshColors = new List<Color>();

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        SetupTrackingPoints();
        SetupArcMesh();

        if (enableStardust)
            SetupStardustSystem();

        if (enableLight)
            SetupLight();

        Stop();
    }

    private void SetupTrackingPoints()
    {
        if (swordTip == null)
        {
            var tip = transform.Find("SwordTip_TrailPoint");
            if (tip == null)
            {
                var go = new GameObject("SwordTip_TrailPoint");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0, 0, 1.2f);
                go.transform.localRotation = Quaternion.identity;
                tip = go.transform;
            }
            swordTip = tip;
        }

        if (swordBase == null)
        {
            var bse = transform.Find("SwordBase_TrailPoint");
            if (bse == null)
            {
                var go = new GameObject("SwordBase_TrailPoint");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                bse = go.transform;
            }
            swordBase = bse;
        }
    }

    private void SetupArcMesh()
    {
        GameObject arcGO = new GameObject("SwordSlash_ArcMesh");
        arcGO.transform.position = Vector3.zero;
        arcGO.transform.rotation = Quaternion.identity;
        // Keep in world space to avoid parent bone scaling distortion
        arcGO.transform.SetParent(null);

        arcMeshFilter = arcGO.AddComponent<MeshFilter>();
        arcMeshRenderer = arcGO.AddComponent<MeshRenderer>();
        arcMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        arcMeshRenderer.receiveShadows = false;

        arcMesh = new Mesh();
        arcMesh.name = "SlashArc_Procedural";
        arcMesh.MarkDynamic();
        arcMeshFilter.mesh = arcMesh;

        Shader trailShader = Shader.Find("VFX/SwordSlashTrail");
        if (trailShader == null) trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (trailShader == null) trailShader = Shader.Find("Particles/Standard Unlit");
        if (trailShader == null) trailShader = Shader.Find("Mobile/Particles/Additive");

        arcMaterial = new Material(trailShader);
        arcMaterial.name = "SlashArc_Material";
        ConfigureAdditiveFallback(arcMaterial, energyColor);

        if (noiseTexture == null)
            noiseTexture = GenerateProceduralNoise(128);

        arcMaterial.SetTexture("_NoiseTex", noiseTexture);
        UpdateMaterialProperties();

        arcMeshRenderer.material = arcMaterial;
        arcMeshRenderer.enabled = false;
    }

    private void SetupStardustSystem()
    {
        GameObject stardustGO = new GameObject("Stardust_WakeMotes");
        // Place in world space so parent bone scaling does NOT distort mote size
        stardustGO.transform.SetParent(null);
        stardustGO.transform.position = swordTip.position;

        stardustSystem = stardustGO.AddComponent<ParticleSystem>();

        var main = stardustSystem.main;
        main.startLifetime = stardustLifetime;
        main.startSize = stardustSize;
        main.startSpeed = stardustDriftSpeed;
        main.startColor = stardustColor;
        main.maxParticles = 300;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        // Crucial: Shape scaling prevents parent transform scaling from turning motes into giant squares
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.gravityModifier = 0f; // No falling - they hover like magical dust
        main.playOnAwake = false;
        main.loop = true;

        var emission = stardustSystem.emission;
        emission.rateOverTime = stardustEmissionRate;

        // Shape: line along the blade cutting edge
        var shape = stardustSystem.shape;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius = 0.5f;

        // Size over lifetime: sharp sparkle then shrink
        var sol = stardustSystem.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.4f);
        sizeCurve.AddKey(0.15f, 1f);
        sizeCurve.AddKey(0.6f, 0.8f);
        sizeCurve.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime: fade out
        var col = stardustSystem.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(0.8f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = g;

        // Renderer
        var renderer = stardustGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Shader sparkShader = Shader.Find("VFX/AdditiveParticle");
        if (sparkShader == null) sparkShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sparkShader == null) sparkShader = Shader.Find("Particles/Standard Unlit");
        if (sparkShader == null) sparkShader = Shader.Find("Mobile/Particles/Additive");

        Material sparkMat = new Material(sparkShader);
        sparkMat.SetColor("_Color", stardustColor);
        sparkMat.SetColor("_BaseColor", stardustColor);
        sparkMat.SetFloat("_Brightness", 3.5f);
        sparkMat.SetFloat("_StarIntensity", 2.0f);
        sparkMat.SetFloat("_CoreSharpness", 5.0f);
        ConfigureAdditiveFallback(sparkMat, stardustColor);
        renderer.material = sparkMat;
    }

    private void ConfigureAdditiveFallback(Material mat, Color col)
    {
        if (mat == null) return;
        mat.SetColor("_Color", col);
        mat.SetColor("_BaseColor", col);
        if (mat.shader != null && !mat.shader.name.StartsWith("VFX/"))
        {
            mat.SetFloat("_Surface", 1.0f); // Transparent
            mat.SetFloat("_Blend", 1.0f);   // Additive
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }

    private void SetupLight()
    {
        GameObject lightGO = new GameObject("SlashPointLight");
        lightGO.transform.SetParent(swordTip);
        lightGO.transform.localPosition = Vector3.zero;

        vfxLight = lightGO.AddComponent<Light>();
        vfxLight.type = LightType.Point;
        vfxLight.color = lightColor;
        vfxLight.intensity = lightIntensity;
        vfxLight.range = lightRange;
        vfxLight.shadows = LightShadows.None;
        vfxLight.enabled = false;
    }

    public void Play()
    {
        isPlaying = true;
        points.Clear();

        if (arcMeshRenderer != null)
            arcMeshRenderer.enabled = true;

        if (stardustSystem != null && enableStardust)
        {
            stardustSystem.transform.position = (swordTip.position + swordBase.position) * 0.5f;
            stardustSystem.Play();
        }

        if (vfxLight != null && enableLight)
            vfxLight.enabled = true;
    }

    public void Stop()
    {
        isPlaying = false;

        if (stardustSystem != null)
            stardustSystem.Stop();

        if (vfxLight != null)
            vfxLight.enabled = false;
    }

    public void ClearImmediate()
    {
        Stop();
        points.Clear();
        if (arcMesh != null)
            arcMesh.Clear();
        if (arcMeshRenderer != null)
            arcMeshRenderer.enabled = false;
    }

    public void UpdateMaterialProperties()
    {
        if (arcMaterial == null) return;
        arcMaterial.SetColor("_Color", energyColor);
        arcMaterial.SetColor("_BaseColor", energyColor);
        arcMaterial.SetColor("_EdgeColor", rimColor);
        arcMaterial.SetColor("_CoreColor", coreColor);
        arcMaterial.SetFloat("_Brightness", brightness);
        arcMaterial.SetFloat("_NoiseStrength", distortionStrength);
        arcMaterial.SetFloat("_NoiseSpeed", distortionSpeed);
        arcMaterial.SetFloat("_SharpEdgePower", cuttingEdgeSharpness);
    }

    /// <summary>
    /// Trigger an impact spark burst at a specific world position (Row 1 Frame 3 'Enemy Contact').
    /// </summary>
    public void TriggerImpactBurst(Vector3 contactPosition)
    {
        StartCoroutine(PlayImpactBurstRoutine(contactPosition));
    }

    private System.Collections.IEnumerator PlayImpactBurstRoutine(Vector3 pos)
    {
        GameObject burstGO = new GameObject("SlashImpact_Burst");
        burstGO.transform.position = pos;

        ParticleSystem ps = burstGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.03f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startColor = rimColor;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.gravityModifier = 0.5f;
        main.playOnAwake = false;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var renderer = burstGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 3f;
        renderer.velocityScale = 0.05f;

        Shader shader = Shader.Find("VFX/AdditiveParticle");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material mat = new Material(shader);
        mat.SetColor("_Color", rimColor);
        mat.SetFloat("_Brightness", 4f);
        renderer.material = mat;

        ps.Play();

        yield return new WaitForSeconds(0.6f);
        Destroy(burstGO);
    }

    private void LateUpdate()
    {
        if (swordTip == null || swordBase == null) return;

        float now = Time.time;

        // 1. If active, sample current blade edge
        if (isPlaying)
        {
            Vector3 tipPos = swordTip.position;
            Vector3 basePos = swordBase.position;

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float dist = points.Count > 0 ? Vector3.Distance(tipPos, points[points.Count - 1].tip) : 999f;
            float currentSpeed = points.Count > 0 ? (dist / dt) : minSwingSpeed;

            bool shouldAdd = false;
            if (points.Count == 0)
            {
                shouldAdd = true;
            }
            else if (dist >= minVertexDistance && currentSpeed >= minSwingSpeed)
            {
                shouldAdd = true;
            }

            if (shouldAdd)
            {
                // Smooth interpolation between previous and current point
                if (points.Count > 0 && arcSmoothness > 1)
                {
                    TrailPoint last = points[points.Count - 1];
                    for (int s = 1; s < arcSmoothness; s++)
                    {
                        float t = (float)s / arcSmoothness;
                        Vector3 subTip = Vector3.Lerp(last.tip, tipPos, t);
                        Vector3 subBase = Vector3.Lerp(last.bse, basePos, t);
                        float subTime = Mathf.Lerp(last.time, now, t);
                        points.Add(new TrailPoint(subTip, subBase, subTime));
                    }
                }

                points.Add(new TrailPoint(tipPos, basePos, now));
            }

            // Update stardust emitter position along blade
            if (stardustSystem != null && enableStardust)
            {
                stardustSystem.transform.position = (tipPos + basePos) * 0.5f;
                Vector3 bladeDir = tipPos - basePos;
                if (bladeDir != Vector3.zero)
                {
                    stardustSystem.transform.rotation = Quaternion.LookRotation(bladeDir);
                    var shape = stardustSystem.shape;
                    shape.radius = bladeDir.magnitude * 0.5f;
                }
            }
        }

        // 2. Remove expired points
        while (points.Count > 0 && (now - points[0].time) > trailLifetime)
        {
            points.RemoveAt(0);
        }

        // 3. Rebuild the mesh
        RebuildArcMesh(now);

        if (points.Count < 2 && !isPlaying && arcMeshRenderer != null)
        {
            arcMeshRenderer.enabled = false;
        }
    }

    private void RebuildArcMesh(float currentTime)
    {
        if (points.Count < 2)
        {
            if (arcMesh != null) arcMesh.Clear();
            return;
        }

        meshVertices.Clear();
        meshTriangles.Clear();
        meshUVs.Clear();
        meshColors.Clear();

        int count = points.Count;
        float invCount = 1f / (count - 1);

        for (int i = 0; i < count; i++)
        {
            TrailPoint pt = points[i];
            float age = (currentTime - pt.time) / trailLifetime;
            float life = Mathf.Clamp01(1f - age);

            // Base vertex (V = 0, hilt)
            meshVertices.Add(pt.bse);
            meshUVs.Add(new Vector2((float)i * invCount, 0f));
            meshColors.Add(new Color(1f, 1f, 1f, life));

            // Tip vertex (V = 1, cutting tip)
            meshVertices.Add(pt.tip);
            meshUVs.Add(new Vector2((float)i * invCount, 1f));
            meshColors.Add(new Color(1f, 1f, 1f, life));
        }

        // Construct quad strip (two triangles per segment, double-sided so visible from any camera angle)
        for (int i = 0; i < count - 1; i++)
        {
            int base0 = i * 2;
            int tip0 = i * 2 + 1;
            int base1 = (i + 1) * 2;
            int tip1 = (i + 1) * 2 + 1;

            // Front face
            meshTriangles.Add(base0);
            meshTriangles.Add(tip0);
            meshTriangles.Add(base1);

            meshTriangles.Add(base1);
            meshTriangles.Add(tip0);
            meshTriangles.Add(tip1);

            // Back face
            meshTriangles.Add(base1);
            meshTriangles.Add(tip0);
            meshTriangles.Add(base0);

            meshTriangles.Add(tip1);
            meshTriangles.Add(tip0);
            meshTriangles.Add(base1);
        }

        arcMesh.Clear();
        arcMesh.SetVertices(meshVertices);
        arcMesh.SetUVs(0, meshUVs);
        arcMesh.SetColors(meshColors);
        arcMesh.SetTriangles(meshTriangles, 0);
        arcMesh.RecalculateNormals();
        arcMesh.RecalculateBounds();
    }

    private Texture2D GenerateProceduralNoise(int res)
    {
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float n = Mathf.PerlinNoise((float)x / res * 4f, (float)y / res * 4f);
                n += 0.5f * Mathf.PerlinNoise((float)x / res * 8f + 5.3f, (float)y / res * 8f + 2.1f);
                n /= 1.5f;
                tex.SetPixel(x, y, new Color(n, n, n, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    private void OnDestroy()
    {
        if (arcMeshFilter != null && arcMeshFilter.gameObject != null)
            Destroy(arcMeshFilter.gameObject);

        if (stardustSystem != null && stardustSystem.gameObject != null)
            Destroy(stardustSystem.gameObject);
    }
}
