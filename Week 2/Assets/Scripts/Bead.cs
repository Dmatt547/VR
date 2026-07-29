using UnityEngine;

// goes on the bead prefab - freezes in place when it hits the blocker
public class Bead : MonoBehaviour
{
    [SerializeField] private string blockerTag = "Blocker";

    private Rigidbody beadRigidbody;
    private Renderer beadRenderer;
    private bool hasFrozen = false;

    private void Awake()
    {
        beadRigidbody = GetComponent<Rigidbody>();
        beadRenderer = GetComponent<Renderer>();
    }

    // called by BeadGun right after it spawns this bead
    public void SetColour(Color colour)
    {
        if (beadRenderer != null)
        {
            beadRenderer.material.color = colour;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasFrozen) return;

        if (collision.collider.CompareTag(blockerTag))
        {
            FreezeInPlace();
        }
    }

    private void FreezeInPlace()
    {
        hasFrozen = true;

        beadRigidbody.linearVelocity = Vector3.zero;
        beadRigidbody.angularVelocity = Vector3.zero;
        beadRigidbody.isKinematic = true;
    }
}
