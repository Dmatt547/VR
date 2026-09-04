using UnityEngine;

/// <summary>
/// Hands one sub-leg's goal to the user. The foot is permanently unplanted, so it carries
/// the low weight and absorbs junction error, which is exactly the intended priority order:
/// ground contact outranks the controller.
/// </summary>
[DefaultExecutionOrder(150)]
public class ControllerFootTarget : MonoBehaviour
{
    public Transform controller;
    public FootEffector effector;
    public float maxReach = 1.5f;
    public Transform hip;

    void LateUpdate()
    {
        if (controller == null || effector == null) return;

        Vector3 p = controller.position;
        if (hip != null)
        {
            Vector3 d = p - hip.position;
            if (d.magnitude > maxReach) p = hip.position + d.normalized * maxReach;
        }
        transform.position = p;

        effector.isPlanted  = false;
        effector.plantBlend = 0f;
    }
}
