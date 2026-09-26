using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ArthurGhostTrailVFX:
/// Attaches directly to King Arthur or character root.
/// Parents all clones in a container under the character (Ghost_Clones).
/// Clones the active character model (King_Arthur_Idle_Animation) with HideFlags.DontSave,
/// synchronizes all bones and attached meshes (Beard, Crown, Sword, Body, Face, etc.) to the
/// exact current animation frame without resetting, applies the glowing golden trail dissolve shader,
/// and applies a position offset so it doesn't overlap the character.
/// </summary>
[AddComponentMenu("VFX/King Arthur/Arthur Ghost Trail VFX")]
[ExecuteAlways]
public class ArthurGhostTrailVFX : MonoBehaviour
{
    public enum GhostSpawnTrigger
    {
        FramesWhileMoving,
        FramesContinuous,
        DistanceThreshold
    }

    [Header("=== Character Source Model ===")]
    [Tooltip("The character model GameObject to duplicate (e.g. King_Arthur_Idle_Animation). If null, auto-detects active animated child.")]
    public GameObject characterModel;

    [Header("=== Ghost Trail Cadence & Timing ===")]
    [Tooltip("Enable / disable the ghost trail effect")]
    public bool enableGhostTrail = true;

    [Tooltip("Trigger mode for spawning ghosts:\n• FramesWhileMoving: Spawns every N frames when moving or animating (recommended)\n• FramesContinuous: Spawns every N frames continuously\n• DistanceThreshold: Spawns whenever moved by X meters")]
    public GhostSpawnTrigger triggerMode = GhostSpawnTrigger.FramesWhileMoving;

    [Tooltip("Spawn a ghost snapshot every N frames (Default = 5 frames as requested)")]
    [Range(1, 30)]
    public int frameInterval = 5;

    [Tooltip("Lifetime duration of each ghost in seconds before fully dissolving")]
    [Range(0.1f, 3.0f)]
    public float ghostDuration = 0.6f;

    [Tooltip("Minimum movement distance in meters to spawn a ghost (prevents ghost piling while standing still)")]
    [Range(0.0f, 1.0f)]
    public float minMovementDistance = 0.02f;

    [Tooltip("If true, spawns ghosts continuously even when character stands in place (for in-place attacks, taunts, or idle animations)")]
    public bool spawnWhileIdle = true;

    [Header("=== Scale & Position Offset ===")]
    [Tooltip("Scale multiplier for ghost duplicates. 1.0 = exact 1:1 identical size with character.")]
    [Range(0.1f, 2.0f)]
    public float ghostScale = 1.0f;

    [Tooltip("3D non-uniform scale multiplier if you need to adjust Width (X), Height (Y), or Depth (Z) independently.")]
    public Vector3 ghostScaleMultiplier = Vector3.one;

    [Tooltip("Position offset applied to the ghost clones so they don't overlap directly on top of the character. Default -0.35 on Z places it directly behind the character.")]
    public Vector3 positionOffset = new Vector3(0f, 0f, -0.35f);

    [Tooltip("If true, positionOffset is evaluated in character's local orientation (-Z is always behind character's back regardless of which way they face).")]
    public bool offsetInLocalSpace = true;

    [Tooltip("If true, each ghost snapshot stays frozen at the world position where it was spawned as character moves forward (creates a motion trail).")]
    public bool leaveTrailInWorldSpace = true;

    [Header("=== Golden Glow & Shading ===")]
    [Tooltip("Dedicated ghost trail material (using VFX/King Arthur/Golden Ghost Trail shader). Auto-loaded if null.")]
    public Material ghostMaterial;

    [ColorUsage(true, true), Tooltip("Core golden holy body tint (HDR)")]
    public Color ghostColor = new Color(1.0f, 0.85f, 0.38f, 0.85f);

    [ColorUsage(true, true), Tooltip("Silhouette rim glow emission (HDR)")]
    public Color emissionColor = new Color(2.4f, 1.8f, 0.6f, 1.0f);

    [Range(0.5f, 8.0f), Tooltip("Bloom intensity multiplier for radiant golden glow")]
    public float bloomMultiplier = 1.8f;

    [ColorUsage(true, true), Tooltip("Searing burn edge color at the dissolve boundary (HDR)")]
    public Color dissolveBurnColor = new Color(3.5f, 2.6f, 0.9f, 1.0f);

    [Range(0.01f, 0.3f), Tooltip("Width of the glowing burning dissolve boundary")]
    public float dissolveEdgeWidth = 0.08f;

    [Tooltip("Scale of 3D organic procedural dissolve noise across character mesh")]
    public float noiseScale = 5.5f;

    [Range(-1.0f, 2.0f), Tooltip("Vertical bias for dissolve (higher value = dissolves bottom-up like rising golden motes)")]
    public float verticalBias = 0.4f;

    [Header("=== Dissolve & Opacity Curves ===")]
    [Tooltip("Opacity fade curve over ghost lifetime (1.0 -> 0.0)")]
    public AnimationCurve opacityCurve = new AnimationCurve(
        new Keyframe(0.0f, 1.0f, 0.0f, -0.4f),
        new Keyframe(0.4f, 0.85f, -0.5f, -0.5f),
        new Keyframe(1.0f, 0.0f, -2.0f, 0.0f)
    );

    [Tooltip("Dissolve progress curve over ghost lifetime (0.0 -> 1.0)")]
    public AnimationCurve dissolveCurve = new AnimationCurve(
        new Keyframe(0.0f, 0.0f, 0.0f, 0.0f),
        new Keyframe(0.3f, 0.05f, 0.3f, 0.3f),
        new Keyframe(0.7f, 0.55f, 1.5f, 1.5f),
        new Keyframe(1.0f, 1.0f, 2.0f, 0.0f)
    );

    [Header("=== Pooling & Optimization ===")]
    [Range(2, 30), Tooltip("Maximum concurrent active ghosts allowed simultaneously")]
    public int maxConcurrentGhosts = 10;

    // Shader property IDs for zero-garbage MaterialPropertyBlock updates
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int PropEmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int PropBloomMult = Shader.PropertyToID("_BloomMultiplier");
    private static readonly int PropBurnColor = Shader.PropertyToID("_DissolveBurnColor");
    private static readonly int PropBurnWidth = Shader.PropertyToID("_DissolveEdgeWidth");
    private static readonly int PropNoiseScale = Shader.PropertyToID("_NoiseScale");
    private static readonly int PropVertBias = Shader.PropertyToID("_VerticalBias");
    private static readonly int PropOpacity = Shader.PropertyToID("_Opacity");
    private static readonly int PropDissolve = Shader.PropertyToID("_DissolveProgress");

    private const string GHOST_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_GoldenGhostTrail_Mat.mat";
    private const string GHOST_SHADER_NAME = "VFX/King Arthur/Golden Ghost Trail";
    public const string CONTAINER_NAME = "Ghost_Clones";

    private struct TransformPair
    {
        public Transform source;
        public Transform clone;
    }

    private class GhostInstance
    {
        public GameObject rootObject;
        public List<TransformPair> transformPairs = new List<TransformPair>();
        public List<Renderer> renderers = new List<Renderer>();
        public Vector3 spawnWorldPos;
        public Quaternion spawnWorldRot;
        public Vector3 spawnWorldScale;
        public float spawnTime;
        public float lifetime;
        public bool isActive;
    }

    private readonly List<GhostInstance> activeGhosts = new List<GhostInstance>();
    private readonly Queue<GhostInstance> availableGhosts = new Queue<GhostInstance>();
    private Transform ghostClonesContainer;
    private MaterialPropertyBlock propertyBlock;
    private GameObject currentSourceModel;

    private int frameCounter = 0;
    private Vector3 lastSpawnPosition;
    private Vector3 lastMotionRefPosition;

    private void Awake()
    {
        CleanUpLegacyPool();
        Initialize();
    }

    private void OnEnable()
    {
        // Prevent duplicate ghost execution if component exists on both parent and child
        if (transform.parent != null)
        {
            ArthurGhostTrailVFX parentVFX = transform.parent.GetComponent<ArthurGhostTrailVFX>();
            if (parentVFX != null && parentVFX.enabled)
            {
                Debug.LogWarning($"[ArthurGhostTrailVFX] Parent '{transform.parent.name}' already has ArthurGhostTrailVFX enabled. Disabling component on '{gameObject.name}' to prevent duplicate ghosts.");
                enabled = false;
                return;
            }
        }

        CleanUpLegacyPool();
        Initialize();
        lastSpawnPosition = transform.position;
        frameCounter = 0;
    }

    private void OnDisable()
    {
        ClearAllGhosts();
    }

    private void OnDestroy()
    {
        DestroyPoolContainer();
    }

    public void Initialize()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        LoadGhostMaterial();
        ResolveSourceModel();
        EnsureContainer();
    }

    /// <summary>
    /// Finds or validates the active character model to duplicate.
    /// </summary>
    public GameObject ResolveSourceModel()
    {
        if (characterModel != null && characterModel.activeInHierarchy)
        {
            if (currentSourceModel != characterModel)
            {
                currentSourceModel = characterModel;
                DestroyPoolContainer();
            }
            return characterModel;
        }

        // Auto-detect:
        // 1. Search direct children for the active animated model (e.g. King_Arthur_Idle_Animation)
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == CONTAINER_NAME) continue;
            if (child.gameObject.activeInHierarchy && (child.GetComponent<Animator>() != null || child.GetComponentInChildren<SkinnedMeshRenderer>() != null))
            {
                characterModel = child.gameObject;
                if (currentSourceModel != characterModel)
                {
                    currentSourceModel = characterModel;
                    DestroyPoolContainer();
                }
                return characterModel;
            }
        }

        // 2. If this GameObject itself has an Animator or SkinnedMeshRenderer
        if (GetComponent<Animator>() != null || GetComponent<SkinnedMeshRenderer>() != null)
        {
            characterModel = gameObject;
            currentSourceModel = characterModel;
            return characterModel;
        }

        // 3. Fallback: first active child that is not the clones container
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == CONTAINER_NAME) continue;
            if (child.gameObject.activeInHierarchy)
            {
                characterModel = child.gameObject;
                currentSourceModel = characterModel;
                return characterModel;
            }
        }

        characterModel = gameObject;
        currentSourceModel = characterModel;
        return characterModel;
    }

    private void LoadGhostMaterial()
    {
        if (ghostMaterial != null && ghostMaterial.shader != null && ghostMaterial.shader.name == GHOST_SHADER_NAME)
        {
            return;
        }

#if UNITY_EDITOR
        ghostMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(GHOST_MAT_PATH);
#endif
        if (ghostMaterial == null)
        {
            Shader shader = Shader.Find(GHOST_SHADER_NAME);
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }
            ghostMaterial = new Material(shader);
            ghostMaterial.name = "KingArthur_GoldenGhostTrail_Runtime";
        }
    }

    /// <summary>
    /// Ensures that the container for all ghost clones is parented directly UNDER the character.
    /// Clones are never left as orphan root objects in the scene.
    /// </summary>
    private void EnsureContainer()
    {
        // Container must be parented UNDER the character
        Transform parentTransform = transform;
        if (transform.parent != null && (name.Contains("Animation") || name.Contains("Model")))
        {
            parentTransform = transform.parent;
        }

        if (ghostClonesContainer == null)
        {
            Transform existing = parentTransform.Find(CONTAINER_NAME);
            if (existing != null)
            {
                ghostClonesContainer = existing;
            }
            else
            {
                GameObject containerGO = new GameObject(CONTAINER_NAME);
                containerGO.transform.SetParent(parentTransform, false);
                containerGO.transform.localPosition = Vector3.zero;
                containerGO.transform.localRotation = Quaternion.identity;
                containerGO.transform.localScale = Vector3.one;
                // Never serialize runtime preview clones into the scene file
                containerGO.hideFlags = HideFlags.DontSave;
                ghostClonesContainer = containerGO.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (!enableGhostTrail)
        {
            UpdateActiveGhosts();
            return;
        }

        GameObject source = ResolveSourceModel();
        if (source == null)
        {
            UpdateActiveGhosts();
            return;
        }

        bool shouldSpawn = false;

        if (triggerMode == GhostSpawnTrigger.FramesContinuous)
        {
            frameCounter++;
            if (frameCounter >= frameInterval)
            {
                frameCounter = 0;
                shouldSpawn = true;
            }
        }
        else if (triggerMode == GhostSpawnTrigger.DistanceThreshold)
        {
            float dist = Vector3.Distance(transform.position, lastSpawnPosition);
            if (dist >= minMovementDistance)
            {
                shouldSpawn = true;
            }
        }
        else // FramesWhileMoving (Default)
        {
            frameCounter++;
            if (frameCounter >= frameInterval)
            {
                frameCounter = 0;
                float rootDist = Vector3.Distance(transform.position, lastSpawnPosition);

                Vector3 currentMotionRef = source.transform.position;
                float motionRefDist = Vector3.Distance(currentMotionRef, lastMotionRefPosition);

                if (spawnWhileIdle || rootDist >= minMovementDistance || motionRefDist >= minMovementDistance)
                {
                    shouldSpawn = true;
                    lastMotionRefPosition = currentMotionRef;
                }
            }
        }

        if (shouldSpawn)
        {
            SpawnGhostSnapshot();
            lastSpawnPosition = transform.position;
        }

        UpdateActiveGhosts();

#if UNITY_EDITOR
        if (!Application.isPlaying && activeGhosts.Count > 0)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
#endif
    }

    /// <summary>
    /// Spawns a snapshot clone of the character, frozen at the current animation frame,
    /// parented under character/Ghost_Clones, offset and glowing golden.
    /// </summary>
    [ContextMenu("Spawn Single Ghost (Test Now)")]
    public void SpawnGhostSnapshot()
    {
        GameObject source = ResolveSourceModel();
        if (source == null) return;

        EnsureContainer();
        LoadGhostMaterial();

        GhostInstance ghost = GetOrCreateGhostInstance(source);
        if (ghost == null || ghost.rootObject == null) return;

        // 1. Synchronize all bone transforms and attached accessories (beard, crown, sword, etc.) to exact current animation frame
        SyncTransforms(ghost);

        // 2. Position the ghost clone
        Vector3 worldOffset = offsetInLocalSpace ? source.transform.TransformDirection(positionOffset) : positionOffset;
        ghost.spawnWorldPos = source.transform.position + worldOffset;
        ghost.spawnWorldRot = source.transform.rotation;

        // Calculate accurate world scale matching character 1:1
        Vector3 desiredWorldScale = Vector3.Scale(source.transform.lossyScale, ghostScaleMultiplier * ghostScale);
        ghost.spawnWorldScale = desiredWorldScale;

        ghost.rootObject.transform.position = ghost.spawnWorldPos;
        ghost.rootObject.transform.rotation = ghost.spawnWorldRot;

        // Convert world scale to local scale relative to container
        Vector3 containerLossy = ghostClonesContainer != null ? ghostClonesContainer.lossyScale : Vector3.one;
        ghost.rootObject.transform.localScale = new Vector3(
            containerLossy.x > 0.0001f ? desiredWorldScale.x / containerLossy.x : desiredWorldScale.x,
            containerLossy.y > 0.0001f ? desiredWorldScale.y / containerLossy.y : desiredWorldScale.y,
            containerLossy.z > 0.0001f ? desiredWorldScale.z / containerLossy.z : desiredWorldScale.z
        );

        // 3. Set initial full visibility state
        ApplyMaterialPropertiesToGhost(ghost, 1.0f, 0.0f);

        ghost.spawnTime = GetCurrentTime();
        ghost.lifetime = Mathf.Max(0.05f, ghostDuration);
        ghost.isActive = true;
        ghost.rootObject.SetActive(true);

        activeGhosts.Add(ghost);

        // Cap concurrent ghosts
        while (activeGhosts.Count > maxConcurrentGhosts)
        {
            RecycleGhost(activeGhosts[0]);
            activeGhosts.RemoveAt(0);
        }
    }

    /// <summary>
    /// Copies local position, rotation, and scale for every mapped transform
    /// from the active source character to the duplicate clone.
    /// This keeps Crown (Object_217), Beard (Beard_Beard_0), Sword, and all bones in 100% sync.
    /// </summary>
    private void SyncTransforms(GhostInstance ghost)
    {
        int count = ghost.transformPairs.Count;
        for (int i = 0; i < count; i++)
        {
            Transform src = ghost.transformPairs[i].source;
            Transform dst = ghost.transformPairs[i].clone;
            if (src != null && dst != null)
            {
                dst.localPosition = src.localPosition;
                dst.localRotation = src.localRotation;
                dst.localScale = src.localScale;
            }
        }
    }

    private void UpdateActiveGhosts()
    {
        if (activeGhosts.Count == 0) return;

        float currentTime = GetCurrentTime();

        for (int i = activeGhosts.Count - 1; i >= 0; i--)
        {
            GhostInstance ghost = activeGhosts[i];
            if (ghost == null || !ghost.isActive || ghost.rootObject == null)
            {
                activeGhosts.RemoveAt(i);
                continue;
            }

            // Keep stationary in world space if leaveTrailInWorldSpace is enabled
            if (leaveTrailInWorldSpace)
            {
                ghost.rootObject.transform.position = ghost.spawnWorldPos;
                ghost.rootObject.transform.rotation = ghost.spawnWorldRot;

                Vector3 containerLossy = ghostClonesContainer != null ? ghostClonesContainer.lossyScale : Vector3.one;
                ghost.rootObject.transform.localScale = new Vector3(
                    containerLossy.x > 0.0001f ? ghost.spawnWorldScale.x / containerLossy.x : ghost.spawnWorldScale.x,
                    containerLossy.y > 0.0001f ? ghost.spawnWorldScale.y / containerLossy.y : ghost.spawnWorldScale.y,
                    containerLossy.z > 0.0001f ? ghost.spawnWorldScale.z / containerLossy.z : ghost.spawnWorldScale.z
                );
            }
            else
            {
                // Follow character with offset
                GameObject source = currentSourceModel;
                if (source != null)
                {
                    Vector3 worldOffset = offsetInLocalSpace ? source.transform.TransformDirection(positionOffset) : positionOffset;
                    ghost.rootObject.transform.position = source.transform.position + worldOffset;
                    ghost.rootObject.transform.rotation = source.transform.rotation;
                }
            }

            float age = currentTime - ghost.spawnTime;
            float normalizedProgress = Mathf.Clamp01(age / ghost.lifetime);

            if (normalizedProgress >= 1.0f)
            {
                RecycleGhost(ghost);
                activeGhosts.RemoveAt(i);
                continue;
            }

            float currentOpacity = opacityCurve.Evaluate(normalizedProgress);
            float currentDissolve = dissolveCurve.Evaluate(normalizedProgress);

            ApplyMaterialPropertiesToGhost(ghost, currentOpacity, currentDissolve);
        }
    }

    private void ApplyMaterialPropertiesToGhost(GhostInstance ghost, float opacity, float dissolve)
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        int rCount = ghost.renderers.Count;
        for (int r = 0; r < rCount; r++)
        {
            Renderer rend = ghost.renderers[r];
            if (rend == null || !rend.gameObject.activeInHierarchy) continue;

            rend.GetPropertyBlock(propertyBlock);

            propertyBlock.SetColor(PropBaseColor, ghostColor);
            propertyBlock.SetColor(PropEmissionColor, emissionColor);
            propertyBlock.SetFloat(PropBloomMult, bloomMultiplier);
            propertyBlock.SetColor(PropBurnColor, dissolveBurnColor);
            propertyBlock.SetFloat(PropBurnWidth, dissolveEdgeWidth);
            propertyBlock.SetFloat(PropNoiseScale, noiseScale);
            propertyBlock.SetFloat(PropVertBias, verticalBias);
            propertyBlock.SetFloat(PropOpacity, opacity);
            propertyBlock.SetFloat(PropDissolve, dissolve);

            rend.SetPropertyBlock(propertyBlock);
        }
    }

    private GhostInstance GetOrCreateGhostInstance(GameObject sourceModel)
    {
        // 1. Try reusing an available pooled ghost
        while (availableGhosts.Count > 0)
        {
            GhostInstance pooled = availableGhosts.Dequeue();
            if (pooled != null && pooled.rootObject != null)
            {
                return pooled;
            }
        }

        // 2. If under limit, create a new clone
        if (activeGhosts.Count < maxConcurrentGhosts)
        {
            return CreateNewGhostClone(sourceModel);
        }

        // 3. Otherwise recycle the oldest active ghost
        if (activeGhosts.Count > 0)
        {
            GhostInstance oldest = activeGhosts[0];
            activeGhosts.RemoveAt(0);
            RecycleGhost(oldest);
            return oldest;
        }

        return CreateNewGhostClone(sourceModel);
    }

    private GhostInstance CreateNewGhostClone(GameObject sourceModel)
    {
        EnsureContainer();

        GhostInstance ghost = new GhostInstance();
        int cloneIndex = activeGhosts.Count + availableGhosts.Count;

        // Direct duplication of the character model under the Ghost_Clones container
        GameObject clone = Instantiate(sourceModel, ghostClonesContainer);
        clone.name = $"Arthur_Ghost_Clone_{cloneIndex}";
        // Prevent preview clones from being serialized into the scene
        clone.hideFlags = HideFlags.DontSave;

        // Strip non-mesh components so clone remains frozen and silent
        ArthurGhostTrailVFX[] allTrailVFX = clone.GetComponentsInChildren<ArthurGhostTrailVFX>(true);
        for (int i = 0; i < allTrailVFX.Length; i++) SafeDestroy(allTrailVFX[i]);

        Animator[] animators = clone.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].enabled = false;
            SafeDestroy(animators[i]);
        }

        Animation[] legacyAnimations = clone.GetComponentsInChildren<Animation>(true);
        for (int i = 0; i < legacyAnimations.Length; i++)
        {
            legacyAnimations[i].enabled = false;
            SafeDestroy(legacyAnimations[i]);
        }

        AudioSource[] audios = clone.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audios.Length; i++) SafeDestroy(audios[i]);

        Collider[] colliders = clone.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++) SafeDestroy(colliders[i]);

        Rigidbody[] rbs = clone.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rbs.Length; i++) SafeDestroy(rbs[i]);

        // Remove any particle systems or visual effects children from character (e.g. character VFX aura)
        ParticleSystem[] particles = clone.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] != null) SafeDestroy(particles[i].gameObject);
        }

        Transform vfxChild = clone.transform.Find("VFX");
        if (vfxChild != null)
        {
            vfxChild.gameObject.SetActive(false);
            SafeDestroy(vfxChild.gameObject);
        }

        // Setup Renderers and assign ghost material
        Renderer[] allRenderers = clone.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer rend = allRenderers[i];
            if (rend == null) continue;

            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            rend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            if (rend is SkinnedMeshRenderer smr)
            {
                smr.updateWhenOffscreen = true;
            }

            int matCount = rend.sharedMaterials != null ? Mathf.Max(1, rend.sharedMaterials.Length) : 1;
            Material[] ghostMats = new Material[matCount];
            for (int m = 0; m < matCount; m++)
            {
                ghostMats[m] = ghostMaterial;
            }
            rend.sharedMaterials = ghostMats;

            ghost.renderers.Add(rend);
        }

        // Map matching transforms by exact relative path so Crown, Beard, Sword, and all bones sync perfectly
        ghost.transformPairs.Clear();
        MapTransformsByPath(sourceModel.transform, clone.transform, ghost.transformPairs);

        ghost.rootObject = clone;
        ghost.isActive = false;
        clone.SetActive(false);

        return ghost;
    }

    /// <summary>
    /// Maps every transform in the source model hierarchy to its matching counterpart in the clone
    /// using unambiguous relative hierarchy paths (e.g. "Beard_Beard_0", "Object_217", "Armature/Root/...").
    /// </summary>
    private static void MapTransformsByPath(Transform sourceRoot, Transform cloneRoot, List<TransformPair> pairs)
    {
        Transform[] allSourceTransforms = sourceRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allSourceTransforms.Length; i++)
        {
            Transform src = allSourceTransforms[i];
            if (src == sourceRoot) continue;

            string relPath = GetRelativeHierarchyPath(sourceRoot, src);
            if (string.IsNullOrEmpty(relPath)) continue;

            Transform dst = cloneRoot.Find(relPath);
            if (dst != null)
            {
                pairs.Add(new TransformPair
                {
                    source = src,
                    clone = dst
                });
            }
        }
    }

    private static string GetRelativeHierarchyPath(Transform root, Transform target)
    {
        if (target == root || target == null) return string.Empty;

        string path = target.name;
        Transform current = target.parent;
        while (current != null && current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    private void RecycleGhost(GhostInstance ghost)
    {
        if (ghost == null) return;
        ghost.isActive = false;
        if (ghost.rootObject != null)
        {
            ghost.rootObject.SetActive(false);
        }
        availableGhosts.Enqueue(ghost);
    }

    [ContextMenu("Clear Active Ghosts")]
    public void ClearAllGhosts()
    {
        for (int i = 0; i < activeGhosts.Count; i++)
        {
            RecycleGhost(activeGhosts[i]);
        }
        activeGhosts.Clear();
    }

    [ContextMenu("Rebuild Clones Pool")]
    public void RebuildPool()
    {
        DestroyPoolContainer();
        Initialize();
    }

    private void DestroyPoolContainer()
    {
        ClearAllGhosts();
        availableGhosts.Clear();

        if (ghostClonesContainer != null)
        {
            SafeDestroy(ghostClonesContainer.gameObject);
            ghostClonesContainer = null;
        }
    }

    /// <summary>
    /// Cleans up any legacy scene-level pools or stray clone objects.
    /// </summary>
    public static void CleanUpLegacyPool()
    {
        try
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isLoaded)
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    if (roots[i] != null && (roots[i].name.Contains("Arthur_GhostTrail_Pool") || roots[i].name.Contains("Arthur_Ghost_Clone")))
                    {
                        SafeDestroy(roots[i]);
                    }
                }
            }
        }
        catch
        {
            // Ignore if scene not ready
        }
    }

    private static void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }

    private float GetCurrentTime()
    {
#if UNITY_EDITOR
        return Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
#else
        return Time.time;
#endif
    }
}
