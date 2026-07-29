using UnityEngine;

// Attach this to the colour selection object. Give it a Collider with
// "Is Trigger" checked, and tag the bead gun's collider "BeadGun".
public class ColorSelector : MonoBehaviour
{
    [SerializeField] private Color colour = Color.red;
    [SerializeField] private string gunTag = "BeadGun";

    [Header("Bonus: Direction-based colour")]
    [Tooltip("If enabled, the colour chosen depends on which side of this object the gun touches.")]
    [SerializeField] private bool useDirectionalColour;
    [SerializeField] private Color colourFront = Color.red;
    [SerializeField] private Color colourBack = Color.blue;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(gunTag)) return;

        BeadGun gun = other.GetComponentInParent<BeadGun>();
        if (gun == null) return;

        Color chosen = colour;

        if (useDirectionalColour)
        {
            // Compare the gun's approach direction against this object's forward axis.
            Vector3 toGun = (other.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, toGun);
            chosen = dot >= 0f ? colourFront : colourBack;
        }

        gun.SetColour(chosen);
    }
}
