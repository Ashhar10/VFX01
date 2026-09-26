import os

TEX_DIR = "Assets/VFX/King Arthur_VFX/Textures"
MAT_DIR = "Assets/VFX/King Arthur_VFX/Materials"

textures = {
    "Radiant_Starburst_Spark.png": "b5c2d3e4f5a6172839405b6c7d8e901a",
    "Enemy_Skull_Ring.png": "d100d750a9444ee0a1f267c8e9b01001",
    "Ally_Shield_Ring.png": "d100d750a9444ee0a1f267c8e9b01002",
    "Ally_Body_Shield.png": "d100d750a9444ee0a1f267c8e9b01003",
    "Golden_Spark_Mote.png": "d100d750a9444ee0a1f267c8e9b01004"
}

materials = [
    {
        "name": "KingArthur_Contact_Starburst_Mat",
        "guid": "d200d750a9444ee0a1f267c8e9b01001",
        "tex_guid": "b5c2d3e4f5a6172839405b6c7d8e901a",
        "color": "{r: 1, g: 1, b: 1, a: 1}",
        "additive": True
    },
    {
        "name": "KingArthur_Contact_Sparks_Mat",
        "guid": "d200d750a9444ee0a1f267c8e9b01002",
        "tex_guid": "d100d750a9444ee0a1f267c8e9b01004",
        "color": "{r: 1, g: 0.85, b: 0.4, a: 1}",
        "additive": True
    },
    {
        "name": "KingArthur_Enemy_SkullRing_Mat",
        "guid": "d200d750a9444ee0a1f267c8e9b01003",
        "tex_guid": "d100d750a9444ee0a1f267c8e9b01001",
        "color": "{r: 1, g: 0.25, b: 0.15, a: 1}",
        "additive": True
    },
    {
        "name": "KingArthur_Ally_ShieldRing_Mat",
        "guid": "d200d750a9444ee0a1f267c8e9b01004",
        "tex_guid": "d100d750a9444ee0a1f267c8e9b01002",
        "color": "{r: 0.25, g: 0.8, b: 1, a: 1}",
        "additive": True
    },
    {
        "name": "KingArthur_Ally_BodyShield_Mat",
        "guid": "d200d750a9444ee0a1f267c8e9b01005",
        "tex_guid": "d100d750a9444ee0a1f267c8e9b01003",
        "color": "{r: 0.35, g: 0.85, b: 1, a: 1}",
        "additive": True
    }
]

def create_tex_metas():
    for tex_name, guid in textures.items():
        meta_path = os.path.join(TEX_DIR, tex_name + ".meta")
        if not os.path.exists(meta_path):
            with open(meta_path, "w", encoding="utf-8") as f:
                f.write("fileFormatVersion: 2\n")
                f.write("guid: " + guid + "\n")
                f.write("TextureImporter:\n")
                f.write("  internalIDToNameTable: []\n")
                f.write("  externalObjects: {}\n")
                f.write("  serializedVersion: 13\n")
                f.write("  sRGBTexture: 1\n")
                f.write("  alphaIsTransparency: 1\n")
                f.write("  textureType: 0\n")
            print("Created " + meta_path)

def create_materials():
    for mat in materials:
        name = mat["name"]
        guid = mat["guid"]
        tex_guid = mat["tex_guid"]
        col = mat["color"]
        dst_blend = "1" if mat["additive"] else "10"
        src_blend = "5" if mat["additive"] else "5"

        mat_path = os.path.join(MAT_DIR, name + ".mat")
        with open(mat_path, "w", encoding="utf-8") as f:
            f.write("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n")
            f.write("--- !u!114 &-984532182687101758\n")
            f.write("MonoBehaviour:\n  m_ObjectHideFlags: 11\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: d0353a89b1f911e48b9e16bdc9f2e058, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n  version: 10\n")
            f.write("--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n")
            f.write("  m_Name: " + name + "\n")
            f.write("  m_Shader: {fileID: 4800000, guid: 0406db5a14f94604a8c57ccfbc9f3b46, type: 3}\n")
            f.write("  m_Parent: {fileID: 0}\n  m_ModifiedSerializedProperties: 0\n  m_ValidKeywords:\n  - _SURFACE_TYPE_TRANSPARENT\n  m_InvalidKeywords:\n  - _FLIPBOOKBLENDING_OFF\n  m_LightmapFlags: 4\n  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 1\n  m_CustomRenderQueue: 3000\n  stringTagMap:\n    RenderType: Transparent\n  disabledShaderPasses:\n  - DepthOnly\n  - SHADOWCASTER\n  m_LockedProperties: \n")
            f.write("  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs:\n")
            f.write("    - _BaseMap:\n        m_Texture: {fileID: 2800000, guid: " + tex_guid + ", type: 3}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}\n")
            f.write("    - _MainTex:\n        m_Texture: {fileID: 2800000, guid: " + tex_guid + ", type: 3}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}\n")
            f.write("    m_Ints: []\n    m_Floats:\n    - _Blend: 0\n    - _DstBlend: " + dst_blend + "\n    - _DstBlendAlpha: " + dst_blend + "\n    - _SrcBlend: " + src_blend + "\n    - _SrcBlendAlpha: " + src_blend + "\n    - _Surface: 1\n    - _ZWrite: 0\n")
            f.write("    m_Colors:\n    - _BaseColor: " + col + "\n    - _Color: " + col + "\n")
            f.write("  m_BuildTextureStacks: []\n  m_AllowLocking: 1\n")
        print("Created " + mat_path)

        meta_path = os.path.join(MAT_DIR, name + ".mat.meta")
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write("fileFormatVersion: 2\nguid: " + guid + "\nNativeFormatImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
        print("Created " + meta_path)

if __name__ == "__main__":
    create_tex_metas()
    create_materials()
