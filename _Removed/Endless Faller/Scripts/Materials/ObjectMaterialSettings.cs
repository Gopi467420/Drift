
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class ObjectMaterialSettings : MonoBehaviour
{
    [Header("Object Appearance")]
    [SerializeField] private Texture baseTexture;
    [SerializeField] private Color baseColor = Color.white;

    private Renderer objectRenderer;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int BaseMapID =
        Shader.PropertyToID("_BaseMap");

    private static readonly int BaseColorID =
        Shader.PropertyToID("_BaseColor");

    private void OnEnable()
    {
        ApplySettings();
    }

    private void OnValidate()
    {
        ApplySettings();
    }

    private void Awake()
    {
        ApplySettings();
    }

    public void SetBaseTexture(Texture newTexture)
    {
        baseTexture = newTexture;
        ApplySettings();
    }

    public void SetBaseColor(Color newColor)
    {
        baseColor = newColor;
        ApplySettings();
    }

    public void ApplySettings()
    {
        if (objectRenderer == null)
            objectRenderer = GetComponent<Renderer>();

        if (objectRenderer == null)
            return;

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        objectRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor(BaseColorID, baseColor);

        if (baseTexture != null)
            propertyBlock.SetTexture(BaseMapID, baseTexture);

        objectRenderer.SetPropertyBlock(propertyBlock);
    }
}
