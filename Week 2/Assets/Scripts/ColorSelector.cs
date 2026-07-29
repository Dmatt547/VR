using UnityEngine;

// changes the bead gun's colour when it touches this object
// collider on this object needs "Is Trigger" checked
public class ColorSelector : MonoBehaviour
{
    [SerializeField] private Color colour = Color.red;
    [SerializeField] private string gunTag = "BeadGun";

    [Header("Bonus: Direction-based colour")]
    [SerializeField] private bool useDirectionalColour = false;
    [SerializeField] private Color colourFront = Color.red;
    [SerializeField] private Color colourBack = Color.blue;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(gunTag)) return;

        // collider that touched us belongs to Handle/Barrel, so search up for the BeadGun script
        BeadGun gun = other.GetComponentInParent<BeadGun>();
        if (gun == null) return;

        Color chosenColour = colour;

        if (useDirectionalColour)
        {
            // which side of this object did the gun come from?
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
