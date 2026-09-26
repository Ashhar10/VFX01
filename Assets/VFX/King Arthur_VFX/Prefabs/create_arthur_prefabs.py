import os

PREFAB_DIR = "Assets/VFX/King Arthur_VFX/Prefabs"

prefabs = [
    {
        "name": "King_Arthur_Enemy_Contact_VFX",
        "guid": "e100d750a9444ee0a1f267c8e9b01001",
        "script_guid": "902715cb002c4894a8efb7188828b87e"
    },
    {
        "name": "King_Arthur_Enemy_Mark_VFX",
        "guid": "e100d750a9444ee0a1f267c8e9b01002",
        "script_guid": "4cbfab4c64c943e59b6ebaf4f6fc9075"
    },
    {
        "name": "King_Arthur_Ally_Shield_VFX",
        "guid": "e100d750a9444ee0a1f267c8e9b01003",
        "script_guid": "15d3d259f40b4f56a7cee2176667f30c"
    }
]

for p in prefabs:
    name = p["name"]
    guid = p["guid"]
    script_guid = p["script_guid"]

    prefab_path = os.path.join(PREFAB_DIR, name + ".prefab")
    with open(prefab_path, "w", encoding="utf-8") as f:
        f.write("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n")
        f.write("--- !u!1 &1000000000000000\n")
        f.write("GameObject:\n")
        f.write("  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n")
        f.write("  serializedVersion: 6\n")
        f.write("  m_Component:\n")
        f.write("  - component: {fileID: 1000000000000001}\n")
        f.write("  - component: {fileID: 1000000000000002}\n")
        f.write("  m_Layer: 0\n")
        f.write("  m_Name: " + name + "\n")
        f.write("  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n")
        f.write("--- !u!4 &1000000000000001\n")
        f.write("Transform:\n")
        f.write("  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n")
        f.write("  m_GameObject: {fileID: 1000000000000000}\n")
        f.write("  serializedVersion: 2\n")
        f.write("  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n")
        f.write("  m_LocalPosition: {x: 0, y: 0, z: 0}\n")
        f.write("  m_LocalScale: {x: 1, y: 1, z: 1}\n")
        f.write("  m_ConstrainProportionsScale: 0\n")
        f.write("  m_Children: []\n")
        f.write("  m_Father: {fileID: 0}\n")
        f.write("  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n")
        f.write("--- !u!114 &1000000000000002\n")
        f.write("MonoBehaviour:\n")
        f.write("  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n")
        f.write("  m_GameObject: {fileID: 1000000000000000}\n")
        f.write("  m_Enabled: 1\n  m_EditorHideFlags: 0\n")
        f.write("  m_Script: {fileID: 11500000, guid: " + script_guid + ", type: 3}\n")
        f.write("  m_Name: \n  m_EditorClassIdentifier: \n")
    print("Created " + prefab_path)

    meta_path = os.path.join(PREFAB_DIR, name + ".prefab.meta")
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write("fileFormatVersion: 2\n")
        f.write("guid: " + guid + "\n")
        f.write("PrefabImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    print("Created " + meta_path)
