using UnityEngine;

/// <summary>
/// One foot at the end of a sub-leg. Holds the tip bone, the world-space goal,
/// and how hard this foot pulls on the shared junction above it.
/// Weight is the whole idea: a planted foot outranks a lifted one.
/// </summary>
[DisallowMultipleComponent]
public class FootEffector : MonoBehaviour
{
    [Header("Rig")]
    [Tooltip("Tip bone of this sub-leg. The solver drives the chain ending here.")]
    public Transform tipBone;

    [Tooltip("World-space goal this foot is trying to reach.")]
    public Transform target;

    [Header("Contact state")]
    [Tooltip("1 = fully planted, 0 = fully lifted. Eased by the gait planner so the weight never steps.")]
    [Range(0f, 1f)] public float plantBlend = 1f;

    [Tooltip("Set by the gait planner. Only planted feet are measured for contact error.")]
    public bool isPlanted = true;

    [Tooltip("Last confirmed ground contact. Siblings must plan their footholds outside this point.")]
    public Vector3 contactPoint;

    [Header("Weights")]
    public float plantedWeight = 1.0f;
    public float liftedWeight  = 0.1f;

    /// <summary>How hard this foot pulls the junction toward its own target.</summary>
    public float Weight => Mathf.Lerp(liftedWeight, plantedWeight, plantBlend);
}
