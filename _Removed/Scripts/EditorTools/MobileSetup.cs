using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;

// Menu: Endless Faller > 6. Fix Mobile And Camera
// Makes the on-screen controls work on phones, pins the stamina bar to the bottom of
// the screen so it fits every screen shape, and gives Cinemachine a stable up
// direction. Safe to run again. Save the scene afterwards.
public static class MobileSetup
{
    private const string UpName = "Camera Up";

    [MenuItem("Endless Faller/6. Fix Mobile And Camera")]
    public static void Apply()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null) { Debug.LogError("Mobile setup: no Canvas in the open scene."); return; }

        SetUpControls(canvas.transform);
        PinStaminaBar(canvas.transform);
        SetCameraUp();

        ComicEffects effects = Object.FindFirstObjectByType<ComicEffects>(FindObjectsInactive.Include);
        if (effects != null) NeonLookSetup.AssignImpactDots(effects);

        KeepPlayerOnTop();

        AssetDatabase.SaveAssets();

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("Mobile and camera fixes applied. Save the scene.");
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;

        return null;
    }

    private static void SetUpControls(Transform canvas)
    {
        var names = new[] { "Image (1)", "Stick Base", "Image", "PauseMenuButton" };
        var controls = new GameObject[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            Transform found = canvas.Find(names[i]);
            if (found != null) controls[i] = found.gameObject;
        }

        MobileControls mobile = canvas.GetComponent<MobileControls>();
        if (mobile == null) mobile = canvas.gameObject.AddComponent<MobileControls>();

        var data = new SerializedObject(mobile);
        SerializedProperty list = data.FindProperty("touchOnlyObjects");
        list.arraySize = controls.Length;

        for (int i = 0; i < controls.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = controls[i];

        data.ApplyModifiedPropertiesWithoutUndo();

        // Slow-mo moves to the same virtual gamepad as the stick. The left trigger is
        // not bound to anything else, so only MobileControls reacts to it.
        Transform slowMo = canvas.Find("Image");
        OnScreenButton button = slowMo != null ? slowMo.GetComponent<OnScreenButton>() : null;

        if (button != null)
        {
            var buttonData = new SerializedObject(button);
            buttonData.FindProperty("m_ControlPath").stringValue = "<Gamepad>/leftTrigger";
            buttonData.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // Pinned to the bottom center: the stick and slow-mo own the bottom corners, and an
    // edge anchor keeps it on screen whatever the shape of the phone.
    private static void PinStaminaBar(Transform canvas)
    {
        Transform hud = FindDeep(canvas, "Stamina HUD");
        if (hud == null) return;

        var rect = (RectTransform)hud;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 62f);
    }

    // Every visible part of the player draws after the surge. Materials are copied, as
    // some of them come from packages and can't be changed, and they look the same.
    private static void KeepPlayerOnTop()
    {
        const int AfterSurge = 3100;
        const string Prefix = "M_PlayerOnTop_";
        const string Folder = "Assets/Endless Faller/Assets/Materials/";

        Player player = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (player == null) return;

        foreach (MeshRenderer renderer in player.GetComponentsInChildren<MeshRenderer>(true))
        {
            // The camera is a child of the player, so its surge quad is in this list too.
            if (renderer.GetComponentInParent<Camera>(true) != null) continue;

            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                if (source == null || source.name.StartsWith(Prefix)) continue;

                string path = $"{Folder}{Prefix}{source.name.Replace(" ", "")}.mat";
                Material copy = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (copy == null)
                {
                    copy = new Material(source);
                    AssetDatabase.CreateAsset(copy, path);
                }

                copy.renderQueue = AfterSurge;
                EditorUtility.SetDirty(copy);
                materials[i] = copy;
            }

            renderer.sharedMaterials = materials;
        }
    }

    // The camera looks straight down, which is the one direction world up can't steady:
    // aim shifts make the view swing around it. This gives Cinemachine an up that sits
    // across the view instead, matching how the camera is already turned.
    private static void SetCameraUp()
    {
        CinemachineBrain brain = Object.FindFirstObjectByType<CinemachineBrain>(FindObjectsInactive.Include);
        CinemachineCamera virtualCamera = Object.FindFirstObjectByType<CinemachineCamera>(FindObjectsInactive.Include);
        Player player = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Include);

        if (brain == null || virtualCamera == null || player == null) return;

        Transform up = player.transform.Find(UpName);

        if (up == null)
        {
            up = new GameObject(UpName).transform;
            up.SetParent(player.transform, false);
        }

        up.localPosition = Vector3.zero;
        up.rotation = Quaternion.LookRotation(Vector3.down, virtualCamera.transform.up);

        var data = new SerializedObject(brain);
        data.FindProperty("WorldUpOverride").objectReferenceValue = up;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
