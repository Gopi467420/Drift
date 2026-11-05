using System;
using System.Collections;
using UnityEngine;

// The player's entrance. The camera is a child of the player, so moving the real player
// would drag the camera along. Instead a stand-in copy of the player's look flies in
// from behind the camera, glitches halfway, then snaps to the real player.
public class PlayerIntro : MonoBehaviour
{
    [Tooltip("Seconds for the whole entrance, in real time.")]
    [SerializeField, Min(0.1f)] private float duration = 1.3f;
    [Tooltip("How far behind the camera it starts.")]
    [SerializeField] private float startBehindCamera = 6f;
    [Tooltip("Share of the flight that glitches.")]
    [SerializeField] private Vector2 glitchWindow = new Vector2(0.4f, 0.8f);
    [Tooltip("How far it jumps around while glitching.")]
    [SerializeField] private float glitchJump = 2.2f;
    [Tooltip("How many times per second the glitch jumps.")]
    [SerializeField] private float glitchRate = 24f;
    [Tooltip("The obstacle spawn glitch, as a material.")]
    [SerializeField] private Material glitchMaterial;

    public void Play(Action onFinished)
    {
        StartCoroutine(Run(onFinished));
    }

    private IEnumerator Run(Action onFinished)
    {
        // After Fall Again this runs on the first frame, so let every Start finish first.
        yield return null;

        Player player = Player.Instance;
        Camera view = FindFirstObjectByType<Camera>();

        Transform standIn = BuildStandIn(player);
        Renderer[] standInParts = standIn.GetComponentsInChildren<Renderer>();
        Material[][] realMaterials = new Material[standInParts.Length][];

        for (int i = 0; i < standInParts.Length; i++)
            realMaterials[i] = standInParts[i].sharedMaterials;

        bool wasGlitching = false;

        Vector3 end = player.transform.position;
        Vector3 start = view.transform.position - view.transform.forward * startBehindCamera;

        float age = 0f;

        while (age < duration)
        {
            age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(age / duration);

            // Fast past the camera, easing into place.
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            Vector3 position = Vector3.Lerp(start, end, eased);

            bool isGlitching = t >= glitchWindow.x && t <= glitchWindow.y;
            bool isShown = true;

            if (isGlitching != wasGlitching)
            {
                wasGlitching = isGlitching;

                for (int i = 0; i < standInParts.Length; i++)
                    standInParts[i].sharedMaterials = isGlitching ? AllGlitch(realMaterials[i].Length) : realMaterials[i];
            }

            if (isGlitching)
            {
                int step = Mathf.FloorToInt(age * glitchRate);

                // Stepped jumps, on and off, like a bad signal.
                position += view.transform.right * ((step % 3 - 1) * glitchJump);
                position += view.transform.up * ((step % 2 * 2 - 1) * glitchJump * 0.5f);
                isShown = step % 4 != 0;
            }

            standIn.position = position;

            foreach (Renderer part in standInParts) part.enabled = isShown;

            yield return null;
        }

        Destroy(standIn.gameObject);
        player.SetVisible(true);

        if (ComicEffects.Instance != null) ComicEffects.Instance.KickFov();

        onFinished?.Invoke();
    }

    private Material[] AllGlitch(int count)
    {
        var materials = new Material[count];
        for (int i = 0; i < count; i++) materials[i] = glitchMaterial;
        return materials;
    }

    // Copies only what can be seen: no scripts, colliders or camera.
    private static Transform BuildStandIn(Player player)
    {
        var root = new GameObject("Player Stand-In").transform;
        root.SetPositionAndRotation(player.transform.position, player.transform.rotation);

        foreach (MeshRenderer source in player.VisibleParts)
        {
            var part = new GameObject(source.name);
            part.transform.SetParent(root, false);
            part.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            part.transform.localScale = source.transform.lossyScale;

            part.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            part.AddComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
        }

        return root;
    }
}
