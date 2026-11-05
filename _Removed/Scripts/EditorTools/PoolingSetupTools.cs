using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Endless Faller > 2. Create Special Obstacle + End Zone Prefabs
// Creates the SO_SquareRing and EZ_Pillar template prefabs under Prefabs/Interacbles/Hazards,
// and adds them to ObjectPoolManager's lists.
public static class PoolingSetupTools
{
    // ------------------------------------------------------------ 2. Templates

    private const string HazardFolder = "Assets/Endless Faller/Prefabs/Interacbles/Hazards";
    private const string SpecialFolder = HazardFolder + "/SpecialObstacles";
    private const string EndZoneFolder = HazardFolder + "/EndZoneAssets";
    private const string HazardMaterialGuid = "293e5d28d1604d239a490293ac84f51c"; // M_ComicBase

    // Same as BaseObstacle: root on layer 3 (what RandomSpawner's Blocking Mask checks), tag "Obstacle".
    private const int ObstacleLayer = 3;
    private const string ObstacleTag = "Obstacle";

    [MenuItem("Endless Faller/2. Create Special Obstacle + End Zone Prefabs")]
    private static void CreateTemplates()
    {
        EnsureFolder(HazardFolder, "SpecialObstacles");
        EnsureFolder(HazardFolder, "EndZoneAssets");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(HazardMaterialGuid));

        GameObject special = SaveIfMissing(SpecialFolder + "/SO_SquareRing.prefab", () => BuildSquareRing(material));
        GameObject endZone = SaveIfMissing(EndZoneFolder + "/EZ_Pillar.prefab", () => BuildPillar(material));

        ObjectPoolManager manager = Object.FindFirstObjectByType<ObjectPoolManager>();
        if (manager == null) { Debug.LogError("Prefabs created, but there is no ObjectPoolManager in the open scene to add them to."); return; }

        var managerData = new SerializedObject(manager);
        AppendUnique(managerData.FindProperty("specialObstacles"), special);
        AppendUnique(managerData.FindProperty("endZoneAssets"), endZone);
        managerData.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Selection.activeGameObject = manager.gameObject;
        Debug.Log($"Created {SpecialFolder}/SO_SquareRing and {EndZoneFolder}/EZ_Pillar and added them to ObjectPoolManager. Save the scene.");
    }

    // A flat square frame lying in the fall direction, so the player can drop
    // through the hole. One reward slot in the middle of the hole.
    private static GameObject BuildSquareRing(Material material)
    {
        GameObject root = NewObstacleRoot("SO_SquareRing");

        // Kinematic Rigidbody: child trigger colliders report to the root's Obstacle script.
        Rigidbody body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        const float outer = 240f, bar = 50f, thickness = 40f;
        float offset = (outer - bar) * 0.5f;
        AddBlock(root, "Bar North", new Vector3(0f, 0f, offset), new Vector3(outer, thickness, bar), material);
        AddBlock(root, "Bar South", new Vector3(0f, 0f, -offset), new Vector3(outer, thickness, bar), material);
        AddBlock(root, "Bar East", new Vector3(offset, 0f, 0f), new Vector3(bar, thickness, outer - bar * 2f), material);
        AddBlock(root, "Bar West", new Vector3(-offset, 0f, 0f), new Vector3(bar, thickness, outer - bar * 2f), material);

        Transform slot = new GameObject("RewardSlot").transform;
        slot.SetParent(root.transform, false);

        SpecialObstacle special = root.AddComponent<SpecialObstacle>();
        var data = new SerializedObject(special);
        SetMoveUp(data);
        data.FindProperty("shakeDistance").floatValue = 500f;
        SerializedProperty slots = data.FindProperty("rewardSlots");
        slots.arraySize = 1;
        slots.GetArrayElementAtIndex(0).objectReferenceValue = slot;
        data.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    // A plain tall block: a cheap stand-in until the real low-detail models are made.
    private static GameObject BuildPillar(Material material)
    {
        GameObject root = NewObstacleRoot("EZ_Pillar");

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        GameObject block = AddBlock(root, "Mesh", Vector3.zero, new Vector3(80f, 300f, 80f), material);
        // Scenery: no shadows needed.
        block.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        EndZoneAsset asset = root.AddComponent<EndZoneAsset>();
        var data = new SerializedObject(asset);
        SetMoveUp(data);
        data.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject NewObstacleRoot(string name)
    {
        var root = new GameObject(name) { layer = ObstacleLayer };
        root.tag = ObstacleTag;
        return root;
    }

    private static GameObject AddBlock(GameObject root, string name, Vector3 position, Vector3 size, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.layer = ObstacleLayer;
        block.transform.SetParent(root.transform, false);
        block.transform.localPosition = position;
        block.transform.localScale = size;
        block.GetComponent<BoxCollider>().isTrigger = true;
        if (material != null) block.GetComponent<MeshRenderer>().sharedMaterial = material;
        return block;
    }

    // Same movement as BaseObstacle: up (Y) at 100-150 units per second.
    private static void SetMoveUp(SerializedObject data)
    {
        data.FindProperty("moveInLine").boolValue = true;
        data.FindProperty("moveInLineSettings.axis").enumValueIndex = (int)MoveAxis.Y;
        data.FindProperty("moveInLineSettings.speedMin").floatValue = 100f;
        data.FindProperty("moveInLineSettings.speedMax").floatValue = 150f;
    }

    // Never overwrites: once you edit a prefab, running the tool again keeps your version.
    private static GameObject SaveIfMissing(string path, System.Func<GameObject> build)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject temp = build();
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return saved;
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static void AppendUnique(SerializedProperty array, Object value)
    {
        if (array == null || value == null) return;

        for (int i = 0; i < array.arraySize; i++)
            if (array.GetArrayElementAtIndex(i).objectReferenceValue == value) return;

        array.arraySize++;
        array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = value;
    }

}
