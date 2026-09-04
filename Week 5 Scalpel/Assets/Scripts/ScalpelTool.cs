using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Goes on the scalpel root, next to its XR Grab Interactable.
// The cut plane is the blade tip plus the axis running through the flat of the blade
// (slide 28), and a raycast out of the tip finds what to cut (slide 27).
[RequireComponent(typeof(XRGrabInteractable))]
public class ScalpelTool : MonoBehaviour
{
    // which local axis of the blade tip runs through the flat of the blade
    public enum BladeAxis { Right, Up, Forward }

    // how far around the tip to check for a collider the blade is already inside
    private const float TipRadius = 0.02f;

    [Header("Blade")]
    [SerializeField] private Transform bladeTip;
    [SerializeField] private BladeAxis planeNormalAxis = BladeAxis.Right;
    [SerializeField] private float cutRange = 0.6f;
    [SerializeField] private LayerMask cuttableLayers = ~0;

    [Header("Fire Rate Limiting")]
    [SerializeField] private float cutCooldown = 0.3f;

    [Header("Debug")]
    [SerializeField] private CutMarkerSpawner markerSpawner;
    [SerializeField] private bool logCuts = true;

    private XRGrabInteractable grabInteractable;
    private bool isHeld = false;
    private bool canCut = true;

    // reused instead of allocating a new list on every trigger pull
    private readonly List<Vector3> crossings = new List<Vector3>();

    // the plane the blade would cut along right now. CutPreview reads it too
    public Plane CutPlane => new Plane(PlaneNormal, bladeTip.position);

    private Vector3 PlaneNormal =>
        planeNormalAxis == BladeAxis.Up ? bladeTip.up :
        planeNormalAxis == BladeAxis.Forward ? bladeTip.forward : bladeTip.right;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (bladeTip == null)
        {
            Debug.LogWarning("ScalpelTool is missing a Blade Tip reference in the Inspector - it will not cut.");
        }
    }

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

    // only cut if picked up and not still on cooldown
    private async void OnActivated(ActivateEventArgs args)
    {
        if (!isHeld || !canCut) return;

        canCut = false;
        PerformCut();

        await Awaitable.WaitForSecondsAsync(cutCooldown);

        // the scalpel may have been destroyed while we were waiting
        if (this == null) return;

        canCut = true;
    }

    private void PerformCut()
    {
        if (bladeTip == null) return;

        CuttableObject target = FindTarget(out float distance);
        if (target == null) return;

        crossings.Clear();

        // the plane is infinite, so the whole object gets cut, not just the part under the tip
        if (!target.TryCut(CutPlane, out GameObject[] pieces, crossings)) return;

        if (markerSpawner != null) markerSpawner.ShowPoints(crossings);

        if (logCuts)
        {
            Debug.Log("Scalpel cut '" + target.name + "' into " + pieces.Length +
                      " pieces across " + crossings.Count + " intersection points.");
        }
    }

    // a ray straight out of the tip. public because CutPreview calls it too, so the preview
    // and the cut can't disagree. distance is how far along the ray the target sits.
    public CuttableObject FindTarget(out float distance)
    {
        distance = cutRange;

        if (bladeTip == null) return null;

        if (Physics.Raycast(bladeTip.position, bladeTip.forward, out RaycastHit hit, cutRange,
                            cuttableLayers, QueryTriggerInteraction.Ignore))
        {
            CuttableObject hitTarget = hit.collider.GetComponentInParent<CuttableObject>();

            // don't return on a null, or something on the layer without the component
            // would swallow the trigger pull
            if (hitTarget != null)
            {
                distance = hit.distance;
                return hitTarget;
            }
        }

        // a ray starting inside a collider doesn't report it, so check for the blade
        // already being buried in something
        Collider[] overlaps = Physics.OverlapSphere(bladeTip.position, TipRadius, cuttableLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            CuttableObject cuttable = overlaps[i].GetComponentInParent<CuttableObject>();
            if (cuttable != null) return cuttable;
        }

        return null;
    }
}
