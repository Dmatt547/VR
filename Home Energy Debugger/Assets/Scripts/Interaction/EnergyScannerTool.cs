using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Goes on the scanner root, next to its XR Grab Interactable. Raycasts out of
// the scan tip to find an appliance to read, mirroring the raycast and delegate
// pattern from Week 5's ScalpelTool and CuttableObject: the tool finds a target
// and hands off to the target's own component rather than owning the reaction.
[RequireComponent(typeof(XRGrabInteractable))]
public class EnergyScannerTool : MonoBehaviour
{
    [Header("Scan Tip")]
    [SerializeField] private Transform scanTip;
    [SerializeField] private float scanRange = 15f;

    // Source: Week 5 workshop project (ScalpelTool.cs, cuttableLayers) - a LayerMask
    // restricts the raycast so walls and furniture are never returned as targets
    [SerializeField] private LayerMask scannableLayers = ~0;

    // Source: Week 5 workshop project (ScalpelTool.cs, TipRadius) - radius of the
    // fallback overlap check around the tip
    private const float TipRadius = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool logScans = true;

    private XRGrabInteractable grabInteractable;
    private bool isHeld = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (scanTip == null)
        {
            Debug.LogWarning("EnergyScannerTool is missing a Scan Tip reference in the Inspector - it will not scan.");
        }
    }

    // Source: Week 3 workshop project (FlightMarker.cs), reused in the Week 5 workshop
    // project (ScalpelTool.cs) - subscribe to the interactable's events in OnEnable and
    // unsubscribe in OnDisable, so listeners are never left dangling on a disabled object
    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
        grabInteractable.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
        grabInteractable.activated.RemoveListener(OnActivated);
    }

    private void OnGrabbed(SelectEnterEventArgs args) => isHeld = true;

    private void OnReleased(SelectExitEventArgs args) => isHeld = false;

    // Source: Week 5 workshop project (ScalpelTool.cs, OnActivated) - the activated
    // event is the trigger press while the object is held
    private void OnActivated(ActivateEventArgs args)
    {
        if (!isHeld) return;

        PerformScan();
    }

    // Source: Week 5 workshop project (ScalpelTool.cs, PerformCut) - raycast and
    // delegate: find a target, then call a method on that target's own component
    // instead of holding the reaction logic here
    private void PerformScan()
    {
        ScannableAppliance target = FindTarget();

        if (target == null)
        {
            if (logScans) Debug.Log("Scanner: nothing in range.");
            return;
        }

        bool started = target.Scan();

        if (logScans)
        {
            Debug.Log(started
                ? "Scanner: started scan on " + target.ApplianceName
                : "Scanner: " + target.ApplianceName + " already diagnosed.");
        }
    }

    public ScannableAppliance FindTarget()
    {
        if (scanTip == null) return null;

        // Source: Week 5 lecture slides, Raycasting (slide 27), as implemented in the
        // Week 5 workshop project (ScalpelTool.cs, FindTarget) - ray from the tip along
        // its own forward axis, layer filtered and ignoring trigger colliders
        if (Physics.Raycast(scanTip.position, scanTip.forward, out RaycastHit hit, scanRange,
                            scannableLayers, QueryTriggerInteraction.Ignore))
        {
            // Source: Week 2 workshop project (ColorSelector.cs), reused in the Week 5
            // workshop project (ScalpelTool.cs) - the collider hit is usually a child mesh,
            // so walk up to find the component that owns it
            ScannableAppliance hitTarget = hit.collider.GetComponentInParent<ScannableAppliance>();
            if (hitTarget != null) return hitTarget;
        }

        // Source: Week 5 workshop project (ScalpelTool.cs, FindTarget) - a ray that
        // starts inside a collider never reports it, so an overlap check covers the tip
        // being buried in the appliance
        Collider[] overlaps = Physics.OverlapSphere(scanTip.position, TipRadius, scannableLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            ScannableAppliance appliance = overlaps[i].GetComponentInParent<ScannableAppliance>();
            if (appliance != null) return appliance;
        }

        return null;
    }
}
