using UnityEngine;

// A hollow cylinder collider, for obstacles the player flies through. Unity has no such
// shape and a hollow Mesh Collider can't be a trigger, so this builds a ring of box
// colliders when the game starts. The shape shows in the Scene view while you adjust it.
// The hole runs along this object's Y axis (the green arrow).
//
// Runs before Obstacle, which collects its colliders in Awake for the near-miss check.
[DefaultExecutionOrder(-100)]
public class HollowCylinderCollider : MonoBehaviour
{
    [Header("Shape")]
    [Tooltip("Radius of the hole.")]
    [SerializeField, Min(0.01f)] private float innerRadius = 70f;
    [Tooltip("Radius of the outside edge.")]
    [SerializeField, Min(0.02f)] private float outerRadius = 120f;
    [Tooltip("How thick the ring is along its hole.")]
    [SerializeField, Min(0.01f)] private float depth = 40f;
    [Tooltip("Moves the shape, to line it up with your mesh.")]
    [SerializeField] private Vector3 center;
    [Tooltip("How many boxes make the ring. More is rounder but costs more.")]
    [Range(6, 32)][SerializeField] private int segments = 12;

    [Header("Debug")]
    [SerializeField] private bool showShape = true;
    [SerializeField] private Color outlineColor = Color.green;
    [SerializeField] private Color boxColor = new Color(0f, 1f, 0f, 0.25f);

    private float SegmentAngle => 360f / segments;
    private float MiddleRadius => (innerRadius + outerRadius) * 0.5f;

    // Wide enough for neighbouring boxes to meet at the outside edge, leaving no gaps.
    private Vector3 BoxSize => new Vector3(
        2f * outerRadius * Mathf.Tan(Mathf.PI / segments),
        depth,
        outerRadius - innerRadius);

    private void Awake()
    {
        BuildColliders();

        // Trigger events from child colliders need a Rigidbody on the object above them.
        if (GetComponentInParent<Rigidbody>() == null)
            gameObject.AddComponent<Rigidbody>().isKinematic = true;
    }

    private void BuildColliders()
    {
        var holder = new GameObject("Hollow Collider").transform;
        holder.SetParent(transform, false);
        holder.localPosition = center;

        for (int i = 0; i < segments; i++)
        {
            var part = new GameObject("Box " + i);
            part.layer = gameObject.layer;
            part.transform.SetParent(holder, false);
            part.transform.localRotation = Quaternion.Euler(0f, i * SegmentAngle, 0f);
            part.transform.localPosition = part.transform.localRotation * (Vector3.forward * MiddleRadius);

            BoxCollider box = part.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = BoxSize;
        }
    }

    private void OnValidate()
    {
        outerRadius = Mathf.Max(outerRadius, innerRadius + 0.01f);
    }

    private void OnDrawGizmos()
    {
        if (!showShape) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 top = center + Vector3.up * depth * 0.5f;
        Vector3 bottom = center - Vector3.up * depth * 0.5f;

        Gizmos.color = boxColor;

        for (int i = 0; i < segments; i++)
        {
            Quaternion turn = Quaternion.Euler(0f, i * SegmentAngle, 0f);

            Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.TRS(center + turn * (Vector3.forward * MiddleRadius), turn, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, BoxSize);
        }

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = outlineColor;

        DrawCircle(top, innerRadius);
        DrawCircle(bottom, innerRadius);
        DrawCircle(top, outerRadius);
        DrawCircle(bottom, outerRadius);

        Gizmos.DrawLine(bottom, top + Vector3.up * depth);
    }

    private static void DrawCircle(Vector3 middle, float radius)
    {
        const int Steps = 48;
        Vector3 last = middle + Vector3.forward * radius;

        for (int i = 1; i <= Steps; i++)
        {
            Vector3 next = middle + Quaternion.Euler(0f, i * 360f / Steps, 0f) * (Vector3.forward * radius);

            Gizmos.DrawLine(last, next);
            last = next;
        }
    }
}
