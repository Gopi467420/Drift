using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Menu: Endless Faller > 5. Apply Neon Look
// Moves the game from cartoon toward neon realism: real clouds from Textures/Cloud.tif,
// quiet comic shading, stronger bloom, new sky colors. Also adds the stamina bar, the
// impact frame's hero material and the ComicEffects manager. Safe to run again.
public static class NeonLookSetup
{
    private const string Dream = "Assets/Endless Faller/Assets/Dream";
    private const string CloudTexture = "Assets/Endless Faller/Textures/Cloud.tif";
    private const string PlayerPrefab = "Assets/Endless Faller/Prefabs/Player.prefab";
    private const string ZoneEndPrefab = "Assets/Endless Faller/Prefabs/Interacbles/Collectables/ZoneEnd.prefab";
    private const string StylePath = "Assets/Endless Faller/Assets/Materials/ComicStyle.asset";
    private const string ProfilePath = "Assets/Settings/SampleSceneProfile.asset";
    private const string DefaultProfilePath = "Assets/Settings/DefaultVolumeProfile.asset";

    [MenuItem("Endless Faller/5. Apply Neon Look")]
    public static void Apply()
    {
        ImportCloud();
        Texture2D cloud = AssetDatabase.LoadAssetAtPath<Texture2D>(CloudTexture);

        ApplyClouds(cloud);
        ApplySky();
        ApplyStyle();
        ApplyBloom();
        BuildHeroMaterial();

        AssetDatabase.SaveAssets();

        SetUpScene();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Neon look applied. Save the scene.");
    }

    private static void ImportCloud()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(CloudTexture);
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.maxTextureSize = 512;   // low detail: clouds are soft and only ever pass by
        importer.SaveAndReimport();
    }

    private static void ApplyClouds(Texture2D cloud)
    {
        Material far = AssetDatabase.LoadAssetAtPath<Material>(Dream + "/M_DreamFarCloud.mat");
        MakeSoft(far, cloud);

        Material reward = AssetDatabase.LoadAssetAtPath<Material>(Dream + "/M_RewardCloud.mat");
        MakeSoft(reward, cloud);
        far.SetFloat("_Opacity", 0.6f);
        reward.SetFloat("_Opacity", 0.7f);
        reward.SetFloat("_Reward", 0.15f);

        string zonePath = Dream + "/M_ZoneCloud.mat";
        Material zone = AssetDatabase.LoadAssetAtPath<Material>(zonePath);
        if (zone == null)
        {
            zone = new Material(reward);
            AssetDatabase.CreateAsset(zone, zonePath);
        }

        // Clouds are switched off for now. These objects stay, so they can come back.
        // The cloud bank on ZoneEnd itself, which the player bursts through.
        GameObject prefab = PrefabUtility.LoadPrefabContents(ZoneEndPrefab);

        if (prefab.transform.Find("Cloud Deck") == null)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Cloud Deck";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(prefab.transform, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(0.17f, 0.17f, 1f);

            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = zone;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        prefab.transform.Find("Cloud Deck").gameObject.SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(prefab, ZoneEndPrefab);
        PrefabUtility.UnloadPrefabContents(prefab);
    }

    private static void MakeSoft(Material material, Texture2D cloud)
    {
        material.SetTexture("_MainTex", cloud);
        material.SetFloat("_Soft", 1f);
        material.EnableKeyword("_DREAM_SOFT");
        EditorUtility.SetDirty(material);
    }

    private static void ApplyStyle()
    {
        ComicStyleSettings style = AssetDatabase.LoadAssetAtPath<ComicStyleSettings>(StylePath);

        // Comic shading stays as a texture in the shadows, not the main look.
        style.hatching.strength = 0.1f;
        style.halftone.strength = 0.65f;

        style.surface.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CloudTexture);

        // Objects glitch in at the spawner's Spawn Distance.
        style.spawn.distance = 1700f;
        style.spawn.band = 220f;

        // Complementary pair, for the spawn shapes and the glitch ghosts.
        style.glitch.colorA = new Color(0.1f, 1f, 0.55f);
        style.glitch.colorB = new Color(1f, 0.05f, 0.55f);

        // Edge light from the poster palette.
        style.rim.colorA = new Color(0.2f, 0.75f, 0.8f);
        style.rim.colorB = new Color(1f, 0.3f, 0.1f);

        EditorUtility.SetDirty(style);
    }

    private static void ApplyBloom()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null || !profile.TryGet(out Bloom bloom)) return;

        // Only the hottest things glow, so shapes keep their hard edges.
        bloom.threshold.Override(1.2f);
        bloom.intensity.Override(0.45f);
        bloom.scatter.Override(0.55f);
        EditorUtility.SetDirty(profile);

        // Strong color fringing softens every edge on screen.
        VolumeProfile defaults = AssetDatabase.LoadAssetAtPath<VolumeProfile>(DefaultProfilePath);
        if (defaults != null && defaults.TryGet(out ChromaticAberration aberration))
        {
            aberration.intensity.Override(0.12f);
            EditorUtility.SetDirty(defaults);
        }
    }

    private static void BuildHeroMaterial()
    {
        string path = "Assets/Endless Faller/Assets/Materials/M_PlayerHero.mat";
        Material hero = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (hero == null)
        {
            Material source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Endless Faller/Assets/Materials/M_ComicBase.mat");
            hero = new Material(source);
            AssetDatabase.CreateAsset(hero, path);
        }

        GameObject prefab = PrefabUtility.LoadPrefabContents(PlayerPrefab);

        foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.name == "M_ComicBase")
                renderer.sharedMaterial = hero;

        PrefabUtility.SaveAsPrefabAsset(prefab, PlayerPrefab);
        PrefabUtility.UnloadPrefabContents(prefab);
    }

    private static void SetUpScene()
    {
        // Replacing the component brings in the new default palettes.
        DreamSky sky = Object.FindFirstObjectByType<DreamSky>(FindObjectsInactive.Include);
        if (sky != null)
        {
            GameObject skyObject = sky.gameObject;
            Object.DestroyImmediate(sky);
            skyObject.AddComponent<DreamSky>();

            FasterStreaks(skyObject.transform.Find("Sparkles"));
            DenserFarClouds(skyObject.transform.Find("Far Clouds"));
            skyObject.transform.Find("Far Clouds").gameObject.SetActive(true);

            // A whole cloud now, so the 2x2 atlas cells are not wanted.
            Transform far = skyObject.transform.Find("Far Clouds");
            if (far != null)
            {
                ParticleSystem.TextureSheetAnimationModule sheet = far.GetComponent<ParticleSystem>().textureSheetAnimation;
                sheet.enabled = false;
            }
        }

        DenserRewardClouds();

        GameManager manager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (manager != null)
        {
            ComicEffects effects = manager.GetComponent<ComicEffects>();
            if (effects == null) effects = manager.gameObject.AddComponent<ComicEffects>();

            AssignImpactDots(effects);
        }

        Player player = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (player == null) return;

        Transform bar = player.transform.Find("Stamina Bar");
        if (bar != null) Object.DestroyImmediate(bar.gameObject);

    }

    private static void DenserFarClouds(Transform clouds)
    {
        if (clouds == null) return;

        var system = clouds.GetComponent<ParticleSystem>();

        ParticleSystem.MainModule main = system.main;
        main.maxParticles = 10;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0.2f;
    }

    private static void DenserRewardClouds()
    {
        RandomSpawner spawner = Object.FindFirstObjectByType<RandomSpawner>(FindObjectsInactive.Include);
        if (spawner != null)
        {
            var data = new SerializedObject(spawner);
            data.FindProperty("zoneCloudsPerZone").intValue = 8;
            data.FindProperty("spawnZoneClouds").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        ObjectPoolManager pools = Object.FindFirstObjectByType<ObjectPoolManager>(FindObjectsInactive.Include);
        if (pools != null)
        {
            var data = new SerializedObject(pools);
            data.FindProperty("zoneCloudPrewarm").intValue = 18;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void FasterStreaks(Transform sparkles)
    {
        if (sparkles == null) return;

        sparkles.localPosition = new Vector3(0f, -1500f, 0f);

        var system = sparkles.GetComponent<ParticleSystem>();

        ParticleSystem.MainModule main = system.main;
        main.startLifetime = 0.75f;
        main.startSize = new ParticleSystem.MinMaxCurve(7f, 15f);
        main.maxParticles = 80;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 55f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.scale = new Vector3(1500f, 1500f, 1f);

        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(1700f, 2500f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var renderer = sparkles.GetComponent<ParticleSystemRenderer>();
        renderer.velocityScale = 0.09f;
        renderer.lengthScale = 3f;
    }

    // The surge quad lives on the camera, and the player's material draws after it,
    // so the player stays on top of the effect.
    public static void AssignImpactDots(ComicEffects effects)
    {
        string path = Dream + "/M_ImpactDots.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(Shader.Find("Custom/ImpactDots"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetFloat("_HoleSize", 0.05f);
        EditorUtility.SetDirty(material);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        Transform oldImage = canvas.transform.Find("Impact Dots");
        if (oldImage != null) Object.DestroyImmediate(oldImage.gameObject);

        Camera camera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
        Transform surge = camera.transform.Find("Power Surge");

        if (surge == null)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Power Surge";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            surge = quad.transform;
            surge.SetParent(camera.transform, false);
        }

        // In front of the camera so it is never culled. The shader fills the screen.
        surge.localPosition = new Vector3(0f, 0f, 5f);
        surge.localRotation = Quaternion.identity;

        MeshRenderer renderer = surge.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;

        var data = new SerializedObject(effects);
        data.FindProperty("surgeRenderer").objectReferenceValue = renderer;
        data.ApplyModifiedPropertiesWithoutUndo();

        Material hero = AssetDatabase.LoadAssetAtPath<Material>("Assets/Endless Faller/Assets/Materials/M_PlayerHero.mat");
        hero.renderQueue = 3100;
        EditorUtility.SetDirty(hero);
    }

    // The sky is worked out entirely in its shader, so it only needs its settings.
    private static void ApplySky()
    {
        Material sky = AssetDatabase.LoadAssetAtPath<Material>(Dream + "/M_DreamSky.mat");
        sky.SetTexture("_MainTex", null);
        sky.SetFloat("_FlowSpeed", 0.5f);
        sky.SetFloat("_Fade", 0.62f);
        sky.SetFloat("_SunGlow", 0.24f);
        EditorUtility.SetDirty(sky);
    }
}
