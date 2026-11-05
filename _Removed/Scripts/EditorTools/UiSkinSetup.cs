using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Menu: Endless Faller > 4. Apply UI Skin
// Builds the Faller Slash font asset, the text materials and the hand-painted UI
// sprites, then restyles and lays out the Canvas in the open scene.
// Safe to run again: existing assets are kept, and the scene is simply restyled.
public static class UiSkinSetup
{
    private const string Fonts = "Assets/Endless Faller/Fonts";
    private const string Sprites = "Assets/Endless Faller/Assets/UI";
    private const string Characters =
        " ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,!?:-+/'%";

    private static readonly Color Ink = new Color(0.1f, 0.04f, 0.2f);
    private static readonly Color Yellow = new Color(1f, 0.93f, 0.1f);
    private static readonly Color Orange = new Color(1f, 0.62f, 0.05f);
    private static readonly Color Pink = new Color(1f, 0.1f, 0.55f);
    private static readonly Color Cyan = new Color(0f, 0.85f, 1f);
    private static readonly Color Green = new Color(0.4f, 1f, 0.35f);

    private const int SpriteSize = 512;
    private const int BarWidth = 1024, BarHeight = 192;

    [MenuItem("Endless Faller/4. Apply UI Skin")]
    public static void Apply()
    {
        TMP_FontAsset font = LoadOrCreateFont();
        Material hud = LoadOrCreateMaterial(font, "FallerSlash - HUD", Pink, 0.26f, 0.75f, -0.8f);
        Material label = LoadOrCreateMaterial(font, "FallerSlash - Label", Ink, 0.2f, 0.4f, -0.45f);

        if (!AssetDatabase.IsValidFolder(Sprites))
            AssetDatabase.CreateFolder("Assets/Endless Faller/Assets", "UI");

        Sprite knob = LoadOrBake("S_StickKnob", () => Paint(Circle(0.36f), Yellow, Pink));
        Sprite ring = LoadOrBake("S_StickBase", PaintRing);
        Sprite slowMo = LoadOrBake("S_ButtonRound", () => Paint(Circle(0.38f), Pink, Cyan));
        Sprite slash = LoadOrBake("S_ButtonSlash", () => Paint(Slash, Cyan, Pink));
        Sprite slashPink = LoadOrBake("S_ButtonSlashPink", () => Paint(Slash, Pink, Cyan));

        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null) { Debug.LogError("UI Skin: no Canvas in the open scene."); return; }

        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = font;
            text.fontSharedMaterial = label;
            text.fontStyle = FontStyles.Normal;
            text.enableVertexGradient = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
        }

        Transform root = canvas.transform;

        // Score: large and centered at the top.
        TMP_Text score = Text(root, "Score");
        Place(score.rectTransform, new Vector2(1f, 1f), new Vector2(-470f, -105f), new Vector2(880f, 170f));
        Style(score, 150f, Color.white, TextAlignmentOptions.Right, hud);
        score.enableVertexGradient = true;
        score.colorGradient = new VertexGradient(Yellow, Yellow, Orange, Orange);

        TMP_Text scoreLabel = Text(root, "Score (1)");
        scoreLabel.text = "SCORE";
        Place(scoreLabel.rectTransform, new Vector2(1f, 1f), new Vector2(-200f, -205f), new Vector2(300f, 50f));
        Style(scoreLabel, 38f, Cyan, TextAlignmentOptions.Right, label);

        TMP_Text multiplier = Text(root, "ScoreMultiplier");
        multiplier.text = "x0.00";
        Place(multiplier.rectTransform, new Vector2(1f, 1f), new Vector2(-200f, -258f), new Vector2(300f, 76f));
        Style(multiplier, 64f, Yellow, TextAlignmentOptions.Right, hud);

        FindDeep(root, "ScoreMultiplier (1)").gameObject.SetActive(false);

        TMP_Text stamina = Text(root, "Stamina");
        stamina.text = "0";
        Place(stamina.rectTransform, new Vector2(1f, 0.5f), new Vector2(-170f, -40f), new Vector2(260f, 90f));
        Style(stamina, 76f, Green, TextAlignmentOptions.Left, hud);

        TMP_Text staminaLabel = Text(root, "Stamina (1)");
        staminaLabel.text = "STAMINA";
        Place(staminaLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-170f, 24f), new Vector2(260f, 56f));
        Style(staminaLabel, 40f, Cyan, TextAlignmentOptions.Left, label);
        stamina.gameObject.SetActive(false);

        // Stamina bar: beside pause, always in the same place.
        staminaLabel.gameObject.SetActive(true);
        Place(staminaLabel.rectTransform, new Vector2(0f, 1f), new Vector2(375f, -84f), new Vector2(260f, 46f));
        Style(staminaLabel, 36f, Cyan, TextAlignmentOptions.Left, label);

        Sprite barBack = LoadOrBake("S_StaminaBack", () => PaintBar(false), BarWidth, BarHeight);
        Sprite barFill = LoadOrBake("S_StaminaFill", () => PaintBar(true), BarWidth, BarHeight);

        Transform existingHud = FindDeep(root, "Stamina HUD");
        RectTransform hudRect = existingHud != null ? (RectTransform)existingHud : NewImage("Stamina HUD", root);
        Place(hudRect, new Vector2(0f, 1f), new Vector2(500f, -148f), new Vector2(520f, 96f));

        Image hudBack = hudRect.GetComponent<Image>();
        hudBack.sprite = barBack;
        hudBack.raycastTarget = false;

        Transform existingFill = hudRect.Find("Fill");
        RectTransform fillRect = existingFill != null ? (RectTransform)existingFill : NewImage("Fill", hudRect);
        Fill(fillRect);

        Image hudFill = fillRect.GetComponent<Image>();
        hudFill.sprite = barFill;
        hudFill.type = Image.Type.Filled;
        hudFill.fillMethod = Image.FillMethod.Horizontal;
        hudFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        hudFill.raycastTarget = false;

        StaminaHud staminaHud = hudRect.GetComponent<StaminaHud>();
        if (staminaHud == null) staminaHud = hudRect.gameObject.AddComponent<StaminaHud>();

        var hudData = new SerializedObject(staminaHud);
        hudData.FindProperty("fill").objectReferenceValue = hudFill;
        hudData.ApplyModifiedPropertiesWithoutUndo();

        // Reward lines sit under the score, away from the player in the middle.
        TMP_Text reward = Text(root, "RewardDisplay");
        reward.text = "";
        Place(reward.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -215f), new Vector2(700f, 60f));
        Style(reward, 40f, Yellow, TextAlignmentOptions.Center, hud);
        if (reward.GetComponent<RewardPopups>() == null) reward.gameObject.AddComponent<RewardPopups>();

        // Slow-mo button.
        var slowMoButton = (RectTransform)FindDeep(root, "Image");
        Skin(slowMoButton, slowMo, 180f);
        TMP_Text slowMoText = slowMoButton.GetComponentInChildren<TMP_Text>(true);
        Fill(slowMoText.rectTransform);
        slowMoText.text = "SLOW\nMO";
        Style(slowMoText, 42f, Color.white, TextAlignmentOptions.Center, label);

        // Stick: the knob travels inside a base ring, which stays still.
        var knobRect = (RectTransform)FindDeep(root, "Image (1)");
        Skin(knobRect, knob, 125f);
        knobRect.GetComponent<OnScreenStick>().movementRange = 125f;

        Transform existingBase = FindDeep(root, "Stick Base");
        RectTransform baseRect = existingBase != null ? (RectTransform)existingBase : NewImage("Stick Base", root);
        baseRect.anchorMin = baseRect.anchorMax = knobRect.anchorMin;
        baseRect.anchoredPosition = knobRect.anchoredPosition;
        baseRect.SetSiblingIndex(knobRect.GetSiblingIndex());
        Skin(baseRect, ring, 340f);
        baseRect.GetComponent<Image>().raycastTarget = false;

        // Pause.
        var pause = (RectTransform)FindDeep(root, "PauseMenuButton");
        Skin(pause, slashPink, 140f);
        TMP_Text pauseText = pause.GetComponentInChildren<TMP_Text>(true);
        Fill(pauseText.rectTransform);
        pauseText.rectTransform.localScale = Vector3.one;
        pauseText.text = "II";
        Style(pauseText, 58f, Color.white, TextAlignmentOptions.Center, label);

        // Pause menu. Its sizes stay as they were, only the look changes.
        Transform menu = FindDeep(root, "PauseMenu");
        menu.Find("Image").GetComponent<Image>().color = new Color(Ink.r, Ink.g, Ink.b, 0.86f);

        SkinKeepSize(menu.Find("QuitButton"), slashPink);
        SkinKeepSize(menu.Find("Decrease"), slash);
        SkinKeepSize(menu.Find("Increase"), slash);

        menu.Find("QuitButton").GetComponentInChildren<TMP_Text>(true).color = Color.white;

        TMP_Text minus = menu.Find("Decrease").GetComponentInChildren<TMP_Text>(true);
        minus.text = "-";
        minus.rectTransform.anchoredPosition = Vector2.zero;

        Transform slider = menu.Find("Slider");
        slider.Find("Background").GetComponent<Image>().color = new Color(Ink.r, Ink.g, Ink.b, 1f);
        slider.Find("Fill Area/Fill").GetComponent<Image>().color = Pink;
        var handle = slider.Find("Handle Slide Area/Handle").GetComponent<Image>();
        handle.sprite = knob;
        handle.color = Color.white;

        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("UI skin applied. Save the scene.");
    }

    // ------------------------------------------------------------ Scene helpers

    private static TMP_Text Text(Transform root, string name) => FindDeep(root, name).GetComponent<TMP_Text>();

    // Searches the whole Canvas, so objects can be moved under other parents.
    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;

        return null;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Fill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void Style(TMP_Text text, float size, Color color, TextAlignmentOptions alignment, Material material)
    {
        text.enableAutoSizing = false;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontSharedMaterial = material;
    }

    private static void Skin(RectTransform rect, Sprite sprite, float size)
    {
        rect.sizeDelta = new Vector2(size, size);
        SkinKeepSize(rect, sprite);
    }

    private static void SkinKeepSize(Transform target, Sprite sprite)
    {
        var image = target.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
    }

    private static RectTransform NewImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // ------------------------------------------------------------ Font

    private static TMP_FontAsset LoadOrCreateFont()
    {
        string path = Fonts + "/FallerSlash SDF.asset";
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing != null) return existing;

        Font source = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "/FallerSlash.ttf");

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
            source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, false);

        asset.TryAddCharacters(Characters);

        // Static, so builds need neither the .ttf nor any text generation at runtime.
        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        asset.name = "FallerSlash SDF";

        AssetDatabase.CreateAsset(asset, path);

        asset.atlasTexture.name = "FallerSlash Atlas";
        AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);

        asset.material.name = "FallerSlash Atlas Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    private static Material LoadOrCreateMaterial(TMP_FontAsset font, string name, Color shadow, float outline, float shadowX, float shadowY)
    {
        string path = $"{Fonts}/{name}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var material = new Material(font.material) { name = name };
        material.shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
        material.EnableKeyword("OUTLINE_ON");
        material.EnableKeyword("UNDERLAY_ON");
        material.SetColor("_OutlineColor", Ink);
        material.SetFloat("_OutlineWidth", outline);
        material.SetFloat("_FaceDilate", 0.3f);
        material.SetColor("_UnderlayColor", shadow);
        material.SetFloat("_UnderlayOffsetX", shadowX);
        material.SetFloat("_UnderlayOffsetY", shadowY);
        material.SetFloat("_UnderlayDilate", 0.3f);
        material.SetFloat("_UnderlaySoftness", 0f);

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // ------------------------------------------------------------ Sprites

    private static Sprite LoadOrBake(string name, System.Func<Color32[]> paint, int width = SpriteSize, int height = SpriteSize)
    {
        string path = $"{Sprites}/{name}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(paint());
        texture.Apply(false);
        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = Mathf.Max(width, height);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Signed distances: below zero is inside the shape. x and y run from -0.5 to 0.5.
    private static System.Func<float, float, float> Circle(float radius) =>
        (x, y) => Mathf.Sqrt(x * x + y * y) - radius;

    private static float Slash(float x, float y)
    {
        float dx = Mathf.Abs(x - y * 0.25f) - 0.32f;
        float dy = Mathf.Abs(y) - 0.26f;
        return Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f))
            + Mathf.Min(Mathf.Max(dx, dy), 0f) - 0.05f;
    }

    private static float Dots(float x, float y, float cell, float radius)
    {
        float u = (x + y) / cell, v = (x - y) / cell;
        float du = u - Mathf.Floor(u) - 0.5f, dv = v - Mathf.Floor(v) - 0.5f;
        return Mathf.Sqrt(du * du + dv * dv) < radius ? 1f : 0f;
    }

    private static float Brushed(float x, float y) =>
        (Mathf.PerlinNoise(x * 7f + 11f, y * 7f + 3f) - 0.5f) * 0.035f +
        (Mathf.PerlinNoise(x * 3f + 40f, y * 26f) - 0.5f) * 0.03f;

    // Offset color layer, ink outline, then a flat fill with print dots and a brushed shine.
    private static Color32[] Paint(System.Func<float, float, float> shape, Color fill, Color shadow)
    {
        var pixels = new Color32[SpriteSize * SpriteSize];

        for (int py = 0; py < SpriteSize; py++)
        for (int px = 0; px < SpriteSize; px++)
        {
            float x = px / (float)SpriteSize - 0.5f;
            float y = py / (float)SpriteSize - 0.5f;

            float edge = shape(x, y) + Brushed(x, y);
            float offsetEdge = shape(x - 0.035f, y + 0.045f) + Brushed(x - 0.035f, y + 0.045f);

            Color color = Color.clear;

            if (offsetEdge < 0.03f) color = shadow;
            if (edge < 0.03f) color = Ink;

            if (edge < 0f)
            {
                color = fill;

                // Print dots shade the lower side, growing toward the bottom corner.
                float shade = Mathf.Clamp01((-y + x * 0.4f + 0.05f) * 3f);
                if (Dots(x, y, 0.045f, shade * 0.52f) > 0f) color = Color.Lerp(fill, Ink, 0.45f);

                // A dry brush shine across the top.
                float shine = Mathf.Clamp01((y - 0.1f) * 5f) * Mathf.PerlinNoise(x * 5f + 3f, y * 60f);
                if (shine > 0.42f && edge < -0.03f) color = Color.Lerp(fill, Color.white, 0.75f);
            }

            pixels[py * SpriteSize + px] = color;
        }

        return pixels;
    }

    // A slanted brush bar. back = ink plate with a colored shadow.
    // fill = white with print dots and dry streaks, so the Image color tints all of it.
    private static Color32[] PaintBar(bool fill)
    {
        var pixels = new Color32[BarWidth * BarHeight];

        for (int py = 0; py < BarHeight; py++)
        for (int px = 0; px < BarWidth; px++)
        {
            float x = (px - BarWidth * 0.5f) / BarHeight;
            float y = (py - BarHeight * 0.5f) / BarHeight;

            float slanted = x - y * 0.45f;
            float halfWidth = BarWidth / (float)BarHeight * 0.5f - 0.3f;
            float rough = (Mathf.PerlinNoise(x * 6f + 11f, y * 6f) - 0.5f) * 0.06f;

            float edge = Mathf.Max(Mathf.Abs(slanted) - halfWidth, Mathf.Abs(y) - 0.36f) + rough;
            float shadow = Mathf.Max(Mathf.Abs(slanted - 0.05f) - halfWidth, Mathf.Abs(y + 0.07f) - 0.36f) + rough;

            Color color = Color.clear;

            if (fill)
            {
                if (edge < -0.1f)
                {
                    color = Color.white;

                    if (Dots(x, y, 0.1f, Mathf.Clamp01(-y * 2.6f + 0.1f) * 0.5f) > 0f) color = new Color(0.72f, 0.72f, 0.72f);

                    // Dry brush streaks run along the bar.
                    if (Mathf.PerlinNoise(x * 1.5f + 7f, y * 34f) > 0.7f) color = new Color(0.86f, 0.86f, 0.86f);
                }
            }
            else
            {
                if (shadow < 0f) color = Pink;
                if (edge < 0f) color = Ink;
            }

            pixels[py * BarWidth + px] = color;
        }

        return pixels;
    }

    // Thin painted ring with a faint dotted floor.
    private static Color32[] PaintRing()
    {
        var pixels = new Color32[SpriteSize * SpriteSize];

        for (int py = 0; py < SpriteSize; py++)
        for (int px = 0; px < SpriteSize; px++)
        {
            float x = px / (float)SpriteSize - 0.5f;
            float y = py / (float)SpriteSize - 0.5f;
            float radius = Mathf.Sqrt(x * x + y * y);
            float edge = Mathf.Abs(radius - 0.43f) - 0.016f + Brushed(x, y);

            Color color = Color.clear;

            if (radius < 0.42f && Dots(x, y, 0.04f, 0.2f) > 0f) color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.38f);
            if (edge < 0.014f) color = Ink;
            if (edge < 0f) color = Cyan;

            pixels[py * SpriteSize + px] = color;
        }

        return pixels;
    }
}
