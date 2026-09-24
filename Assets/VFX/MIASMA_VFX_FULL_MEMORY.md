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
└── 5_Sparkling_Energy_Motes     (Emissive additive cloudy sparkles)
```

### Detailed Layer Parameters

| Layer | Particles (Max / Rate) | Start Size | Lifetime | Shape & Velocity | Color & Opacity |
|---|---|---|---|---|---|
| **1. Billowing Fog** | 65 max / 16/s | 2.2m – 3.4m | 3.5s – 5.2s | Cylinder (`r=1.8m`, `h=2.6m`), Y `0.30`, Orbit `30°/s`, Noise `0.32` | Bone White ➔ Lavender. Alpha peak `0.65`. Expands `1.5×` over life. |
| **2. Vortex Wisps** | 45 max / 14/s | 1.4m – 2.2m | 2.2s – 3.4s | Donut (`r=1.6m`), Y `0.50`, Orbit `90°/s` (Fast swirl) | Bone White ➔ Vibrant Lavender. Alpha peak `0.60`. |
| **3. Ground Mist** | 35 max / 10/s | 2.2m – 3.8m | 3.2s – 4.8s | Flat Circle (`r=2.3m`, `y=0.05m`), Radial crawl `0.08` | Bone White with lavender tint. Alpha peak `0.55`. |
| **4. Soul Orbs** | 6 max / 1.5/s | 0.5m – 0.9m | 3.5s – 5.0s | Sphere (`r=1.5m`, `y=1.5m`), gentle upward drift | Luminous Bone White core, soft lavender aura. |
| **5. Energy Motes** | 45 max / 22/s | 0.20m – 0.42m | 1.4s – 2.4s | Donut (`r=1.5m`), Y `0.40`, Fast orbit `110°/s` | Additive emissive Bone White & glowing Lavender. Alpha `0.90`. |

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
#    - Gaussian glow core with soft misty halo
#    - Pure zero at radius 0.78 (no square borders)
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
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.20f * particleScale, 0.42f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 45;

        var emission = ps.emission;
        emission.rateOverTime = 22f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = vortexRadius * 0.90f;
        shape.donutRadius = 0.40f;
        shape.position = new Vector3(0f, vortexHeight * 0.45f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = upwardSpeed * 1.0f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 1.8f;
        vol.radial = 0.02f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.2f);
        sc.AddKey(0.3f, 1.0f);
        sc.AddKey(0.7f, 0.9f);
        sc.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(boneWhiteColor, 0.0f),
                new GradientColorKey(new Color(0.88f, 0.74f, 1.0f), 0.45f),
                new GradientColorKey(boneWhiteColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.90f, 0.18f),
                new GradientAlphaKey(0.85f, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.7f;
        noise.scrollSpeed = 0.35f;

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
