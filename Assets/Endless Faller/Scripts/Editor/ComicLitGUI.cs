
using UnityEditor;
using UnityEngine;

// Material inspector for Custom/ComicLit: only what each material needs.
// The shared look (hatching, halftone, glitch) lives on the ComicStyle asset.
public class ComicLitGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        MaterialProperty baseMap = FindProperty("_BaseMap", properties);
        MaterialProperty baseColor = FindProperty("_BaseColor", properties);
        MaterialProperty halftone = FindProperty("_ComicHalftone", properties);
        MaterialProperty glitch = FindProperty("_ComicGlitch", properties);

        // Albedo texture with a tint beside it (final color = texture x tint).
        // Leave the tint white to show the texture as painted.
        materialEditor.TexturePropertySingleLine(
            new GUIContent("Base Color", "Albedo / base color texture. The swatch tints it."),
            baseMap,
            baseColor);
        materialEditor.ShaderProperty(halftone, "Halftone Dots");

        // The ghost pass costs vertices even when calm, so skip it when Glitch is off.
        EditorGUI.BeginChangeCheck();
        materialEditor.ShaderProperty(glitch, "Glitch");
        if (EditorGUI.EndChangeCheck())
        {
            foreach (Object target in materialEditor.targets)
                ((Material)target).SetShaderPassEnabled("SRPDefaultUnlit", glitch.floatValue > 0.5f);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Hatching is always on. Hatching, halftone and glitch settings are shared by every material.",
            MessageType.Info);

        if (GUILayout.Button("Edit Comic Style Settings"))
        {
            string[] guids = AssetDatabase.FindAssets("t:ComicStyleSettings");

            if (guids.Length > 0)
            {
                Object settings = AssetDatabase.LoadAssetAtPath<ComicStyleSettings>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
        }

        EditorGUILayout.Space();
        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
    }
}
