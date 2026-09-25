# Alura Graves "Miasma" Rotating Smoke VFX — Complete Project Memory & Training Specification

> **Purpose**: This document contains the entire memory, technical architecture, visual design requirements, troubleshooting history, shader configurations, and codebase for the **Alura Graves "3. MIASMA (Ultimate)" Wind-up Smoke VFX** in Unity 6 URP.  
> Share this file directly into another AI chat session to instantly train and give full context to any agent or developer.

---

## 1. Project & Technical Environment

| Setting | Specification |
|---|---|
| **Engine Version** | Unity 6 (6000.0.x / 6000.x) |
| **Render Pipeline** | Universal Render Pipeline (URP 17.0.4) |
| **Color Space** | Linear |
| **Project Root Path** | `D:\Projects\Waheed\Attack VFX\VFX01` |
| **Active Scene** | `Assets/Scenes/SampleScene.unity` |
| **Main VFX Prefab** | `Assets/Prefabs/Necromantic_Rotating_Smoke.prefab` |
| **Primary Script** | `Assets/VFX/Scripts/RotatingSmokeVFX.cs` |
| **Custom Inspector** | `Assets/VFX/Editor/RotatingSmokeVFXEditor.cs` |
| **Scene Auto-Setup** | `Assets/VFX/Editor/SetupMiasmaScene.cs` |

---

## 2. Character & Visual Design Specification

### 2.1 Character Context
- **Name**: Alura Graves
- **Title**: Freelance Necromancer (*"Death is a resource too."*)
- **Style**: Anime / Stylized JRPG character with glowing necromantic magical circles, dark trench coat, cyan hair.
- **Skill**: **3. MIASMA (Ultimate)**
  - *Stage [1] Wind-Up*: Alura raises both hands. Massive swirling necromantic fog gathers around her in a vortex, magical rings form on the ground, ethereal soul embers float upward, and luminous motes orbit the vortex.
  - *Stage [2] Release / Fog Spreads*: A wave of spectral rolling fog spreads across the battlefield, damaging enemies and healing allies.

### 2.2 Color Palette & Aesthetic Directives
The user established clear rules for the visual palette:
- **Bone White** (`#F6F3EC` / RGB `0.96, 0.93, 0.88`): Primary base color for the cloud bodies, healing/life energy, and core sparkles.
- **Lavender** (`#BFA3E6` / RGB `0.75, 0.60, 0.90`): Ethereal mystical accent, billow shading, wisp ribbons, and outer rim catch.
- **Deep Ethereal Lilac** (`#D8C7F0` / RGB `0.85, 0.78, 0.92`): Soft shadowed cloud base.
- **Luminous Emissive Glow**: Sparkles and soul motes must have intense HDR emissive glow in Bone White and Lavender.
- **Strict Aesthetic Rule**: **"Proper cloudy power"** — Voluminous, fluffy, thick, soft rolling clouds. **NO dark/pitch-black clouds. NO sharp geometric slash swords. NO green square quads.**

---

## 3. Critical Technical Lessons & Gotchas (Unity 6 URP)

### 3.1 The "Pink Shader" Bug
- **Issue**: Custom HLSL shaders (`NecromanticSmoke.shader`) with `#pragma multi_compile_fog` or `DeclareDepthTexture.hlsl` failed compilation silently in Unity 6 URP, causing materials to turn solid magenta/pink.
- **Fix**: Use Unity's native built-in `Universal Render Pipeline/Particles/Unlit` shader:
  - **Shader GUID**: `0406db5a14f94604a8c57ccfbc9f3b46`
  - Fully pre-compiled, highly optimized, natively supports soft particles, camera distance fade, vertex color tinting, and HDR emission.

### 3.2 The "Green Square Quad" Bug
- **Issue**: Additive particles rendered as flat, rotated green square tiles orbiting the character.
- **Root Causes**:
  1. Blend mode was set to `_SrcBlend: 1` (`One`) + `_DstBlend: 1` (`One`) or `_DstBlend: 10` (`OneMinusSrcAlpha`), ignoring particle alpha masking.
  2. Procedural texture generator had non-zero RGB at the outer texture borders.
  3. The serialized field `necroticGreenColor` on the prefab held old green values (`0.35, 1.0, 0.70`).
- **Fixes**:
  1. Material blend mode for additive particles must be `_SrcBlend: 5` (`SrcAlpha`) and `_DstBlend: 1` (`One`).
  2. All procedural textures must have generous outer margins with absolute `0.0` RGB and `0.0` Alpha at the borders.
  3. Serialized prefab fields and script gradients updated to pure Bone White & Lavender.

### 3.3 ParticleSystem API in Unity 6
- `ParticleSystemShapeType.Cylinder` **does not exist** in Unity 6 C# API.
- Use `ParticleSystemShapeType.Cone` with `angle = 0f` to create a perfect cylinder shape.

### 3.4 Prefab Serialization Overrides
- Unity serializes component property values into `.prefab` and `.unity` files.
- Changing C# field initializers **will not affect** existing scene objects or prefabs if those fields were already serialized.
- Both the C# code, the `.prefab` file, and `SetupMiasmaScene.cs` must stay synchronized.

### 3.5 Alpha Multiplication
- In URP `Particles/Unlit`, the material's `_BaseColor.a` **multiplies** the ParticleSystem `main.startColor.a` and `colorOverLifetime.alpha`.
- Keep `_BaseColor: {r: 1, g: 1, b: 1, a: 1}` on materials so the ParticleSystem gradient has 100% full, unattenuated control over particle visibility.

---

## 4. Architecture of the 5 Particle System Layers

The `RotatingSmokeVFX.cs` component generates and manages 5 distinct child particle systems under a single rotating root:

```
Necromantic_Rotating_Smoke (Root - rotates around Y at 60°/s)
├── 1_Backdrop_Billowing_Fog     (Large volumetric cumulus clouds)
├── 2_Swirling_Vortex_Wisps      (Fast spiral vapor streamers)
├── 3_Ground_Creeping_Mist       (Dense crawling floor fog)
├── 4_Floating_Soul_Orbs         (Floating luminous soul embers)
└── 5_Sparkling_Energy_Motes     (Tiny twinkling 4-point star sparkles in rich 3D volume)
```

### Detailed Layer Parameters

| Layer | Particles (Max / Rate) | Start Size | Lifetime | Shape & Velocity | Color & Opacity |
|---|---|---|---|---|---|
| **1. Billowing Fog** | 65 max / 16/s | 2.2m – 3.4m | 3.5s – 5.2s | Cylinder (`r=1.8m`, `h=2.6m`), Y `0.30`, Orbit `30°/s`, Noise `0.32` | Bone White ➔ Lavender. Alpha peak `0.65`. Expands `1.5×` over life. |
| **2. Vortex Wisps** | 45 max / 14/s | 1.4m – 2.2m | 2.2s – 3.4s | Donut (`r=1.6m`), Y `0.50`, Orbit `90°/s` (Fast swirl) | Bone White ➔ Vibrant Lavender. Alpha peak `0.60`. |
| **3. Ground Mist** | 35 max / 10/s | 2.2m – 3.8m | 3.2s – 4.8s | Flat Circle (`r=2.3m`, `y=0.05m`), Radial crawl `0.08` | Bone White with lavender tint. Alpha peak `0.55`. |
| **4. Soul Orbs** | 6 max / 1.5/s | 0.5m – 0.9m | 3.5s – 5.0s | Sphere (`r=1.5m`, `y=1.5m`), gentle upward drift | Luminous Bone White core, soft lavender aura. |
| **5. Energy Motes** | 150 max / 65/s | 0.04m – 0.14m (Tiny Stars) | 1.6s – 3.2s | Volumetric Cylinder (`r=1.6m`, `h=2.4m`), Y `0.45`, Orbit `115°/s`, Twinkle Spin | Additive emissive Bone White & glowing Lavender star gleams. Twinkle shimmer curve. Alpha `0.95`. |

---

## 5. Material Specifications

All 4 materials use Shader `Universal Render Pipeline/Particles/Unlit` (`guid: 0406db5a14f94604a8c57ccfbc9f3b46`):

### 1. `NecromanticSmokeMat.mat` (Plume Clouds)
- **Texture**: `Assets/VFX/Textures/MiasmaSmoke_Plume.png` (`guid: b4a2c101d5e341209bcae01a884f1001`)
- **Blend Mode**: Alpha Blend (`_Blend: 0`, `_SrcBlend: 5`, `_DstBlend: 10`, `_ZWrite: 0`)
- **Base Color**: `{r: 1, g: 1, b: 1, a: 1}`
- **Soft Particles**: Enabled (`_SoftParticlesNearFadeDistance: 0.1`, `_SoftParticlesFarFadeDistance: 0.8`)

### 2. `NecromanticWispMat.mat` (Wispy Streamers)
- **Texture**: `Assets/VFX/Textures/MiasmaSmoke_Wisp.png` (`guid: b4a2c101d5e341209bcae01a884f1002`)
- **Blend Mode**: Alpha Blend (`_Blend: 0`, `_SrcBlend: 5`, `_DstBlend: 10`, `_ZWrite: 0`)
- **Base Color**: `{r: 1, g: 1, b: 1, a: 1}`
- **Soft Particles**: Enabled

### 3. `MiasmaSoulOrbMat.mat` (Soul Embers)
- **Texture**: `Assets/VFX/Textures/SoulOrb_SkullMote.png` (`guid: b4a2c101d5e341209bcae01a884f1003`)
- **Blend Mode**: Alpha Blend (`_Blend: 0`, `_SrcBlend: 5`, `_DstBlend: 10`, `_ZWrite: 0`)
- **Base Color**: `{r: 1, g: 1, b: 1, a: 1}`
- **Emission**: `{r: 0.6, g: 0.45, b: 0.8, a: 1}` (`_EMISSION` keyword enabled)

### 4. `MiasmaEnergyMoteMat.mat` (Emissive Sparkles)
- **Texture**: `Assets/VFX/Textures/EnergyMote_CrossStar.png` (`guid: b4a2c101d5e341209bcae01a884f1004`)
- **Blend Mode**: Additive Blend (`_Blend: 1`, `_SrcBlend: 5`, `_DstBlend: 1`, `_ZWrite: 0`)
- **Base Color**: `{r: 1, g: 1, b: 1, a: 1}`
- **Emission**: `{r: 1.0, g: 0.85, b: 1.3, a: 1}` (`_EMISSION` keyword enabled)

---

## 6. Procedural Textures & Generator Script

Generated via Python (`PIL` + `numpy`) at `Assets/VFX/Textures/generate_cloud_textures.py`:

```python
# Key properties of generated textures:
# 1. MiasmaSmoke_Plume.png (512x512):
#    - Multi-lobed cumulus cloud body with fractal billow creases
#    - Smooth feathered alpha falloff (smoothstep 0.05 to 0.85)
#    - Premultiplied zero border so edges never touch the UV boundary
# 2. MiasmaSmoke_Wisp.png (512x512):
#    - Curved S-curve smoke streamer with soft wispy turbulence
#    - No sharp blade/slash edges
# 3. EnergyMote_CrossStar.png (256x256):
#    - Luminous 4-point cross star with 4 diagonal micro-glints and radiant Gaussian core
#    - Pure zero border at radius >= 0.75 (strictly 0.0 RGBA, no square borders)
# 4. SoulOrb_SkullMote.png (256x256):
#    - Ethereal soul sphere with luminous center and fading vapor body
```

---

## 7. Complete C# Script Reference: `RotatingSmokeVFX.cs`

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteAlways]
[DisallowMultipleComponent]
public class RotatingSmokeVFX : MonoBehaviour
{
    [Header("=== Vortex Geometry & Dynamics ===")]
    [SerializeField, Range(0.5f, 5.0f)] private float vortexRadius = 1.7f;
    [SerializeField, Range(0.5f, 5.0f)] private float vortexHeight = 2.4f;
    [SerializeField, Range(-200f, 200f)] private float rotationSpeed = 60f;
    [SerializeField, Range(0.05f, 2.0f)] private float upwardSpeed = 0.40f;
    [SerializeField, Range(0.3f, 2.5f)] private float particleScale = 1.0f;
    [SerializeField, Range(0.1f, 2.5f)] private float densityMultiplier = 1.0f;

    [Header("=== Color Palette (Bone White & Lavender) ===")]
    [SerializeField] private Color boneWhiteColor = new Color(0.96f, 0.93f, 0.88f, 1.0f);
    [SerializeField] private Color midtoneViolet = new Color(0.75f, 0.60f, 0.90f, 1.0f);
    [SerializeField] private Color deepCharcoalPurple = new Color(0.85f, 0.78f, 0.92f, 1.0f);
    [SerializeField] private Color rimHighlightLilac = new Color(0.92f, 0.86f, 0.98f, 1.0f);
    [SerializeField] private Color groundMistColor = new Color(0.90f, 0.87f, 0.85f, 1.0f);
    [SerializeField] private Color necroticGreenColor = new Color(0.98f, 0.95f, 0.90f, 1.0f);

    [Header("=== Layer Toggles ===")]
    [SerializeField] private bool enableBackdropFog = true;
    [SerializeField] private bool enableVortexWisps = true;
    [SerializeField] private bool enableGroundMist = true;
    [SerializeField] private bool enableSoulOrbs = true;
    [SerializeField] private bool enableEnergyMotes = true;
    [SerializeField, Range(0.01f, 0.3f)] private float moteMinSize = 0.04f;
    [SerializeField, Range(0.02f, 0.6f)] private float moteMaxSize = 0.14f;
    [SerializeField, Range(10f, 250f)] private float moteEmissionRate = 65f;
    [SerializeField, Range(20, 400)] private int moteMaxParticles = 150;
    [SerializeField, Range(0.5f, 5.0f)] private float moteOrbitMultiplier = 1.9f;
    [SerializeField, Range(0f, 360f)] private float moteTwinkleSpeed = 90f;

    [Header("=== Materials & Textures ===")]
    [SerializeField] private Material smokePlumeMaterial;
    [SerializeField] private Material smokeWispMaterial;
    [SerializeField] private Material soulOrbMaterial;
    [SerializeField] private Material energyMoteMaterial;

    private ParticleSystem backdropFogPS;
    private ParticleSystem vortexWispsPS;
    private ParticleSystem groundMistPS;
    private ParticleSystem soulOrbsPS;
    private ParticleSystem energyMotesPS;

    public float VortexRadius => vortexRadius;
    public float RotationSpeed => rotationSpeed;

    private void Awake() => InitializeEffect();
    private void OnEnable() { LoadDefaultAssets(); InitializeEffect(); Play(); }
    private void OnDisable() => Stop();

    private void Update()
    {
        if (Mathf.Abs(rotationSpeed) > 0.01f)
        {
            float delta = Application.isPlaying ? Time.deltaTime : 0.016f;
            transform.Rotate(Vector3.up, rotationSpeed * 0.25f * delta, Space.Self);
        }
    }

    private void OnValidate() { LoadDefaultAssets(); UpdateAllLayers(); }

    private void LoadDefaultAssets()
    {
#if UNITY_EDITOR
        if (smokePlumeMaterial == null)
            smokePlumeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticSmokeMat.mat");
        if (smokeWispMaterial == null)
            smokeWispMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticWispMat.mat");
        if (soulOrbMaterial == null)
            soulOrbMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSoulOrbMat.mat");
        if (energyMoteMaterial == null)
            energyMoteMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaEnergyMoteMat.mat");
#endif
    }

    public void InitializeEffect()
    {
        LoadDefaultAssets();
        backdropFogPS = GetOrCreateChildPS("1_Backdrop_Billowing_Fog");
        ConfigureBackdropFog(backdropFogPS);
        vortexWispsPS = GetOrCreateChildPS("2_Swirling_Vortex_Wisps");
        ConfigureVortexWisps(vortexWispsPS);
        groundMistPS = GetOrCreateChildPS("3_Ground_Creeping_Mist");
        ConfigureGroundMist(groundMistPS);
        soulOrbsPS = GetOrCreateChildPS("4_Floating_Soul_Orbs");
        ConfigureSoulOrbs(soulOrbsPS);
        energyMotesPS = GetOrCreateChildPS("5_Sparkling_Energy_Motes");
        ConfigureEnergyMotes(energyMotesPS);
    }

    public void UpdateAllLayers()
    {
        if (backdropFogPS == null) backdropFogPS = GetOrCreateChildPS("1_Backdrop_Billowing_Fog");
        if (vortexWispsPS == null) vortexWispsPS = GetOrCreateChildPS("2_Swirling_Vortex_Wisps");
        if (groundMistPS == null) groundMistPS = GetOrCreateChildPS("3_Ground_Creeping_Mist");
        if (soulOrbsPS == null) soulOrbsPS = GetOrCreateChildPS("4_Floating_Soul_Orbs");
        if (energyMotesPS == null) energyMotesPS = GetOrCreateChildPS("5_Sparkling_Energy_Motes");

        if (backdropFogPS != null) { backdropFogPS.gameObject.SetActive(enableBackdropFog); ConfigureBackdropFog(backdropFogPS); }
        if (vortexWispsPS != null) { vortexWispsPS.gameObject.SetActive(enableVortexWisps); ConfigureVortexWisps(vortexWispsPS); }
        if (groundMistPS != null) { groundMistPS.gameObject.SetActive(enableGroundMist); ConfigureGroundMist(groundMistPS); }
        if (soulOrbsPS != null) { soulOrbsPS.gameObject.SetActive(enableSoulOrbs); ConfigureSoulOrbs(soulOrbsPS); }
        if (energyMotesPS != null) { energyMotesPS.gameObject.SetActive(enableEnergyMotes); ConfigureEnergyMotes(energyMotesPS); }
    }

    private ParticleSystem GetOrCreateChildPS(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            child = go.transform;
        }
        ParticleSystem ps = child.GetComponent<ParticleSystem>();
        if (ps == null) ps = child.gameObject.AddComponent<ParticleSystem>();
        return ps;
    }

    private void ConfigureBackdropFog(ParticleSystem ps)
    {
        if (ps == null) return;
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(2.2f * particleScale, 3.4f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 65;

        var emission = ps.emission;
        emission.rateOverTime = 16f * densityMultiplier;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.length = vortexHeight * 1.1f;
        shape.radius = vortexRadius * 1.1f;
        shape.radiusThickness = 0.65f;
        shape.position = new Vector3(0f, 0.2f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = upwardSpeed * 0.75f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 0.5f;
        vol.radial = 0.04f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.55f);
        sc.AddKey(0.35f, 1.0f);
        sc.AddKey(0.75f, 1.35f);
        sc.AddKey(1.0f, 1.50f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(boneWhiteColor, 0.0f),
                new GradientColorKey(midtoneViolet, 0.35f),
                new GradientColorKey(deepCharcoalPurple, 0.70f),
                new GradientColorKey(rimHighlightLilac, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.65f * densityMultiplier, 0.18f),
                new GradientAlphaKey(0.60f * densityMultiplier, 0.65f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-25f * Mathf.Deg2Rad, 25f * Mathf.Deg2Rad);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.32f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.18f;
        noise.damping = true;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = smokePlumeMaterial;
        renderer.sortingOrder = 1;
    }

    private void ConfigureVortexWisps(ParticleSystem ps)
    {
        if (ps == null) return;
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.4f * particleScale, 2.2f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 45;

        var emission = ps.emission;
        emission.rateOverTime = 14f * densityMultiplier;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = vortexRadius * 0.95f;
        shape.donutRadius = 0.35f;
        shape.position = new Vector3(0f, vortexHeight * 0.35f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = upwardSpeed * 1.25f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 1.5f;
        vol.radial = 0.03f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.45f);
        sc.AddKey(0.35f, 1.0f);
        sc.AddKey(0.70f, 1.25f);
        sc.AddKey(1.0f, 0.40f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(boneWhiteColor, 0.0f),
                new GradientColorKey(midtoneViolet, 0.40f),
                new GradientColorKey(rimHighlightLilac, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.60f * densityMultiplier, 0.18f),
                new GradientAlphaKey(0.55f * densityMultiplier, 0.65f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-40f * Mathf.Deg2Rad, 40f * Mathf.Deg2Rad);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.45f;
        noise.scrollSpeed = 0.25f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = smokeWispMaterial != null ? smokeWispMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 3;
    }

    private void ConfigureGroundMist(ParticleSystem ps)
    {
        if (ps == null) return;
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.2f, 4.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(2.2f * particleScale, 3.8f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 35;

        var emission = ps.emission;
        emission.rateOverTime = 10f * densityMultiplier;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = vortexRadius * 1.35f;
        shape.radiusThickness = 0.8f;
        shape.position = new Vector3(0f, 0.05f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = 0.02f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 0.4f;
        vol.radial = 0.08f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.5f);
        sc.AddKey(0.5f, 1.0f);
        sc.AddKey(1.0f, 1.45f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(groundMistColor, 0.0f),
                new GradientColorKey(midtoneViolet, 0.45f),
                new GradientColorKey(boneWhiteColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.55f * densityMultiplier, 0.20f),
                new GradientAlphaKey(0.50f * densityMultiplier, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = smokePlumeMaterial;
        renderer.sortingOrder = 0;
    }

    private void ConfigureSoulOrbs(ParticleSystem ps)
    {
        if (ps == null) return;
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f * particleScale, 0.9f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.10f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-20f * Mathf.Deg2Rad, 20f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 6;

        var emission = ps.emission;
        emission.rateOverTime = 1.5f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = vortexRadius * 0.9f;
        shape.position = new Vector3(0f, vortexHeight * 0.65f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = upwardSpeed * 0.4f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 0.35f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.3f);
        sc.AddKey(0.25f, 1.0f);
        sc.AddKey(0.75f, 1.0f);
        sc.AddKey(1.0f, 0.2f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(boneWhiteColor, 0.0f),
                new GradientColorKey(midtoneViolet, 0.50f),
                new GradientColorKey(boneWhiteColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.80f, 0.20f),
                new GradientAlphaKey(0.75f, 0.75f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.1f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = soulOrbMaterial != null ? soulOrbMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 4;
    }

    private void ConfigureEnergyMotes(ParticleSystem ps)
    {
        if (ps == null) return;
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(moteMinSize * particleScale, moteMaxSize * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.20f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = moteMaxParticles;

        var emission = ps.emission;
        emission.rateOverTime = moteEmissionRate * densityMultiplier;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = vortexRadius * 0.95f;
        shape.radiusThickness = 0.85f;
        shape.length = vortexHeight * 1.0f;
        shape.position = new Vector3(0f, 0.20f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = upwardSpeed * 1.15f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * moteOrbitMultiplier;
        vol.radial = 0.02f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.0f);
        sc.AddKey(0.12f, 1.0f);
        sc.AddKey(0.32f, 0.40f);
        sc.AddKey(0.52f, 1.20f);
        sc.AddKey(0.72f, 0.50f);
        sc.AddKey(0.88f, 1.05f);
        sc.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-moteTwinkleSpeed * Mathf.Deg2Rad, moteTwinkleSpeed * Mathf.Deg2Rad);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(boneWhiteColor, 0.0f),
                new GradientColorKey(new Color(0.92f, 0.84f, 1.0f), 0.45f),
                new GradientColorKey(boneWhiteColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.95f, 0.15f),
                new GradientAlphaKey(0.90f, 0.75f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.18f;
        noise.frequency = 0.65f;
        noise.scrollSpeed = 0.30f;
        noise.damping = true;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = energyMoteMaterial != null ? energyMoteMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 5;
    }

    public void Play()
    {
        if (backdropFogPS != null && enableBackdropFog) backdropFogPS.Play();
        if (vortexWispsPS != null && enableVortexWisps) vortexWispsPS.Play();
        if (groundMistPS != null && enableGroundMist) groundMistPS.Play();
        if (soulOrbsPS != null && enableSoulOrbs) soulOrbsPS.Play();
        if (energyMotesPS != null && enableEnergyMotes) energyMotesPS.Play();
    }

    public void Stop()
    {
        if (backdropFogPS != null) backdropFogPS.Stop();
        if (vortexWispsPS != null) vortexWispsPS.Stop();
        if (groundMistPS != null) groundMistPS.Stop();
        if (soulOrbsPS != null) soulOrbsPS.Stop();
        if (energyMotesPS != null) energyMotesPS.Stop();
    }

    public void Clear()
    {
        if (backdropFogPS != null) backdropFogPS.Clear();
        if (vortexWispsPS != null) vortexWispsPS.Clear();
        if (groundMistPS != null) groundMistPS.Clear();
        if (soulOrbsPS != null) soulOrbsPS.Clear();
        if (energyMotesPS != null) energyMotesPS.Clear();
    }
}
```

---

## 8. Quick Verification Checklist for Future Agents

1. **Are shaders pink?**
   - Check `m_Shader` in `.mat` files: it must be `{fileID: 4800000, guid: 0406db5a14f94604a8c57ccfbc9f3b46, type: 3}` (URP Particles/Unlit).
2. **Are there green quads?**
   - Check `_SrcBlend: 5` and `_DstBlend: 1` in `MiasmaEnergyMoteMat.mat`.
   - Verify the texture has 0 alpha AND 0 RGB at its borders.
   - Verify `necroticGreenColor` is Bone White (`0.98, 0.95, 0.90`), NOT green.
3. **Are clouds visible and voluminous?**
   - Plume size must be `2.2m – 3.4m`.
   - Plume alpha in color-over-lifetime must reach `0.65`.
   - BaseColor on material must be `(1, 1, 1, 1)`.
4. **How to test in Unity**:
   - Select `Necromantic_Rotating_Smoke` in Hierarchy.
   - Click `🔄 Rebuild All Layers` in Inspector, or menu `Tools > VFX Setup > Configure Miasma Smoke in Scene`.

---

## 9. Affected Enemy Status Cloud VFX Specification (Images 4 & 5)

> **Purpose**: Standalone status debuff VFX applied to enemies affected by Alura's Miasma fog.

### 9.1 Visual Hierarchy & Components
```
Miasma_Affected_Enemy (Root - Prefab: Assets/Prefabs/Miasma_Affected_Enemy.prefab)
├── 1_Spectral_Skull_Icon     (Floating stylized glowing skull mask over face/chest - Image 4)
├── 2_Body_Mist_Shroud        (Volumetric necromantic fog cylinder wrapping enemy torso - Images 4 & 5)
├── 3_Waist_Orbit_Ring        (Spectral rotating waist ring circling the hips - Image 5)
├── 4a_Diamond_Sparkles       (Pure geometric ◆ Diamond Stars - Image 2, zero disc halo, HDR Bloom)
└── 4b_Cross_Sparkles         (Pure geometric + Greek Crosses - Image 3, zero disc halo, HDR Bloom)
```

### 9.2 Technical Implementation
- **Script**: `Assets/VFX/Scripts/MiasmaEnemyEffectVFX.cs` (`[ExecuteAlways]`)
- **Custom Inspector**: `Assets/VFX/Editor/MiasmaEnemyEffectVFXEditor.cs` with Play/Stop/Rebuild and presets.
- **Textures**:
  - `Assets/VFX/Textures/Miasma_Spectral_Skull.png` (256×256 stylized anime skull with spectral glow).
  - `Assets/VFX/Textures/Sparkle_DiamondStar.png` (256×256 pure geometric diamond star, 100% zero background disc).
  - `Assets/VFX/Textures/Sparkle_GreekCross.png` (256×256 pure geometric Greek cross, 100% zero background disc).
- **Materials**:
  - `Assets/VFX/Materials/MiasmaSpectralSkullMat.mat` (URP Particles/Unlit, Alpha Blend, `_EMISSION` enabled, HDR `{r: 3.5, g: 4.8, b: 4.0}`).
  - `Assets/VFX/Materials/MiasmaSparkleDiamondMat.mat` (Additive Blend `_SrcBlend: 5`, `_DstBlend: 1`, HDR emission `{5.0, 4.5, 6.5}`).
  - `Assets/VFX/Materials/MiasmaSparkleCrossMat.mat` (Additive Blend `_SrcBlend: 5`, `_DstBlend: 1`, HDR emission `{5.0, 4.5, 6.5}`).
- **Spawn Menus**:
  - Menu: `Tools > VFX Setup > Spawn Affected Enemy VFX in Scene`
  - Hierarchy / Right-Click: `GameObject > Effects > Miasma Affected Enemy VFX`

---

## 10. Pure Separate Shape Sparkles & High-Intensity Bloom Resolution

### 10.1 Problem & Root Cause
- In initial iterations, sparkles appeared as circular puffs / cloudy discs with faint diamond shapes in the center (`media_1790288491109.png`).
- **Root Cause**: The texture `EnergyMote_CrossStar.png` contained a soft radial gaussian halo intended for glow (`exp(-(r/0.35)^2)`), which rendered as a visible circular cloudy disc against the pink/purple fog.
- The user explicitly requested: *"i forgot to say i dont want the sparkle as a cloud sparkle should as seperate shapes"* and requested bloom on skull and sparkles.

### 10.2 Architectural Solution: Dedicated Pure Geometries
1. **Zero Halo Single Textures**:
   - `Sparkle_DiamondStar.png`: Crisp 4-point concave diamond star (Image 2 ◆), pure black (alpha = 0) outside the star geometry.
   - `Sparkle_GreekCross.png`: Crisp 4-arm flared Greek cross (Image 3 +), pure black (alpha = 0) outside the cross geometry.
2. **Dual Child Particle Systems**:
   - Instead of a single system using a texture atlas (which can bleed or distort aspect ratios), both `RotatingSmokeVFX.cs` and `MiasmaEnemyEffectVFX.cs` spawn two dedicated child particle systems:
     - `5a_Diamond_Sparkles` & `5b_Cross_Sparkles` (Main Miasma)
     - `4a_Diamond_Sparkles` & `4b_Cross_Sparkles` (Affected Enemy)
   - Texture Sheet Animation is completely **disabled** (`tsa.enabled = false`), eliminating atlas artifacts.
   - Legacy child systems (`5_Sparkling_Energy_Motes` and `4_Status_Sparkles`) are automatically found and destroyed in `InitializeEffect()` so old discs are wiped cleanly.
3. **Selective Bloom Threshold Calibration (Strictly VFX-Only)**:
   - To ensure the **character model has ZERO bloom** while the **sparkles and skull bloom brightly**, Bloom Threshold must be set strictly above standard LDR luminance (`> 1.0`):
   - `Assets/Settings/SampleSceneProfile.asset` & `Assets/Settings/DefaultVolumeProfile.asset`:
     - **Bloom Threshold: `1.15`** (Standard character textures/diffuse/specular max out at `1.0`, so the character receives `0.00%` bloom).
     - **Bloom Intensity: `1.85`** (Produces a rich, soft, emissive bloom aura on HDR elements).
     - **Bloom Scatter: `0.70`**.
   - Because the Spectral Skull and Sparkles use HDR emission values (`3.5` to `6.5`), they easily cross the `1.15` threshold and bloom brilliantly, leaving the character completely crisp and unaffected.
4. **Inspector Sliders**:
   - `sparkleBloomIntensity`: Range `1.0` to `10.0` (default `4.0`)
   - `skullBloomIntensity`: Range `1.0` to `10.0` (default `4.0`)
   - Controls emission multiplier in real time for dynamic adjustments.

### 10.3 Editor Safety & OnValidate Architecture
- **No Synchronous Hierarchy Changes in `OnValidate()`**:
  - Calling `new GameObject()`, `SetParent()`, `AddComponent<ParticleSystem>()`, or `DestroyImmediate()` inside `OnValidate()` triggers `SendMessage cannot be called during Awake, CheckConsistency, or OnValidate` and `Setting the parent of a transform which resides in a Prefab Asset is disabled`.
  - In `OnValidate()`:
    1. Guard with `if (!gameObject.scene.IsValid()) return;` to avoid running on disk Prefab assets.
    2. Only query existing children via `FindChildPS()` and update particle settings without creating/destroying objects.
    3. If children need creation or cleanup, schedule via `EditorApplication.delayCall += DelayedRebuild;` to execute cleanly outside the `OnValidate` callback frame.
- **Unused Fields Fixed**:
  - `skullPulseSpeed` is wired into `ConfigureSkullMask` to drive pulsing breathing frequency (`main.startLifetime = Mathf.Max(0.5f, 6.0f / skullPulseSpeed)`), resolving compiler warning `CS0414`.

---

## 11. Stage [4] Fog Dissipation & Cloud Elimination Architecture (Image 4)

### 11.1 Design Specification & User Requirements
- **Reference**: Image 4 (`media_1790291226710.png`):
  `[4] END / FOG DISSIPATION: "After 3 turns the fog dissipates. Particles fade. The board returns to normal."`
- **User Requests**:
  1. *"it should start from 0"*: All slider ranges (sparkles, bloom intensity, emission rates, particles, dissipation progress, height cutoffs) must start from `0`.
  2. *"also add the bolean and axis where it eliminate the clouds and stuff like that"*: Provide an instant boolean toggle to eliminate tall billowing clouds/wisps, plus an axis controller (`Vertical_Y`, `Radial_XZ`, `Both_Y_and_XZ`) to control dissipation dynamics along individual or combined axes.

### 11.2 Core Dissipation Mechanics (`RotatingSmokeVFX.cs`)

```
                          Y-Axis Cutoff (Vertical_Y)
                                     │
           [1_Backdrop_Billowing_Fog]  ▼ Height shrinks down to 0
           [2_Swirling_Vortex_Wisps]   ▼ Alpha fades out (1 -> 0)
           [4_Floating_Soul_Orbs]      ▼ Deactivated when cutoff <= 0.05m
                                     │
           ──────────────────────────┼────────────────────────── Ground Plane
                                     │
           [3_Ground_Creeping_Mist]  ◄───► Mist expands radially outward
           [5a_Diamond_Sparkles]           from 1.7m to 4.5m (Radial_XZ)
           [5b_Cross_Sparkles]             Alpha fades to 0 -> Board returns to normal
```

#### 1. Instant Boolean Elimination (`EliminateClouds`)
- **Toggle**: `eliminateClouds` (`bool`)
- When enabled (`true`):
  - `1_Backdrop_Billowing_Fog.SetActive(false)`
  - `2_Swirling_Vortex_Wisps.SetActive(false)`
  - `4_Floating_Soul_Orbs.SetActive(false)`
  - Ground creeping mist (`3_Ground_Creeping_Mist`) and star sparkles (`5a`, `5b`) remain active on the floor plane.
  - Zero GPU/CPU overhead for eliminated cloud layers.
- When toggled back (`false`), layers immediately re-enable and resume playback.

#### 2. Multi-Axis Dissipation Control (`DissipationAxisType`)
- **Axis Options**:
  - `Vertical_Y`: Shrinks cloud cylinder/donut shapes downward from `vortexHeight` to `0` along the Y axis, eliminating tall billowing clouds first.
  - `Radial_XZ`: Spreads the ground creeping mist outward from radius `1.7m` to `dissipationRadialSpread` (default `4.5m`) across the floor plane while fading opacity to `0`.
  - `Both_Y_and_XZ` (Default): Simultaneously drops cloud height on Y and spreads mist radially on XZ, matching Image 4 where the fog rolls outward and all particles fade to clear the battlefield.

#### 3. Real-Time Sliders (All Starting from 0)
- `dissipationProgress`: `[0f, 1f]` (0 = full active miasma vortex, 1 = 100% eliminated / clear).
- `cloudHeightCutoff`: `[0f, 5f]` (default `2.4f`). Scrubbing down to 0 clips clouds from the top down.
- `dissipationRadialSpread`: `[0f, 8f]` (default `4.5f`). Target radius for mist dissipation crawl.
- `dissipationDuration`: `[0f, 10f]` (default `3.0f`). Animation transition time.
- `moteEmissionRate`: `[0f, 250f]` (default `65f`).
- `moteMaxParticles`: `[0, 400]` (default `150`).
- `moteOrbitMultiplier`: `[0f, 5.0f]` (default `1.8f`).
- `sparkleBloomIntensity`: `[0f, 10.0f]` (default `4.0f`).
- In `MiasmaEnemyEffectVFX.cs`: `sparkleRate` `[0f, 100f]`, `skullBloomIntensity` `[0f, 10.0f]`, `sparkleBloomIntensity` `[0f, 10.0f]`.

#### 4. Dual-Mode Animation (Play Mode & Edit Mode)
- **Play Mode**: Runs smooth `IEnumerator AnimateDissipation(duration)` with `Time.deltaTime`.
- **Edit Mode**: Runs an `EditorApplication.update` timer (`TriggerEditorDissipation`), allowing artists to test the 3-second dissipation animation live in the Scene view with one click without entering Play mode!
- **Methods**:
  - `smoke.TriggerDissipation()` / `smoke.TriggerDissipation(duration)`
  - `smoke.ResetDissipation()` (restores full miasma vortex instantly).

### 11.3 Custom Inspector GUI (`RotatingSmokeVFXEditor.cs`)
- Added dedicated **Stage [4] Fog Dissipation & Cloud Elimination** box:
  - `[❌ Eliminate Clouds (Instant)]` / `[☁️ Restore Clouds]` quick toggle button.
  - `[💨 Trigger Dissipation (Fade)]` triggers animated 3-turn dissipation.
  - `[↺ Reset Full Fog]` resets all dissipation parameters back to full vortex.
- Quick Presets ("Cloudy Power" and "Subtle Wispy") reset dissipation fields to ensure presets always load in full bloom.

---

## 12. Sparkle Rate Slider Toggle & Small/Big Size Hierarchy Architecture

### 12.1 Problem Analysis (`media_1790291408413.png`)
- **Visual Defect**: In the user's test screenshot, the scene was overwhelmed by a blizzard of large, uniform white diamond boxes orbiting the character.
- **Root Causes**:
  1. Default emission rates (`65/sec` on Smoke, `35/sec` on Enemy) flooded the screen with over 150 concurrent large billboard particles.
  2. Particle sizes (`0.08m` - `0.14m`) were scaled too uniformly, lacking delicate variation between tiny dust twinkles and prominent accent stars.
  3. No slider existed that allowed the user to slide the sparkle count all the way to `0` to completely toggle/silence sparkles.
- **User Requirement**:
  *"i have to toggle the all the sparcle rate of it from a slider also for the enemy small big and others"*

### 12.2 Unified Sparkle Control Hierarchy

Implemented identically across **both** `RotatingSmokeVFX.cs` (Main Miasma) and `MiasmaEnemyEffectVFX.cs` (Affected Enemy):

```
                                  Master Sparkle Controls
                                             │
                       ┌─────────────────────┴─────────────────────┐
                       ▼                                           ▼
             Rate Controls (Slider-as-Toggle)            Size Controls (Small & Big)
        ┌───────────────────────────────────────┐    ┌───────────────────────────────────┐
        │ • Master Sparkle Rate Slider [0-250]  │    │ • Small Sparkle Size [0 - 0.25m]  │
        │   (Pull to 0 to completely turn off)  │    │   (Tiny delicate star motes)      │
        │ • Diamond Rate Slider (◆)             │    │ • Big Sparkle Size [0 - 0.50m]    │
        │ • Cross Rate Slider (+)               │    │   (Prominent hero accent stars)   │
        │ • Master Toggle Button & Checkbox     │    │ • Sparkle Scale Slider [0 - 3.0x] │
        └───────────────────────────────────────┘    └───────────────────────────────────┘
```

#### 1. Slider-as-a-Toggle Mechanics
- **Master Rate Slider (`moteEmissionRate` / `sparkleRate`)**:
  - Pulling the slider down to `0` immediately stops particle emission and deactivates the sparkle GameObject layers (`active = false`), effectively acting as a direct toggle right from the slider.
  - As the slider is moved above `0`, sparkles scale up proportionally and smoothly.
- **Independent Shape Rates ("and others")**:
  - `diamondSparkleRate`: Controls the emission of 4-point Diamond Stars (◆). Set to `0` to eliminate diamonds.
  - `crossSparkleRate`: Controls the emission of 4-arm Greek Crosses (+). Set to `0` to eliminate crosses.
  - Shape Toggles: `enableDiamondSparkles` and `enableCrossSparkles` checkboxes.

#### 2. Size Hierarchy ("Small, Big & Others")
- **Small Size Slider (`moteMinSize` / `smallSparkleSize`)**:
  - Calibrated default `0.02m` - `0.025m`.
  - Generates tiny, delicate background star dust motes matching the reference concept art.
- **Big Size Slider (`moteMaxSize` / `bigSparkleSize`)**:
  - Calibrated default `0.055m` - `0.075m`.
  - Generates clear, crisp hero accent stars that twinkle brightly.
- **Master Sparkle Scale Slider (`sparkleScale`)**:
  - Range `[0f, 3.0f]`, default `1.0f`.
  - Enables artists to resize all sparkles smaller or bigger simultaneously with a single slider.
- **Unity MinMaxCurve Integration**:
  ```csharp
  float minS = Mathf.Max(0.002f, smallSparkleSize * sparkleScale * particleScale);
  float maxS = Mathf.Max(minS, bigSparkleSize * sparkleScale * particleScale);
  main.startSize = new ParticleSystem.MinMaxCurve(minS, maxS);
  ```
  Unity automatically samples particle sizes smoothly between the small and big sliders, creating a rich, natural starry atmosphere.

### 12.3 Custom Inspector GUI (`RotatingSmokeVFXEditor.cs` & `MiasmaEnemyEffectVFXEditor.cs`)
- Added dedicated **`✨ Sparkle Controls (Small, Big & Shape Rates)`** box in both inspector editors:
  - `[✨ Sparkles: ACTIVE]` / `[⭕ Sparkles: DISABLED]` single-click master toggle button.
  - `[Subtle Tiny Motes (Quick Set)]` instant calibration button to instantly restore calm, elegant star dust.
- Cleanly organized sliders for Small Size, Big Size, Sparkle Scale, Master Rate, Diamond Rate, and Cross Rate.
- Backwards compatible with all existing presets, scenes, and serialized prefabs.

---

## 13. Drastic Sparkle Reduction, Streamlined Friendly Controls & Dedicated Stage [4] Fog Diffusion VFX

### 13.1 User Request & Problem Diagnosis
- **User Feedback**:
  1. *"i dont want too much ssparkles kindly reduce it"*: Screenshot `media_1790292698006.png` revealed an overwhelming blizzard of hundreds of large white diamond particles obstructing the character and ground.
  2. *"and also for the controls kindly make it user friendly do [not] add too much off contols just add a slight sparkle quanity with both"*: The inspector was cluttered with a dizzying wall of technical variables (`moteMinSize`, `moteMaxSize`, `sparkleScale`, `moteEmissionRate`, `diamondSparkleRate`, `crossSparkleRate`, `moteMaxParticles`, etc.).
  3. *"and for stage for make it sperate diffusion iffect of all as i show on the image like you created for enemy"*: Create a **separate standalone Diffusion VFX component and prefab** for Stage [4] ("After 3 turns the fog dissipates. Particles fade. The board returns to normal" from Image 4 `media_1790291226710.png`), parallel to how the enemy effect was built as a standalone entity.

### 13.2 Sparkle Density Drastically Reduced
- Capped maximum active particle pool from `120-150` down to `15-20` max.
- Emission rates lowered from `35-65/sec` down to subtle `2-3/sec` per shape (only ~5-8 active motes total around the effect at any instant).
- Particle sizes calibrated from bloated `0.075-0.14m` down to delicate `0.028-0.032m`.
- Bloom intensity adjusted from `4.0x` down to `2.5x` for crisp, sharp twinkling points instead of blurry white blocks.

### 13.3 User-Friendly Sparkle Control Scheme
Eliminated the confusing array of duplicate sliders. Replaced with 3 clean, intuitive inspector controls in both `RotatingSmokeVFX` and `MiasmaEnemyEffectVFX`:
1. **Sparkle Quantity Slider (`[0, 20]` for Smoke, `[0, 15]` for Enemy)**:
   - `0` = Completely OFF (slider acts as an intuitive toggle).
   - `4-5` = Subtle, delicate ambient twinkles (calibrated default).
   - Automatically distributes emission between Diamond Stars (◆ 55%) and Greek Crosses (+ 45%).
2. **Sparkle Size Slider (`[0.01, 0.08]` for Smoke, `[0.01, 0.06]` for Enemy)**:
   - Single slider controlling the star mote scale with gentle internal variance (0.8x - 1.25x).
3. **Sparkle Glow / Bloom Slider (`[0, 6]` default `2.5`)**:
   - Controls emissive intensity without washing out into blurry blobs.
4. **Clean Inspector Buttons**:
   - `[✨ Sparkles: Active / Disabled]` master toggle.
   - `[🌸 Reset to Delicate Twinkles (Default)]` 1-click restore.

### 13.4 Dedicated Stage [4] Standalone Fog Diffusion VFX
Built as a separate, modular component and prefab modeled directly after Image 4 (`media_1790291226710.png`):
- **Script**: `Assets/VFX/Scripts/MiasmaDissipationVFX.cs`
- **Editor**: `Assets/VFX/Editor/MiasmaDissipationVFXEditor.cs`
- **Prefab**: `Assets/Prefabs/Miasma_Fog_Dissipation.prefab`
- **Menu Item**: `Tools > VFX Setup > Spawn Stage 4 Fog Diffusion VFX in Scene`

#### Core Particle Systems in Stage 4:
1. `1_Ground_Diffusion_Mist`:
   - Spills across the grid tiles, creeping radially outward (`startRadius: 1.7m -> diffusionSpreadRadius: 4.5m`) hugging the floor (`Y = 0.04m`) and dissolving.
2. `2_Ascending_Evaporation_Wisps`:
   - Ethereal plumes lifting off the floor and dispersing into thin air (`Y velocity: 0.65m/s`).
3. `3_Cleansing_Floor_Ripple`:
   - Ethereal boundary shockwave expanding across the floor tiles to demarcate the clearing edge of the mist.
4. `4_Lingering_Star_Twinkles`:
   - A slight quantity (3-4) of delicate Diamond (◆) and Cross (+) motes that softly extinguish as the board clears.

#### 3-Turn Step Controller:
- `CurrentTurn` (1, 2, 3) or `DissipationProgress` slider (`0.0 to 1.0`):
  - **Turn 1 (Thinning)**: Fog lowers and pools toward the floor tiles.
  - **Turn 2 (Floor Crawl)**: Ground mist expands radially outward across the board.
  - **Turn 3 (Board Clear)**: Particles dissolve, returning the board completely to normal.
- Automated playback via `TriggerDissipation(duration)` or step-by-step turn buttons in Editor & Runtime.
- Optional `targetMiasmaFog` link: seamlessly fades the main vortex in sync if assigned, or operates 100% autonomously as a standalone VFX prefab.

---

## 14. Directional +Z Axis Sweep, Smoke Tail Deformation & Complete Dispersion/Vanishing

### 14.1 User Request & Visual Specification
- **User Request**: *"Miasma_Fog_Dissipation when it triggers it should move al the particles to the Z axis and defrom to the tail and dipersed vanished all the particles cloud and stuff"*
- **Annotated Image Reference (`media_1790297436663.png`)**: A prominent red arrow drawn across the isometric game grid pointing forward from Alura Graves along the **+Z axis** (with a slight +X angle along the diagonal grid toward the enemy line).
- **Core Requirements Implemented**:
  1. **Z-Axis Directional Sweep**: When `Miasma_Fog_Dissipation` triggers (or as `dissipationProgress` increases from 0 to 1), all particles (ground diffusion mist, evaporation wisps, cleansing ripple, star motes, and linked main fog clouds) are propelled forward along the **+Z axis** following the red arrow trajectory.
  2. **Deform to Tail (Aerodynamic Particle Elongation)**: Particles deform from round stationary billboards into elongated, streamlined wispy smoke tails pointing along their motion heading using `ParticleSystemRenderMode.Stretch` with velocity and length scaling.
  3. **Disperse & Vanish Completely**: As particles stretch and rush along +Z, lateral dispersion forces push them outward, emission terminates at 75% progress, alpha fades to 0%, and at 100% progress all particles vanish completely, returning the game board 100% to normal.
  4. **Full Two-Way Coordination**: Both `MiasmaDissipationVFX` and `RotatingSmokeVFX` coordinate their Z-sweep and tail deformation so the entire miasma effect dissipates cleanly as a unified phenomenon.

### 14.2 Mathematical & Technical Implementation

#### A. Directional Sweep Module (`velocityOverLifetime`)
- **Direction Modes**:
  - `Diagonal_Z_RedArrow` (Default): Normalized vector `(0.35f, 0.05f, 1.0f).normalized` matching the exact diagonal grid orientation in Image 4.
  - `Forward_Z`: Standard `(0f, 0f, 1f)` World/Local Z-forward.
  - `Custom_Direction`: User-defined 3D vector.
- **Velocity Formula**:
  `sweepSpeed = zSweepSpeed * Mathf.Clamp01(dissipationProgress * 1.5f);`
  `vol.space = sweepSpace;` (Defaults to `ParticleSystemSimulationSpace.World`)
  `vol.x = dir.x * sweepSpeed;`
  `vol.y = dir.y * sweepSpeed + upwardDrift * (1f - dissipationProgress);`
  `vol.z = dir.z * sweepSpeed;`
  `vol.radial = baseRadial + lateralDispersion * progress;`

#### B. Dynamic Tail Deformation (`ParticleSystemRenderer.renderMode`)
- When `dissipationProgress <= 0.02f`: Particles render as standard `ParticleSystemRenderMode.Billboard`.
- When `dissipationProgress > 0.02f`:
  - `renderer.renderMode = ParticleSystemRenderMode.Stretch;`
  - `renderer.velocityScale = Mathf.Lerp(0.3f, tailDeformationScale, Mathf.Clamp01(dissipationProgress * 1.5f));`
  - `renderer.lengthScale = Mathf.Lerp(1.0f, tailLengthScale, Mathf.Clamp01(dissipationProgress * 1.5f));`
- As particles accelerate forward along +Z, their geometry dynamically elongates in proportion to their speed, rendering as wispy trailing smoke ribbons.

#### C. Complete Vanishing Mechanics
- **Emission Shutdown**: At `dissipationProgress >= 0.75f`, `emission.rateOverTime = 0f;` preventing new particles from spawning.
- **Alpha Decay**: Color gradients fade alpha down to 0 as progress nears 1.0.
- **Board Clear**: At `dissipationProgress >= 0.99f`, all child particle systems are cleared and deactivated (`boardReturnedToNormal = true`), leaving 0 active particles and 0 draw calls.
- **Board Reset**: `ResetBoard()` / `ResetDissipation()` restores full fog and restarts emission cleanly via `Play()`.

### 14.3 Updated Components & Prefabs
1. `Assets/VFX/Scripts/MiasmaDissipationVFX.cs`:
   - Added Z-axis sweep direction modes, speed, tail deformation scale, length scale, lateral dispersion, and complete vanishing logic across all 5 layers.
2. `Assets/VFX/Scripts/RotatingSmokeVFX.cs`:
   - Added `Z_Axis_Forward` and `Combined_Z_Sweep_And_Radial` dissipation modes.
   - Updated Backdrop Fog, Vortex Wisps, Ground Mist, Soul Orbs, and Sparkles to sweep along Z, deform into tails, and vanish.
3. `Assets/VFX/Editor/MiasmaDissipationVFXEditor.cs`:
   - Added dedicated `🏹 Z-Axis Directional Sweep & Tail Deformation` inspector panel with 1-click trigger button.
### 14.4 Sparkle Preservation (Billboards Only) & Smoke Tail Vector Alignment
- **Problem**: When `enableTailDeformation` stretched particles via `ParticleSystemRenderMode.Stretch`, star sparkles (Diamond ◆ and Greek Cross +) were inadvertently stretched into long, flat, horizontal white rods/bars floating around the character (as shown in user feedback). Furthermore, rotation over lifetime caused stretched smoke plumes to spin sideways off their travel vector.
- **Sparkle Billboard Enforcement**:
  - Star sparkles and soul orbs **NEVER** use `ParticleSystemRenderMode.Stretch`.
  - Both `ConfigureDiamondSparkles` and `ConfigureCrossSparkles` in `RotatingSmokeVFX.cs` and `MiasmaDissipationVFX.cs` unconditionally enforce:
    ```csharp
    renderer.renderMode = ParticleSystemRenderMode.Billboard;
    renderer.velocityScale = 0f;
    renderer.lengthScale = 1f;
    ```
  - Sparkle sweep velocity is softened (`* 0.6f`) so sparkles drift gracefully along the Z sweep while continuing their delicate, crisp twinkling rotation and size shimmer, smoothly fading their alpha to 0 as dissipation reaches 1.0.
- **Smoke Tail Alignment**:
  - Smoke plumes (`ConfigureBackdropFog`, `ConfigureVortexWisps`) only apply `rotationOverLifetime` when `dissipationProgress <= 0.02f`.
  - During the dissipation stretch (`dissipationProgress > 0.02f`), rotation is disabled (`rol.enabled = false`) so the aerodynamic smoke ribbons stay strictly aligned with the +Z travel trajectory without spinning off-axis.

---



