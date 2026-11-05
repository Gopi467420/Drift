using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Menu: Endless Faller > 3. Build Dream Sky
// Generates the sky and cloud textures, materials, the reward cloud prefab and the
// "Dream Sky" scene object, then sets the scene skybox. Safe to run again: it keeps
// anything that already exists. Delete a texture or material to get a fresh one.
public static class DreamSkySetup
{
    private const string Root = "Assets/Endless Faller/Assets/Dream";
    private const string CloudAtlasPath = Root + "/T_DreamClouds.png";
    private const string RewardPrefabPath = "Assets/Endless Faller/Prefabs/Interacbles/Hazards/EndZoneAssets/ZC_RewardCloud.prefab";
    private const string ObjectName = "Dream Sky";

    [MenuItem("Endless Faller/3. Build Dream Sky")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(Root))
            AssetDatabase.CreateFolder("Assets/Endless Faller/Assets", "Dream");

        Texture2D cloudAtlas = LoadOrBake(CloudAtlasPath, BakeCloudAtlas, TextureWrapMode.Clamp);

        Material sky = LoadOrCreateMaterial("M_DreamSky", "Custom/DreamSky", null);
        Material farCloud = LoadOrCreateMaterial("M_DreamFarCloud", "Custom/DreamCloud", cloudAtlas);
        Material sparkle = LoadOrCreateMaterial("M_DreamSparkle", "Custom/DreamSparkle", null);

        bool isNewRewardMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/M_RewardCloud.mat") == null;
        Material rewardCloud = LoadOrCreateMaterial("M_RewardCloud", "Custom/DreamCloud", cloudAtlas);
        if (isNewRewardMaterial)
        {
            rewardCloud.SetFloat("_Billboard", 1f);
            rewardCloud.SetFloat("_Reward", 0.85f);
            rewardCloud.SetFloat("_Opacity", 0.72f); // obstacles behind a cloud must stay visible
            rewardCloud.EnableKeyword("_DREAM_BILLBOARD");
            EditorUtility.SetDirty(rewardCloud);
        }

        GameObject rewardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RewardPrefabPath);
        if (rewardPrefab == null) rewardPrefab = BuildRewardCloud(rewardCloud);

        AssetDatabase.SaveAssets();

        RenderSettings.skybox = sky;

        GameObject existing = GameObject.Find(ObjectName);
        if (existing == null)
            BuildSceneObject(farCloud, sparkle);
        else
            RepairSparkles(existing.transform);

        ObjectPoolManager manager = Object.FindFirstObjectByType<ObjectPoolManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogError("Dream Sky: no ObjectPoolManager in the open scene, so the reward cloud was not added.");
        }
        else
        {
            var data = new SerializedObject(manager);
            SerializedProperty clouds = data.FindProperty("zoneClouds");
            bool found = false;

            for (int i = 0; i < clouds.arraySize; i++)
                found |= clouds.GetArrayElementAtIndex(i).objectReferenceValue == rewardPrefab;

            if (!found)
            {
                clouds.arraySize++;
                clouds.GetArrayElementAtIndex(clouds.arraySize - 1).objectReferenceValue = rewardPrefab;
                data.ApplyModifiedProperties();
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Dream Sky built. Save the scene.");
    }

    // ------------------------------------------------------------ Scene

    private static void BuildSceneObject(Material farCloud, Material sparkle)
    {
        var root = new GameObject(ObjectName);
        root.AddComponent<DreamSky>();

        BuildFarClouds(root.transform, farCloud);
        BuildSparkles(root.transform, sparkle);
    }

    // Older builds left X and Z in a different curve mode from Y, which Unity rejects.
    private static void RepairSparkles(Transform root)
    {
        Transform sparkles = root.Find("Sparkles");
        if (sparkles == null || !sparkles.TryGetComponent(out ParticleSystem system)) return;

        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(420f, 640f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    // Slow, huge and far below the obstacles: rising slower than they do reads as distance.
    private static void BuildFarClouds(Transform parent, Material material)
    {
        var go = new GameObject("Far Clouds");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, -5800f, 0f);

        ParticleSystem system = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 50f;
        main.startLifetime = 50f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(1000f, 1700f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 14;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0.26f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(7000f, 7000f, 1f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(60f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = FadeInOut(0.15f, 0.8f);

        ParticleSystem.TextureSheetAnimationModule sheet = system.textureSheetAnimation;
        sheet.enabled = true;
        sheet.numTilesX = 2;
        sheet.numTilesY = 2;
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
        sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 1f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.maxParticleSize = 4f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // Streaks stretched along their velocity, the near layer that shows the speed.
    private static void BuildSparkles(Transform parent, Material material)
    {
        var go = new GameObject("Sparkles");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, -700f, 0f);

        ParticleSystem system = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 5f;
        main.startLifetime = 1f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 22f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(900f, 900f, 1f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        // Unity needs all three axes in the same mode, so X and Z are ranges too.
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(420f, 640f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = FadeInOut(0.2f, 0.85f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.05f;
        renderer.lengthScale = 2f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Gradient FadeInOut(float fadeInEnd, float fadeOutStart)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, fadeInEnd),
                new GradientAlphaKey(1f, fadeOutStart),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    // ------------------------------------------------------------ Reward cloud

    private static GameObject BuildRewardCloud(Material material)
    {
        var root = new GameObject("ZC_RewardCloud");

        // The quad lies flat so its bounds suit a camera looking straight down.
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Cloud";
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(root.transform, false);
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = Vector3.one * 400f;

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        // Same speed as ZoneEnd, so the clouds and the reward arrive together.
        EndZoneAsset asset = root.AddComponent<EndZoneAsset>();
        var data = new SerializedObject(asset);
        data.FindProperty("damagesPlayer").boolValue = false;
        data.FindProperty("moveInLine").boolValue = true;
        data.FindProperty("moveInLineSettings.axis").enumValueIndex = (int)MoveAxis.Y;
        data.FindProperty("moveInLineSettings.speedMin").floatValue = 120f;
        data.FindProperty("moveInLineSettings.speedMax").floatValue = 120f;
        data.ApplyModifiedPropertiesWithoutUndo();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, RewardPrefabPath);
        Object.DestroyImmediate(root);
        return saved;
    }

    // ------------------------------------------------------------ Assets

    private static Material LoadOrCreateMaterial(string name, string shaderName, Texture2D texture)
    {
        string path = $"{Root}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        material = new Material(Shader.Find(shaderName));
        if (texture != null) material.SetTexture("_MainTex", texture);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Texture2D LoadOrBake(string path, System.Func<Texture2D> bake, TextureWrapMode wrap)
    {
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        Texture2D baked = bake();
        System.IO.File.WriteAllBytes(path, baked.EncodeToPNG());
        Object.DestroyImmediate(baked);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        // Masks, not colors, so no sRGB. No alpha, which keeps the compressed size down.
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.sRGBTexture = false;
        importer.wrapMode = wrap;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // Four clouds in a 2x2 grid, each a pile of overlapping puffs.
    // R is a smooth height, so the shader's threshold gives a clean edge at any size.
    // G is light coming from one corner.
    private static Texture2D BakeCloudAtlas()
    {
        const int size = 1024, cell = size / 2;
        var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        var pixels = new Color32[size * size];
        var random = new System.Random(911);

        for (int cy = 0; cy < 2; cy++)
        for (int cx = 0; cx < 2; cx++)
        {
            var puffs = new Vector3[16];

            for (int i = 0; i < puffs.Length; i++)
            {
                float t = i / (float)puffs.Length;
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float distance = (float)random.NextDouble() * Mathf.Lerp(0.05f, 0.27f, t);
                float radius = Mathf.Lerp(0.2f, 0.09f, t) * Mathf.Lerp(0.8f, 1.15f, (float)random.NextDouble());

                puffs[i] = new Vector3(0.5f + Mathf.Cos(angle) * distance * 1.25f, 0.5f + Mathf.Sin(angle) * distance, radius);
            }

            var heights = new float[cell * cell];

            for (int y = 0; y < cell; y++)
            for (int x = 0; x < cell; x++)
            {
                float u = x / (cell - 1f);
                float v = y / (cell - 1f);

                // A little noise makes the puffs lumpy rather than perfect circles.
                float lumpU = u + (Mathf.PerlinNoise(u * 9f + cx * 13f, v * 9f + cy * 7f) - 0.5f) * 0.05f;
                float lumpV = v + (Mathf.PerlinNoise(u * 9f + 31f, v * 9f + cx * 5f + cy * 11f) - 0.5f) * 0.05f;

                float height = 0f;
                foreach (Vector3 puff in puffs)
                {
                    float dx = lumpU - puff.x;
                    float dy = lumpV - puff.y;
                    float inside = 1f - (dx * dx + dy * dy) / (puff.z * puff.z);
                    if (inside > 0f) height = Mathf.Max(height, Mathf.Sqrt(inside) * 0.5f + 0.5f);
                }

                // Nothing touches the cell border, or neighbouring clouds would bleed in.
                float border = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                heights[y * cell + x] = height * Mathf.Clamp01(border * 12f);
            }

            for (int y = 1; y < cell - 1; y++)
            for (int x = 1; x < cell - 1; x++)
            {
                float height = heights[y * cell + x];
                float slopeX = heights[y * cell + x + 1] - heights[y * cell + x - 1];
                float slopeY = heights[(y + 1) * cell + x] - heights[(y - 1) * cell + x];

                float lit = Mathf.Clamp01(0.43f + (slopeY - slopeX) * 30f);

                pixels[(cy * cell + y) * size + cx * cell + x] =
                    new Color32((byte)(Mathf.Clamp01(height) * 255f), (byte)(lit * 255f), 0, 255);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false);
        return texture;
    }
}
