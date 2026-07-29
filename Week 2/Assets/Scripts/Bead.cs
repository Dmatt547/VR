using UnityEngine;

// Attach this to the Bead prefab, alongside a Rigidbody and Collider.
[RequireComponent(typeof(Rigidbody))]
public class Bead : MonoBehaviour
{
    [Tooltip("Tag used on the blocking plane object.")]
    [SerializeField] private string blockerTag = "Blocker";

    private Rigidbody _rb;
    private Renderer _renderer;
    private bool _frozen;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _renderer = GetComponentInChildren<Renderer>();
    }

    // Called by BeadGun right after it instantiates this bead.
    public void SetColour(Color colour)
    {
        if (_renderer != null)
            _renderer.material.color = colour; // .material (not .sharedMaterial) creates a per-instance copy.
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_frozen) return;
        if (collision.collider.CompareTag(blockerTag))
            Freeze();
    }

    private void Freeze()
    {
        _frozen = true;
        _rb.linearVelocity = Vector3.zero; // Unity 6: Rigidbody.velocity was renamed to linearVelocity.
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
    }
}
