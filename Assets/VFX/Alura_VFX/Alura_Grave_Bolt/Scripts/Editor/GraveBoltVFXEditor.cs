using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for GraveBoltVFX:
/// - Start & End Blasts (same blast, same size: big glowing ball + '*' electric asterisk).
/// - Straight arrow beam with length adjustment.
/// - Rotating helix rope ("robe type stuff rotating like a spark").
/// </summary>
[CustomEditor(typeof(GraveBoltVFX))]
public class GraveBoltVFXEditor : Editor
{
    private SerializedProperty castHandTransformProp;
    private SerializedProperty targetTransformProp;
    private SerializedProperty aluraAnimatorProp;

    private SerializedProperty loopProp;
    private SerializedProperty loopIntervalProp;

    private SerializedProperty windupDurationProp;
    private SerializedProperty strikeDurationProp;
    private SerializedProperty impactDurationProp;
    private SerializedProperty recoveryDurationProp;

    private SerializedProperty blastScaleProp;
    private SerializedProperty centerBallBrightnessProp;
    private SerializedProperty centerBallSizeProp;
    private SerializedProperty asteriskSparksScaleProp;

    private SerializedProperty enableGatheringSparklesProp;
    private SerializedProperty gatheringRadiusProp;
    private SerializedProperty gatheringSparklesScaleProp;
    private SerializedProperty gatheringSparklesCountProp;

    private SerializedProperty enableSkullMotesProp;
    private SerializedProperty skullMotesScaleProp;
    private SerializedProperty skullMotesCountProp;
    private SerializedProperty enableSparkleMotesProp;
    private SerializedProperty sparkleMotesScaleProp;
    private SerializedProperty sparkleMotesCountProp;

    private SerializedProperty straightArrowProp;
    private SerializedProperty arrowLengthProp;
    private SerializedProperty useManualLengthProp;
    private SerializedProperty arrowWidthProp;
    private SerializedProperty arrowShaftJitterProp;

    private SerializedProperty enableSnakeRopesProp;
    private SerializedProperty snakeAmplitudeProp;
    private SerializedProperty snakeWavesProp;
    private SerializedProperty snakeSpeedProp;
    private SerializedProperty snakeWidthProp;
    private SerializedProperty snakeElectricJitterProp;

    private SerializedProperty bloomMultiplierProp;
    private SerializedProperty neonCoreColorProp;
    private SerializedProperty electricGreenProp;
    private SerializedProperty toxicJadeProp;

    private SerializedProperty asteriskBlastMaterialProp;
    private SerializedProperty glowBallMaterialProp;
    private SerializedProperty helixRopeMaterialProp;
    private SerializedProperty lightningMaterialProp;
    private SerializedProperty moteMaterialProp;
    private SerializedProperty skullMaterialProp;

    private bool showAnchors = true;
    private bool showBlastTuner = true;
    private bool showArrowControls = true;
    private bool showHelixRopeControls = true;
    private bool showTiming = false;
    private bool showColors = true;
    private bool showMaterials = false;

    private void OnEnable()
    {
        castHandTransformProp = serializedObject.FindProperty("castHandTransform");
        targetTransformProp = serializedObject.FindProperty("targetTransform");
        aluraAnimatorProp = serializedObject.FindProperty("aluraAnimator");

        loopProp = serializedObject.FindProperty("loop");
        loopIntervalProp = serializedObject.FindProperty("loopInterval");

        windupDurationProp = serializedObject.FindProperty("windupDuration");
        strikeDurationProp = serializedObject.FindProperty("strikeDuration");
        impactDurationProp = serializedObject.FindProperty("impactDuration");
        recoveryDurationProp = serializedObject.FindProperty("recoveryDuration");

        blastScaleProp = serializedObject.FindProperty("blastScale");
        centerBallBrightnessProp = serializedObject.FindProperty("centerBallBrightness");
        centerBallSizeProp = serializedObject.FindProperty("centerBallSize");
        asteriskSparksScaleProp = serializedObject.FindProperty("asteriskSparksScale");

        enableGatheringSparklesProp = serializedObject.FindProperty("enableGatheringSparkles");
        gatheringRadiusProp = serializedObject.FindProperty("gatheringRadius");
        gatheringSparklesScaleProp = serializedObject.FindProperty("gatheringSparklesScale");
        gatheringSparklesCountProp = serializedObject.FindProperty("gatheringSparklesCount");

        enableSkullMotesProp = serializedObject.FindProperty("enableSkullMotes");
        skullMotesScaleProp = serializedObject.FindProperty("skullMotesScale");
        skullMotesCountProp = serializedObject.FindProperty("skullMotesCount");
        enableSparkleMotesProp = serializedObject.FindProperty("enableSparkleMotes");
        sparkleMotesScaleProp = serializedObject.FindProperty("sparkleMotesScale");
        sparkleMotesCountProp = serializedObject.FindProperty("sparkleMotesCount");

        straightArrowProp = serializedObject.FindProperty("straightArrow");
        arrowLengthProp = serializedObject.FindProperty("arrowLength");
        useManualLengthProp = serializedObject.FindProperty("useManualLength");
        arrowWidthProp = serializedObject.FindProperty("arrowWidth");
        arrowShaftJitterProp = serializedObject.FindProperty("arrowShaftJitter");

        enableSnakeRopesProp = serializedObject.FindProperty("enableSnakeRopes");
        snakeAmplitudeProp = serializedObject.FindProperty("snakeAmplitude");
        snakeWavesProp = serializedObject.FindProperty("snakeWaves");
        snakeSpeedProp = serializedObject.FindProperty("snakeSpeed");
        snakeWidthProp = serializedObject.FindProperty("snakeWidth");
        snakeElectricJitterProp = serializedObject.FindProperty("snakeElectricJitter");

        bloomMultiplierProp = serializedObject.FindProperty("bloomMultiplier");
        neonCoreColorProp = serializedObject.FindProperty("neonCoreColor");
        electricGreenProp = serializedObject.FindProperty("electricGreen");
        toxicJadeProp = serializedObject.FindProperty("toxicJade");

        asteriskBlastMaterialProp = serializedObject.FindProperty("asteriskBlastMaterial");
        glowBallMaterialProp = serializedObject.FindProperty("glowBallMaterial");
        helixRopeMaterialProp = serializedObject.FindProperty("helixRopeMaterial");
        lightningMaterialProp = serializedObject.FindProperty("lightningMaterial");
        moteMaterialProp = serializedObject.FindProperty("moteMaterial");
        skullMaterialProp = serializedObject.FindProperty("skullMaterial");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        GraveBoltVFX vfx = (GraveBoltVFX)target;

        vfx.CleanupLegacyAndOffendingObjects();
        if (vfx.LightningBolt != null)
        {
            vfx.LightningBolt.CleanupStrayChildren();
        }

        // ==========================================
        //  HEADER BANNER
        // ==========================================
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("⚡ Alura Graves - 1. GRAVE BOLT VFX", titleStyle);
        EditorGUILayout.LabelField("Straight Arrow • Rotating Spark Rope • Same Start & End Blasts", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.Space(2);
        string stageStatus = vfx.IsPlayingSequence ? $"▶ ACTIVE: {vfx.CurrentStageName}" : "● STATUS: Ready / Idle";
        GUIStyle statusStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = vfx.IsPlayingSequence ? new Color(0.2f, 1f, 0.4f) : new Color(0.7f, 0.7f, 0.7f) }
        };
        EditorGUILayout.LabelField(stageStatus, statusStyle);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);

        // ==========================================
        //  PRIMARY TRANSPORT CONTROLS
        // ==========================================
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎮 1-Click Ability Testing", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.25f, 0.95f, 0.45f);
        if (GUILayout.Button("⚡ Cast Grave Bolt Ability", GUILayout.Height(36)))
        {
            vfx.CastGraveBolt();
        }

        GUI.backgroundColor = new Color(0.95f, 0.4f, 0.35f);
        if (GUILayout.Button("↺ Reset All", GUILayout.Height(36), GUILayout.Width(90)))
        {
            vfx.ResetAll();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        // Continuous Loop Toggle
        bool isLooping = (Application.isPlaying && vfx.IsPlayingSequence) || (!Application.isPlaying && vfx.EditorLooping);
        GUI.backgroundColor = isLooping ? new Color(1f, 0.45f, 0.35f) : new Color(0.2f, 0.82f, 1f);
        string loopBtnText = isLooping ? "⏹ Stop Continuous Looping" : "🔄 Play Continuous Loop (Realtime Preview)";
        if (GUILayout.Button(loopBtnText, GUILayout.Height(32)))
        {
            if (Application.isPlaying)
            {
                if (isLooping) vfx.StopLoop();
                else vfx.StartLoop();
            }
            else
            {
                vfx.ToggleEditorLoop();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(3);

        // Live Bolt & Rope Preview Toggle
        bool isPreviewing = vfx.LightningBolt != null && vfx.LightningBolt.LiveScenePreview;
        GUI.backgroundColor = isPreviewing ? new Color(1f, 0.6f, 0.2f) : new Color(0.3f, 0.85f, 1f);
        string previewText = isPreviewing ? "⏸ Stop Live Scene Preview" : "⚡ Keep Arrow & Rotating Rope Visible (Live Scene Preview)";
        if (GUILayout.Button(previewText, GUILayout.Height(30)))
        {
            vfx.EnsureInitialized();
            if (vfx.LightningBolt != null)
            {
                vfx.LightningBolt.LiveScenePreview = !vfx.LightningBolt.LiveScenePreview;
                if (vfx.LightningBolt.LiveScenePreview)
                {
                    vfx.LightningBolt.GenerateJaggedBolt(vfx.GetHandPosition(), vfx.GetTargetPosition());
                }
                SceneView.RepaintAll();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);

        // ==========================================
        //  💥 START & END BLAST TUNER (IDENTICAL BLASTS)
        // ==========================================
        showBlastTuner = EditorGUILayout.BeginFoldoutHeaderGroup(showBlastTuner, "💥 Start & End Point Blasts (Gathering Sparkles, Asterisk '*', Glow Ball)");
        if (showBlastTuner)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("• Starting Palm: Sparkles spawn randomly spread across radius and size, then gather inward to palm center to form the blast (NO skulls on start!).\n• Blasts: Big glowing ball + Asterisk '*' electric rays + needle sparks + twinkling motes.\n• Target Impact: Identical blast born directly at arrow tip + floating spectral skulls!", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(blastScaleProp, new GUIContent("Master Blast Size", "Uniform scale for BOTH starting palm blast and ending target blast."));
            EditorGUILayout.PropertyField(centerBallBrightnessProp, new GUIContent("Center Ball Glow", "Brightness of the big glowing ball in the center."));
            EditorGUILayout.PropertyField(centerBallSizeProp, new GUIContent("Center Ball Radius", "Relative size of the center glowing ball."));
            EditorGUILayout.PropertyField(asteriskSparksScaleProp, new GUIContent("Asterisk '*' Rays Scale", "Length multiplier for the '*' electric branches radiating outward."));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("✨ Inward Gathering Sparkles (Start Wind-Up Charge)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableGatheringSparklesProp, new GUIContent("Enable Gathering Sparkles", "Sparkles spawn spread in a radius with random sizes and collect inward to the palm before blast."));
            if (enableGatheringSparklesProp.boolValue)
            {
                EditorGUILayout.PropertyField(gatheringRadiusProp, new GUIContent("Gathering Radius (Meters)", "Spawn radius spread around palm where sparkles appear."));
                EditorGUILayout.PropertyField(gatheringSparklesScaleProp, new GUIContent("Sparkles Size Scale", "Size multiplier for gathering sparkles."));
                EditorGUILayout.PropertyField(gatheringSparklesCountProp, new GUIContent("Sparkles Count", "Number / density of sparkles gathering inward."));
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("💀 Floating Spectral Skull Motes (Impact Target Only)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableSkullMotesProp, new GUIContent("Enable Impact Skulls", "Floating spectral skulls drifting around the impact blast (strictly on target, not on start)."));
            if (enableSkullMotesProp.boolValue)
            {
                EditorGUILayout.PropertyField(skullMotesScaleProp, new GUIContent("Skull Size Scale"));
                EditorGUILayout.PropertyField(skullMotesCountProp, new GUIContent("Skull Motes Count"));
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("✨ Small '*' Twinkling Sparkle Motes (Dispersing)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableSparkleMotesProp, new GUIContent("Enable Sparkle Motes", "Small twinkling '*' motes dispersing around blast."));
            if (enableSparkleMotesProp.boolValue)
            {
                EditorGUILayout.PropertyField(sparkleMotesScaleProp, new GUIContent("Sparkle Size Scale"));
                EditorGUILayout.PropertyField(sparkleMotesCountProp, new GUIContent("Sparkle Motes Count"));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                vfx.ConfigureAllParticleSystems();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("✨ Preview Charge (Gathering)", GUILayout.Height(24)))
            {
                vfx.PlayStage1_Windup();
            }
            if (GUILayout.Button("💥 Preview Start Blast", GUILayout.Height(24)))
            {
                vfx.PlayStage2_CastRelease();
            }
            if (GUILayout.Button("🎯 Preview Impact Blast", GUILayout.Height(24)))
            {
                vfx.PlayStage3_Impact();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(4);

        // ==========================================
        //  🏹 ARROW SHAFT & LENGTH ADJUSTMENT
        // ==========================================
        showArrowControls = EditorGUILayout.BeginFoldoutHeaderGroup(showArrowControls, "🏹 Arrow Shaft & Length Adjustment");
        if (showArrowControls)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Arrow projectile pointing straight forward. Note: The impact blast is GUARANTEED to be born directly at the end/tip of the arrow! If you adjust Arrow Length, the blast automatically follows.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(straightArrowProp, new GUIContent("Straight Arrow", "Keep arrow beam straight to the front."));
            EditorGUILayout.PropertyField(useManualLengthProp, new GUIContent("Use Manual Length", "Check to manually override the length slider instead of target distance."));
            if (useManualLengthProp.boolValue)
            {
                EditorGUILayout.PropertyField(arrowLengthProp, new GUIContent("Arrow Length (Meters)", "Adjustable length of the arrow. The impact blast automatically locks to this tip!"));
            }
            EditorGUILayout.PropertyField(arrowWidthProp, new GUIContent("Arrow Shaft Width"));
            EditorGUILayout.PropertyField(arrowShaftJitterProp, new GUIContent("Electric Crackle Jitter", "Subtle electric vibration along the arrow shaft."));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                vfx.UpdateImpactAnchorPosition();
                if (vfx.LightningBolt != null)
                {
                    vfx.LightningBolt.StraightArrow = straightArrowProp.boolValue;
                    vfx.LightningBolt.UseManualLength = useManualLengthProp.boolValue;
                    vfx.LightningBolt.ManualLength = arrowLengthProp.floatValue;
                    vfx.LightningBolt.StartWidth = arrowWidthProp.floatValue;
                    vfx.LightningBolt.JaggedAmplitude = arrowShaftJitterProp.floatValue;
                    vfx.LightningBolt.ApplyLineProperties();
                    if (vfx.LightningBolt.LiveScenePreview)
                    {
                        vfx.LightningBolt.GenerateJaggedBolt(vfx.GetHandPosition(), vfx.GetTargetPosition());
                    }
                }
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(4);

        // ==========================================
        //  🐍 MULTIPLE SNAKE WAVING ROPES ("Vave Like Snack")
        // ==========================================
        showHelixRopeControls = EditorGUILayout.BeginFoldoutHeaderGroup(showHelixRopeControls, "🐍 Multiple Snake Waving Ropes ('Vave Like Snack')");
        if (showHelixRopeControls)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Multiple braided electric ropes undulating with sinusoidal traveling waves around the arrow shaft like swimming snakes.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(enableSnakeRopesProp, new GUIContent("Enable Snake Ropes"));
            EditorGUILayout.PropertyField(snakeAmplitudeProp, new GUIContent("Snake Wave Amplitude (Height)"));
            EditorGUILayout.PropertyField(snakeWavesProp, new GUIContent("Snake Wave Count (Loops)"));
            EditorGUILayout.PropertyField(snakeSpeedProp, new GUIContent("Snake Swimming Speed"));
            EditorGUILayout.PropertyField(snakeWidthProp, new GUIContent("Snake Rope Width"));
            EditorGUILayout.PropertyField(snakeElectricJitterProp, new GUIContent("Voltage Crackle Jitter"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (vfx.LightningBolt != null)
                {
                    vfx.LightningBolt.EnableSnakeRopes = enableSnakeRopesProp.boolValue;
                    vfx.LightningBolt.SnakeAmplitude = snakeAmplitudeProp.floatValue;
                    vfx.LightningBolt.SnakeWaves = snakeWavesProp.floatValue;
                    vfx.LightningBolt.SnakeSpeed = snakeSpeedProp.floatValue;
                    vfx.LightningBolt.SnakeWidth = snakeWidthProp.floatValue;
                    vfx.LightningBolt.SnakeElectricJitter = snakeElectricJitterProp.floatValue;
                    vfx.LightningBolt.ApplyLineProperties();
                    if (vfx.LightningBolt.LiveScenePreview)
                    {
                        vfx.LightningBolt.GenerateJaggedBolt(vfx.GetHandPosition(), vfx.GetTargetPosition());
                    }
                }
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(4);

        // ==========================================
        //  🎯 SCENE ANCHORS
        // ==========================================
        showAnchors = EditorGUILayout.BeginFoldoutHeaderGroup(showAnchors, "🎯 Scene Anchors & Character Bones");
        if (showAnchors)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(castHandTransformProp, new GUIContent("Cast Hand (Left Palm)"));
            EditorGUILayout.PropertyField(targetTransformProp, new GUIContent("Target Transform"));
            EditorGUILayout.PropertyField(aluraAnimatorProp, new GUIContent("Alura Animator"));
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(4);

        // ==========================================
        //  🎨 HDR BLOOM & COLOR PALETTE
        // ==========================================
        showColors = EditorGUILayout.BeginFoldoutHeaderGroup(showColors, "🎨 HDR Bloom & Color Palette");
        if (showColors)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(bloomMultiplierProp, new GUIContent("HDR Bloom Multiplier (3.5x - 6x)"));
            EditorGUILayout.PropertyField(neonCoreColorProp, new GUIContent("Neon Core (#FFFFFF)"));
            EditorGUILayout.PropertyField(electricGreenProp, new GUIContent("Electric Green (#40FF73)"));
            EditorGUILayout.PropertyField(toxicJadeProp, new GUIContent("Toxic Jade Rim (#0FA854)"));

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("1-Click Preset Themes:", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.35f, 1.0f, 0.5f);
            if (GUILayout.Button("💀 Necrotic Jade (Storyboard)", GUILayout.Height(26)))
            {
                Undo.RecordObject(vfx, "Apply Necrotic Jade Theme");
                neonCoreColorProp.colorValue = new Color(0.95f, 1.0f, 1.0f, 1.0f);
                electricGreenProp.colorValue = new Color(0.25f, 1.0f, 0.45f, 1.0f);
                toxicJadeProp.colorValue = new Color(0.08f, 0.65f, 0.35f, 1.0f);
                bloomMultiplierProp.floatValue = 3.8f;
                serializedObject.ApplyModifiedProperties();
                vfx.ConfigureAllParticleSystems();
                if (vfx.LightningBolt != null)
                {
                    vfx.LightningBolt.CoreColor = neonCoreColorProp.colorValue;
                    vfx.LightningBolt.GlowColor = electricGreenProp.colorValue;
                    vfx.LightningBolt.BloomIntensityMultiplier = bloomMultiplierProp.floatValue;
                }
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = new Color(0.2f, 0.85f, 1.0f);
            if (GUILayout.Button("⚡ Electric Cyan", GUILayout.Height(26)))
            {
                Undo.RecordObject(vfx, "Apply Electric Cyan Theme");
                neonCoreColorProp.colorValue = new Color(0.95f, 1.0f, 1.0f, 1.0f);
                electricGreenProp.colorValue = new Color(0.0f, 0.90f, 1.0f, 1.0f);
                toxicJadeProp.colorValue = new Color(0.05f, 0.45f, 1.0f, 1.0f);
                bloomMultiplierProp.floatValue = 3.8f;
                serializedObject.ApplyModifiedProperties();
                vfx.ConfigureAllParticleSystems();
                if (vfx.LightningBolt != null)
                {
                    vfx.LightningBolt.CoreColor = neonCoreColorProp.colorValue;
                    vfx.LightningBolt.GlowColor = electricGreenProp.colorValue;
                    vfx.LightningBolt.BloomIntensityMultiplier = bloomMultiplierProp.floatValue;
                }
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(4);

        // ==========================================
        //  ⏱️ SEQUENCE TIMING & LOOPING
        // ==========================================
        showTiming = EditorGUILayout.BeginFoldoutHeaderGroup(showTiming, "⏱️ Sequence Timing & Looping");
        if (showTiming)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(loopProp, new GUIContent("Auto-Loop In Play Mode"));
            EditorGUILayout.PropertyField(loopIntervalProp, new GUIContent("Pause Between Loops (Sec)"));
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(windupDurationProp, new GUIContent("Stage 1: Wind-Up Charge"));
            EditorGUILayout.PropertyField(strikeDurationProp, new GUIContent("Stage 2: Arrow Travel"));
            EditorGUILayout.PropertyField(impactDurationProp, new GUIContent("Stage 3: Impact Blast"));
            EditorGUILayout.PropertyField(recoveryDurationProp, new GUIContent("Stage 4: Recovery Fade"));
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(4);

        // ==========================================
        //  📦 MATERIALS
        // ==========================================
        showMaterials = EditorGUILayout.BeginFoldoutHeaderGroup(showMaterials, "📦 URP Particle Materials");
        if (showMaterials)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(asteriskBlastMaterialProp, new GUIContent("Asterisk '*' Blast"));
            EditorGUILayout.PropertyField(glowBallMaterialProp, new GUIContent("Center Glowing Ball"));
            EditorGUILayout.PropertyField(helixRopeMaterialProp, new GUIContent("Helix Rope Ribbon"));
            EditorGUILayout.PropertyField(lightningMaterialProp, new GUIContent("Arrow Shaft"));
            EditorGUILayout.PropertyField(moteMaterialProp, new GUIContent("Needle Sparks"));
            EditorGUILayout.PropertyField(skullMaterialProp, new GUIContent("Spectral Skulls"));
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }
}
