using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Endless Faller > 7. Assign Game Settings
// Creates the GameSettings asset, already holding the game's current values,
// and plugs it into GameManager. Safe to run again: it keeps an existing asset.
public static class GameSettingsSetup
{
    private const string Folder = "Assets/Endless Faller/Assets/Data";
    private const string AssetPath = Folder + "/GameSettings.asset";

    [MenuItem("Endless Faller/7. Assign Game Settings")]
    public static void Apply()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Endless Faller/Assets", "Data");

        GameSettings settings = AssetDatabase.LoadAssetAtPath<GameSettings>(AssetPath);

        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<GameSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
        }

        GameManager manager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (manager == null) { Debug.LogError("Game Settings: no GameManager in the open scene."); return; }

        var data = new SerializedObject(manager);
        data.FindProperty("settings").objectReferenceValue = settings;
        data.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Debug.Log("Game Settings assigned. Save the scene.");
    }
}
