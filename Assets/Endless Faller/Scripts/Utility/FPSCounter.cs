using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    private float deltaTime;

    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    private void OnGUI()
    {
        float fps = 1.0f / deltaTime;

        GUIStyle style = new GUIStyle();
        style.fontSize = 50;
        style.normal.textColor = Color.black;
        style.fontStyle = FontStyle.Bold;

        GUI.Label(
            new Rect(200, 200, 200, 40),
            $"FPS: {Mathf.CeilToInt(fps)}",
            style
        );
    }
}