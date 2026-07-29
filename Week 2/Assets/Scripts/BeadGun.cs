using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Attach this to the bead gun's root GameObject, alongside an XRGrabInteractable.
[RequireComponent(typeof(XRGrabInteractable))]
public class BeadGun : MonoBehaviour
{
    [Header("Bead Setup")]
    [SerializeField] private GameObject beadPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireForce = 8f;

    [Header("Bonus: Fire Rate Limiting")]
    [Tooltip("Minimum seconds between shots so new beads don't collide with the ones just fired.")]
    [SerializeField] private float fireRate = 0.15f;

    [Header("Colour")]
    [Tooltip("Optional: part of the gun's mesh that visually shows the current colour.")]
    [SerializeField] private Renderer gunColourIndicator;
    [SerializeField] private Color currentColour = Color.red;

    private XRGrabInteractable _grabInteractable;
    private bool _isHeld;
    private float _lastFireTime;

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        _grabInteractable.selectEntered.AddListener(OnGrabbed);
        _grabInteractable.selectExited.AddListener(OnReleased);
        _grabInteractable.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        _grabInteractable.selectExited.RemoveListener(OnReleased);
        _grabInteractable.activated.RemoveListener(OnActivated);
    }

    // Bonus: track whether the gun is currently held.
    private void OnGrabbed(SelectEnterEventArgs args) => _isHeld = true;
    private void OnReleased(SelectExitEventArgs args) => _isHeld = false;

    // Fired when the controller trigger (the Activate input action) is pressed
    // while this object is selected/held.
    private void OnActivated(ActivateEventArgs args)
    {
        if (!_isHeld) return; // Bonus: only shoot once picked up.
        if (Time.time - _lastFireTime < fireRate) return; // Bonus: rate limit.

        _lastFireTime = Time.time;
        FireBead();
    }

    private void FireBead()
    {
        if (beadPrefab == null || firePoint == null)
        {
            Debug.LogWarning("BeadGun is missing a beadPrefab or firePoint reference.", this);
            return;
        }

        GameObject bead = Instantiate(beadPrefab, firePoint.position, firePoint.rotation);

        Bead beadScript = bead.GetComponent<Bead>();
        if (beadScript != null)
            beadScript.SetColour(currentColour);

        Rigidbody rb = bead.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(firePoint.forward * fireForce, ForceMode.VelocityChange);
    }

    // Called by ColorSelector when the gun touches a colour selection object.
    public void SetColour(Color newColour)
    {
        currentColour = newColour;
        if (gunColourIndicator != null)
            gunColourIndicator.material.color = newColour;
    }
}
