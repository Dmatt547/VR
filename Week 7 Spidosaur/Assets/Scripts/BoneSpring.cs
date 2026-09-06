using UnityEngine;

// One spring between two bone markers. F = -k * (length - restLength).
[RequireComponent(typeof(LineRenderer))]
public class BoneSpring : MonoBehaviour
{
    [SerializeField] private Transform end1;
    [SerializeField] private Transform end2;
    [SerializeField] private float stiffness = 30f;

    private float restLength;
    private LineRenderer lineRenderer;

    public float RestLength => restLength;
    public float CurrentLength => (end1.position - end2.position).magnitude;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    // called by SpidosaurRig once the two ends are known
    public void Initialise(Transform first, Transform second, float springStiffness, float lineWidth, bool visible)
    {
        end1 = first;
        end2 = second;
        stiffness = springStiffness;
        restLength = Vector3.Distance(end1.position, end2.position);

        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.enabled = visible;
    }

    public Vector3 GetForce(Transform end)
    {
        Vector3 d = (end1.position - end2.position).normalized;
        float length = (end1.position - end2.position).magnitude;
        Vector3 force = -stiffness * (length - restLength) * d;

        // the maths above is written from end1's point of view
        if (end == end2)
        {
            force = -force;
        }

        return force;
    }

    private void LateUpdate()
    {
        if (!lineRenderer.enabled || end1 == null || end2 == null) return;

        lineRenderer.SetPosition(0, end1.position);
        lineRenderer.SetPosition(1, end2.position);
    }
}
