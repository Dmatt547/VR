using UnityEngine;

// This script goes on the Bead prefab. It caches the bead's Rigidbody and
// Renderer in Awake() so we don't need to call GetComponent() every time we
// need them, then uses them to freeze the bead in place and change its colour.
public class Bead : MonoBehaviour
{
    [Tooltip("The tag used on the Blocking Plane object, so we know what to freeze against.")]
    [SerializeField] private string blockerTag = "Blocker";

    private Rigidbody beadRigidbody;
    private Renderer beadRenderer;
    private bool hasFrozen = false;

    private void Awake()
    {
        beadRigidbody = GetComponent<Rigidbody>();
        beadRenderer = GetComponent<Renderer>();
    }

    // Called by BeadGun straight after Instantiate(), so the bead is the
    // correct colour before it starts moving.
    public void SetColour(Color colour)
    {
        if (beadRenderer != null)
        {
            // Accessing .material (rather than .sharedMaterial) creates a copy of the
            // material just for this bead, so recolouring one bead doesn't recolour
            // every other bead that uses the same material asset.
            beadRenderer.material.color = colour;
        }
    }

    // Unity calls OnCollisionEnter automatically whenever this object's collider
    // hits another (non-trigger) collider - the same collider-based interaction
    // concept used in the Week 1 workshop script.
    private void OnCollisionEnter(Collision collision)
    {
        if (hasFrozen) return;

        if (collision.collider.CompareTag(blockerTag))
        {
            FreezeInPlace();
        }
    }

    // Stops the bead from moving any further, so it "sticks" wherever it lands
    // on the blocking plane, rather than sliding or bouncing away.
    private void FreezeInPlace()
    {
        hasFrozen = true;

        beadRigidbody.linearVelocity = Vector3.zero;  // Unity 6 renamed Rigidbody.velocity to linearVelocity
        beadRigidbody.angularVelocity = Vector3.zero;
        beadRigidbody.isKinematic = true;              // Kinematic Rigidbodies ignore all physics forces
    }
}
