using System.Collections.Generic;
using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    public float speed = 2f;
    public Vector3 direction = Vector3.forward;

    private readonly List<Rigidbody> onBelt = new List<Rigidbody>();

    void FixedUpdate()
    {
        Vector3 beltVelocity = direction.normalized * speed;

        for (int i = onBelt.Count - 1; i >= 0; i--)
        {
            if (onBelt[i] == null) { onBelt.RemoveAt(i); continue; }

            Rigidbody rb = onBelt[i];
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(beltVelocity.x, v.y, beltVelocity.z);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null && !onBelt.Contains(rb)) onBelt.Add(rb);
    }

    private void OnCollisionExit(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null) onBelt.Remove(rb);
    }

    // --- XR support ---
    // Called by ItemXRGrabHandler when a VR controller grabs an item.
    // The grab uses XRGrabInteractable's Velocity Tracking mode, so the
    // Rigidbody stays fully physics-driven (not kinematic) while held. But if
    // the item is grabbed before it physically leaves the belt's collider,
    // this script's FixedUpdate and the grab's hand-following would both try
    // to control its velocity at once, causing jitter. Removing it from
    // onBelt immediately on grab avoids that conflict instead of waiting for
    // OnCollisionExit to fire naturally.
    public bool IsOnBelt(Rigidbody rb) => rb != null && onBelt.Contains(rb);

    public void RemoveFromBelt(Rigidbody rb)
    {
        if (rb != null) onBelt.Remove(rb);
    }
}