using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps the creature's body at a fixed ride height above the ground, without physics.
///
/// Rigidbody gravity is the wrong tool here. The solver writes bone rotations every LateUpdate,
/// so a physics body would be fighting it for control of the same transform, and the creature
/// has no collider to land on anyway. In procedural locomotion the body's height is DERIVED:
/// the feet decide where the ground is, and the body rides above them. This inverts the usual
/// dependency, which is the point — gravity pushes a body down onto its feet, whereas a
/// procedural creature reads its feet to decide where its body belongs.
///
/// Two sources, blended by footWeighting:
///   - a downward raycast from the body, which works even before any foot is planted
///   - the average height of the planted feet, which is what actually carries the creature
/// </summary>
[DefaultExecutionOrder(50)]          // before GaitStub (100) and the solver (200)
public class BodyGrounder : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional. When set, planted feet contribute to the body height.")]
    public GaitStub gait;

    [Header("Ride height")]
    [Tooltip("Metres from the ground to this transform's origin. Use the context menu item " +
             "'Capture Ride Height' to read it off the pose you have already set up.")]
    public float rideHeight = 0.9f;

    [Tooltip("0 = raycast only, 1 = planted feet only. Feet are more accurate once the gait " +
             "is running; the raycast is the fallback that always works.")]
    [Range(0f, 1f)] public float footWeighting = 0.6f;

    [Tooltip("Seconds to settle. Too low reads as a rigid hover, too high as a floaty lag.")]
    public float smoothTime = 0.15f;

    [Header("Ground")]
    public LayerMask groundMask = ~0;
    public float rayHeight = 4f;
    public float rayLength = 12f;

    [Header("Levelling")]
    [Tooltip("Hold pitch and roll at zero every frame. The creature only yaws. Without this, " +
             "any accumulated tilt compounds and the body slowly corkscrews as it turns.")]
    public bool keepLevel = true;

    [Header("Readout")]
    public float currentGroundY;

    float velY;

    void LateUpdate()
    {
        if (!TryGroundBelowBody(out float rayY)) return;

        float groundY = rayY;

        // Blend in the feet that are actually carrying the creature.
        if (gait != null && gait.feet != null && footWeighting > 0f)
        {
            float sum = 0f; int n = 0;
            foreach (var f in gait.feet)
            {
                if (f == null || !f.isPlanted) continue;
                sum += f.contactPoint.y;
                n++;
            }
            if (n > 0) groundY = Mathf.Lerp(rayY, sum / n, footWeighting);
        }

        currentGroundY = groundY;

        Vector3 p = transform.position;
        p.y = Mathf.SmoothDamp(p.y, groundY + rideHeight, ref velY, smoothTime);
        transform.position = p;

        if (keepLevel)
        {
            // NOT via eulerAngles. Reading eulerAngles decomposes the quaternion, and the
            // decomposition is not unique: (0.8, 55, 9) and (179.2, 125, 171) are the same
            // orientation. Writing the read-back value can therefore pick a different branch
            // each frame and jerk the yaw. Flattening the forward vector is idempotent.
            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        }
    }

    /// <summary>
    /// Moves the body vertically until the rig's own feet sit on the ground, then records that
    /// as the ride height. This is the setup step that matters: it makes the bind pose the
    /// resting pose, so the legs keep their natural bend instead of being stretched toward a
    /// ground plane that is too far below them.
    /// </summary>
    [ContextMenu("Drop Body So Feet Touch Ground")]
    void DropBodyToFeet()
    {
        if (gait == null || gait.feet == null || gait.feet.Count == 0)
        {
            Debug.LogWarning("[BodyGrounder] Assign the gait reference first.");
            return;
        }

        float sumDelta = 0f; int n = 0;

        foreach (var f in gait.feet)
        {
            if (f == null || f.tipBone == null) continue;

            Vector3 o = f.tipBone.position + Vector3.up * rayHeight;
            if (!Physics.Raycast(o, Vector3.down, out var hit, rayLength, groundMask)) continue;

            sumDelta += hit.point.y - f.tipBone.position.y;   // how far this foot must fall
            n++;
        }

        if (n == 0)
        {
            Debug.LogWarning("[BodyGrounder] No ground found under any foot. Check groundMask.");
            return;
        }

        transform.position += Vector3.up * (sumDelta / n);
        CaptureRideHeight();
        Debug.Log($"[BodyGrounder] Body moved {sumDelta / n:F3} m so the feet rest on the ground.");
    }

    bool TryGroundBelowBody(out float y)
    {
        Vector3 origin = transform.position + Vector3.up * rayHeight;
        if (Physics.Raycast(origin, Vector3.down, out var hit, rayLength, groundMask))
        {
            y = hit.point.y;
            return true;
        }
        y = 0f;
        return false;
    }

    /// <summary>Reads the ride height off the pose you have already positioned by hand.</summary>
    [ContextMenu("Capture Ride Height From Current Pose")]
    void CaptureRideHeight()
    {
        if (!TryGroundBelowBody(out float y))
        {
            Debug.LogWarning("[BodyGrounder] No ground below the body. Check groundMask and rayLength.");
            return;
        }
        rideHeight = transform.position.y - y;
        Debug.Log($"[BodyGrounder] rideHeight captured as {rideHeight:F3} m.");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 o = transform.position + Vector3.up * rayHeight;
        Gizmos.DrawLine(o, o + Vector3.down * rayLength);
        if (Application.isPlaying)
            Gizmos.DrawWireCube(new Vector3(transform.position.x, currentGroundY, transform.position.z),
                                new Vector3(0.4f, 0.01f, 0.4f));
    }
}
