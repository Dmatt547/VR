using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// This script controls the bead gun tool from the Week 2 workshop task.
// It works alongside an XR Grab Interactable component (added separately in
// the Inspector), which is what actually lets a controller pick the gun up.
//
// Rather than checking for input every frame inside Update(), this uses the
// event functions covered in lectures (OnEnable/OnDisable), combined with the
// events the XR Grab Interactable itself exposes for being grabbed, released,
// and "activated" (the controller trigger being pressed while held).
public class BeadGun : MonoBehaviour
{
    [Header("Bead Setup")]
    [SerializeField] private GameObject beadPrefab;    // Prefab to Instantiate each time we fire
    [SerializeField] private Transform firePoint;      // Where the bead spawns, and which way it should face
    [SerializeField] private float fireForce = 8f;     // Force applied to the bead's Rigidbody when fired

    [Header("Bonus: Fire Rate Limiting")]
    [Tooltip("Time to wait between shots so a new bead doesn't spawn on top of, and collide with, the last one.")]
    [SerializeField] private float fireRate = 0.15f;

    [Header("Colour")]
    [Tooltip("Optional: a Renderer on the gun that gets recoloured so the player can see the gun's current colour.")]
    [SerializeField] private Renderer gunColourIndicator;
    [SerializeField] private Color currentColour = Color.red;

    // Reference to the Grab Interactable component on this same GameObject.
    private XRGrabInteractable grabInteractable;

    // Tracks whether a controller is currently holding this object.
    private bool isHeld = false;

    // Used for the fire-rate-limiting bonus below.
    private bool canFire = true;

    // Awake() runs once when the script instance is first loaded. This is the
    // recommended place to cache component references we'll reuse, rather
    // than calling GetComponent() repeatedly later on.
    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable == null)
        {
            Debug.LogWarning("BeadGun requires an XR Grab Interactable component on the same GameObject.");
        }
    }

    // OnEnable()/OnDisable() are the Unity event functions covered in lectures.
    // Subscribing here (rather than in Awake/Start) means the listeners are
    // only active while this object is enabled in the scene, which avoids
    // leaving dangling references if it's ever disabled or destroyed.
    private void OnEnable()
    {
        if (grabInteractable == null) return;

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
        grabInteractable.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        if (grabInteractable == null) return;

        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
        grabInteractable.activated.RemoveListener(OnActivated);
    }

    // Called automatically by the Grab Interactable when a controller grabs this object.
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;
    }

    // Called automatically by the Grab Interactable when the controller releases this object.
    private void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
    }

    // Called automatically when the controller trigger is pressed while holding this object.
    // Bonus challenge: only fire once the gun has actually been picked up, and limit how
    // fast it can fire so beads don't spawn inside one another.
    private async void OnActivated(ActivateEventArgs args)
    {
        if (!isHeld) return;
        if (!canFire) return;

        canFire = false;
        FireBead();

        // Using the same Awaitable pattern from lectures (the Fade() example) to
        // pause here without freezing the rest of the game, then allow firing again.
        await Awaitable.WaitForSecondsAsync(fireRate);
        canFire = true;
    }

    // Creates a new bead at the fire point, passes it the gun's current colour,
    // and pushes it forward using the physics engine.
    private void FireBead()
    {
        if (beadPrefab == null || firePoint == null)
        {
            Debug.LogWarning("BeadGun is missing a Bead Prefab or Fire Point reference in the Inspector.");
            return;
        }

        // Instantiate the bead at the fire point's position and rotation, as covered in lectures.
        GameObject bead = Instantiate(beadPrefab, firePoint.position, firePoint.rotation);

        // Pass the current colour on to the new bead before it starts moving.
        Bead beadScript = bead.GetComponent<Bead>();
        if (beadScript != null)
        {
            beadScript.SetColour(currentColour);
        }

        // Apply a force along the direction the gun is facing so the bead flies outward.
        Rigidbody beadRigidbody = bead.GetComponent<Rigidbody>();
        if (beadRigidbody != null)
        {
            beadRigidbody.AddForce(firePoint.forward * fireForce, ForceMode.VelocityChange);
        }
    }

    // Public so the ColorSelector script on a separate object in the scene can call this
    // when the gun touches it, changing the gun's current colour and its visual indicator.
    public void SetColour(Color newColour)
    {
        currentColour = newColour;

        if (gunColourIndicator != null)
        {
            gunColourIndicator.material.color = newColour;
        }
    }
}
