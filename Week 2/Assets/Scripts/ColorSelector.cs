using UnityEngine;

// This script goes on each colour selection object from the workshop task.
// Its collider must have "Is Trigger" enabled in the Inspector, since we use
// OnTriggerEnter here rather than OnCollisionEnter - the gun should pass
// through this object rather than physically collide with it.
public class ColorSelector : MonoBehaviour
{
    [SerializeField] private Color colour = Color.red;
    [SerializeField] private string gunTag = "BeadGun";

    [Header("Bonus: Direction-based colour")]
    [Tooltip("If enabled, the colour chosen depends on which side of this object the gun touches.")]
    [SerializeField] private bool useDirectionalColour = false;
    [SerializeField] private Color colourFront = Color.red;
    [SerializeField] private Color colourBack = Color.blue;

    // Unity calls this automatically when another collider enters this object's
    // trigger volume - the same collider-based event pattern used in the
    // Week 1 workshop script, just using OnTriggerEnter instead of OnCollisionEnter.
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(gunTag)) return;

        // The collider that actually touches this object belongs to one of the
        // gun's child parts (Handle or Barrel), so we search upward through its
        // parents to find the BeadGun script that lives on the root object.
        BeadGun gun = other.GetComponentInParent<BeadGun>();
        if (gun == null) return;

        Color chosenColour = colour;

        if (useDirectionalColour)
        {
            // Bonus challenge: work out which side of the selector the gun touched
            // by comparing this object's forward direction against the direction
            // the gun approached from, using the dot product between the two.
            Vector3 directionToGun = (other.transform.position - transform.position).normalized;
            float dotResult = Vector3.Dot(transform.forward, directionToGun);

            if (dotResult >= 0f)
            {
                chosenColour = colourFront;
            }
            else
            {
                chosenColour = colourBack;
            }
        }

        gun.SetColour(chosenColour);
    }
}
