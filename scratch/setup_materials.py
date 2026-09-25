import os

tex_dir = r"d:\Projects\Waheed\Attack VFX\VFX01\Assets\VFX\Grave_Bolt\Textures"
mat_dir = r"d:\Projects\Waheed\Attack VFX\VFX01\Assets\VFX\Grave_Bolt\Materials"

def create_texture_meta(filename, guid):
    path = os.path.join(tex_dir, filename + ".meta")
    content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  doesTextureContainColorSpaceAttribute: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 1
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbook: 0
  ignorePngGamma: 0
  cookieLightType: 0
  platformSettings: []
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Created meta: {path}")

def create_mat_and_meta(mat_name, mat_guid, tex_guid):
    mat_path = os.path.join(mat_dir, mat_name + ".mat")
    mat_content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &-984532182687101758
MonoBehaviour:
  m_ObjectHideFlags: 11
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: d0353a89b1f911e48b9e16bdc9f2e058, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  version: 10
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {mat_name}
  m_Shader: {{fileID: 4800000, guid: 0406db5a14f94604a8c57ccfbc9f3b46, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords:
  - _SURFACE_TYPE_TRANSPARENT
  m_InvalidKeywords:
  - _FLIPBOOKBLENDING_OFF
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 1
  m_CustomRenderQueue: 3000
  stringTagMap:
    RenderType: Transparent
  disabledShaderPasses:
  - DepthOnly
  - SHADOWCASTER
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _BumpMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _EmissionMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {tex_guid}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _AlphaClip: 0
    - _AlphaToMask: 0
    - _Blend: 1
    - _BlendOp: 0
    - _CameraFadingEnabled: 0
    - _CameraFarFadeDistance: 2
    - _CameraNearFadeDistance: 1
    - _ColorMode: 0
    - _Cull: 0
    - _Cutoff: 0.5
    - _DistortionBlend: 0.5
    - _DistortionEnabled: 0
    - _DistortionStrength: 1
    - _DistortionStrengthScaled: 0.1
    - _DstBlend: 10
    - _DstBlendAlpha: 10
    - _FlipbookBlending: 0
    - _FlipbookMode: 0
    - _Mode: 0
    - _QueueOffset: 0
    - _Smoothness: 0.5
    - _SoftParticlesEnabled: 0
    - _SoftParticlesFarFadeDistance: 1
    - _SoftParticlesNearFadeDistance: 0
    - _SrcBlend: 1
    - _SrcBlendAlpha: 1
    - _Surface: 1
    - _ZWrite: 0
    m_Colors:
    - _BaseColor: {{r: 1, g: 1, b: 1, a: 1}}
    - _BaseColorAddSubDiff: {{r: 0, g: 0, b: 0, a: 0}}
    - _CameraFadeParams: {{r: 0, g: Infinity, b: 0, a: 0}}
    - _Color: {{r: 1, g: 1, b: 1, a: 1}}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 0}}
    - _SoftParticleFadeParams: {{r: 0, g: 0, b: 0, a: 0}}
  m_BuildTextureStacks: []
  m_AllowLocking: 1
"""
    with open(mat_path, "w", encoding="utf-8") as f:
        f.write(mat_content)
    print(f"Created mat: {mat_path}")

    meta_path = mat_path + ".meta"
    meta_content = f"""fileFormatVersion: 2
guid: {mat_guid}
NativeFormatImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(meta_content)
    print(f"Created mat meta: {meta_path}")

# Textures
create_texture_meta("Electric_Jagged_Arcs.png", "a1b2c3d4e5f60718293a4b5c6d7e8f90")
create_texture_meta("Ground_Lightning_Crawlers.png", "b2c3d4e5f60718293a4b5c6d7e8f90a1")
create_texture_meta("Stylized_Electric_Burst.png", "c3d4e5f60718293a4b5c6d7e8f90a1b2")
create_texture_meta("Flame_Tail_Ribbon.png", "d4e5f60718293a4b5c6d7e8f90a1b2c3")

# Materials
create_mat_and_meta("GraveBolt_JaggedArcs_Mat", "e5f60718293a4b5c6d7e8f90a1b2c3d4", "a1b2c3d4e5f60718293a4b5c6d7e8f90")
create_mat_and_meta("GraveBolt_GroundCrawlers_Mat", "f60718293a4b5c6d7e8f90a1b2c3d4e5", "b2c3d4e5f60718293a4b5c6d7e8f90a1")
create_mat_and_meta("GraveBolt_StylizedBurst_Mat", "0718293a4b5c6d7e8f90a1b2c3d4e5f6", "c3d4e5f60718293a4b5c6d7e8f90a1b2")
create_mat_and_meta("GraveBolt_FlameTail_Mat", "18293a4b5c6d7e8f90a1b2c3d4e5f607", "d4e5f60718293a4b5c6d7e8f90a1b2c3")

print("All meta and material files successfully created!")
