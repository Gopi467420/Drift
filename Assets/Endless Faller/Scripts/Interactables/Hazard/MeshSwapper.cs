using System;
using UnityEngine;

// Gives one prefab many looks: each spawn picks a random piece and fits the box collider
// to it. Runs on spawn, not Awake, because the pool reuses each copy without waking it again.
public class MeshSwapper : MonoBehaviour, IPoolable
{
    [Serializable]
    private struct Piece
    {
        public Mesh mesh;
        [Tooltip("Size of this piece. Models from one file often need different values.")]
        [Min(0.001f)] public float scale;
    }

    [Tooltip("One of these is picked on every spawn.")]
    [SerializeField] private Piece[] pieces;
    [Tooltip("Shows the mesh. Its rotation stands every piece upright.")]
    [SerializeField] private MeshFilter meshFilter;
    [Tooltip("Resized to fit the picked mesh.")]
    [SerializeField] private BoxCollider box;
    [Tooltip("Size of the box against the piece. Below 1, as curved pieces leave its corners empty.")]
    [Range(0.4f, 1f)][SerializeField] private float colliderFit = 0.75f;

    public void OnSpawn()
    {
        if (pieces.Length == 0) return;

        Apply(pieces[UnityEngine.Random.Range(0, pieces.Length)]);
    }

    public void OnDespawn() { }

    private void Apply(Piece piece)
    {
        meshFilter.sharedMesh = piece.mesh;

        Transform view = meshFilter.transform;
        Bounds bounds = piece.mesh.bounds;

        view.localScale = Vector3.one * piece.scale;

        // Models from one file are often placed apart, so each is moved back to the centre.
        view.localPosition = -(view.localRotation * (bounds.center * piece.scale));

        box.center = Vector3.zero;
        box.size = RotatedSize(bounds.size * piece.scale, view.localRotation) * colliderFit;
    }

    // The size a box has once turned, as the collider's axes don't turn with the mesh.
    private static Vector3 RotatedSize(Vector3 size, Quaternion rotation)
    {
        return Abs(rotation * new Vector3(size.x, 0f, 0f))
             + Abs(rotation * new Vector3(0f, size.y, 0f))
             + Abs(rotation * new Vector3(0f, 0f, size.z));
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}
