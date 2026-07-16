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
}