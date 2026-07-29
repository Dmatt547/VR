using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// controls the bead gun tool - fires a bead when the trigger is pressed while held
public class BeadGun : MonoBehaviour
{
    [Header("Bead Setup")]
    [SerializeField] private GameObject beadPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireForce = 8f;

    [Header("Bonus: Fire Rate Limiting")]
    [SerializeField] private float fireRate = 0.15f;

    [Header("Colour")]
    [SerializeField] private Renderer gunColourIndicator;
    [SerializeField] private Color currentColour = Color.red;

    private XRGrabInteractable grabInteractable;
    private bool isHeld = false;
    private bool canFire = true;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable == null)
        {
            Debug.LogWarning("BeadGun requires an XR Grab Interactable component on the same GameObject.");
        }
    }

    // hook into the grab interactable's own events instead of polling for input
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

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
    }

    // only fire if actually picked up, and not still on cooldown
    private async void OnActivated(ActivateEventArgs args)
    {
        if (!isHeld) return;
        if (!canFire) return;

        canFire = false;
        FireBead();

        // wait before allowing the next shot so beads don't spawn on top of each other
        await Awaitable.WaitForSecondsAsync(fireRate);
        canFire = true;
    }

    private void FireBead()
    {
        if (beadPrefab == null || firePoint == null)
        {
            Debug.LogWarning("BeadGun is missing a Bead Prefab or Fire Point reference in the Inspector.");
            return;
        }

        GameObject bead = Instantiate(beadPrefab, firePoint.position, firePoint.rotation);

        Bead beadScript = bead.GetComponent<Bead>();
        if (beadScript != null)
        {
            beadScript.SetColour(currentColour);
        }

        Rigidbody beadRigidbody = bead.GetComponent<Rigidbody>();
        if (beadRigidbody != null)
        {
            beadRigidbody.AddForce(firePoint.forward * fireForce, ForceMode.VelocityChange);
        }
    }

    // called by ColorSelector when the gun touches a colour selector
    public void SetColour(Color newColour)
    {
        currentColour = newColour;

        if (gunColourIndicator != null)
        {
            gunColourIndicator.material.color = newColour;
        }
    }
}
