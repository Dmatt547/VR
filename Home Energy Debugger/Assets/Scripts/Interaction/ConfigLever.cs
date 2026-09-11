using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Goes on the insulation lever next to the fridge. A straight slide is simpler
// and more reliable than a twisting dial - no grab transformer components
// needed, just clamp the handle's own position along one local axis every
// frame, plain Vector3/Mathf code like everywhere else in this project.
//
// The lever is reusable rather than one-shot. Let go of it and it slides itself
// back to the closed end of the rail, and once it gets there it is ready to be
// pulled again for the next appliance. It never has to choose which appliance
// to fix, because ApplyUpgrade only acts on an appliance that is currently Leak
// Identified - the lever tells all of them and only the scanned one responds.
[RequireComponent(typeof(XRGrabInteractable))]
public class ConfigLever : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private ScannableAppliance targetAppliance;
    [SerializeField] private ScannableAppliance targetAppliance2;
    [SerializeField] private ScannableAppliance targetAppliance3;
    [SerializeField] private ScannableAppliance targetAppliance4;

    [Header("Slide Range")]
    // how far along the rail the handle is allowed to travel, in metres,
    // measured from wherever it started
    [SerializeField] private float maxSlideDistance = 0.15f;

    [Header("Upgrade")]
    [Range(0f, 1f)]
    [SerializeField] private float upgradeThreshold = 0.7f;

    [Header("Return")]
    // how fast the handle slides itself back once it is let go, in metres
    // per second
    [SerializeField] private float returnSpeed = 0.3f;

    private Vector3 startLocalPosition;

    // true from the moment the upgrade is sent until the handle is back at the
    // closed end, so holding it out does not fire the upgrade every frame
    private bool upgradeSent = false;

    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        // Source: Week 3 lecture slides, Transforms (slide 32) - transform.localPosition
        // is the offset from the parent, so the rail works wherever the lever is placed
        startLocalPosition = transform.localPosition;
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void Update()
    {
        float travelled = transform.localPosition.x - startLocalPosition.x;

        // Source: Week 6 workshop project (Aquarium.cs, ClampToBounds) - Mathf.Clamp keeps
        // a value inside a range, so pulling past the end of the rail does not count as
        // more than fully open and pushing back does not go negative
        travelled = Mathf.Clamp(travelled, 0f, maxSlideDistance);

        // Source: Week 5 workshop project (CuttableObject.cs) - isSelected reports
        // whether an interactable is currently held
        if (!grabInteractable.isSelected)
        {
            // Source: Week 3 workshop project (Aircraft.cs) using the Time.deltaTime * speed
            // pattern from the Week 5 lecture slides, Frame-Rate Independent Movement -
            // MoveTowards travels a fixed distance per second rather than per frame
            travelled = Mathf.MoveTowards(travelled, 0f, returnSpeed * Time.deltaTime);
        }

        // Source: Week 3 lecture slides, Transforms - rewriting localPosition every frame
        // locks Y and Z to their starting values, so the handle can only move along X
        // instead of floating wherever the hand drags it
        Vector3 clampedPosition = startLocalPosition;
        clampedPosition.x = startLocalPosition.x + travelled;
        transform.localPosition = clampedPosition;

        float value = travelled / maxSlideDistance;

        if (!upgradeSent && value >= upgradeThreshold)
        {
            upgradeSent = true;

            ApplyUpgradeTo(targetAppliance);
            ApplyUpgradeTo(targetAppliance2);
            ApplyUpgradeTo(targetAppliance3);
            ApplyUpgradeTo(targetAppliance4);
        }

        // back at the closed end, so the lever rearms for the next appliance
        if (upgradeSent && travelled <= 0f)
        {
            upgradeSent = false;
        }
    }

    // null-checked individually so an unassigned slot in the Inspector does
    // nothing, instead of throwing and leaving the rest unupgraded
    private void ApplyUpgradeTo(ScannableAppliance appliance)
    {
        if (appliance == null) return;

        appliance.ApplyUpgrade();
    }

    // Called by SessionManager when the player exits back to the start screen.
    // Snaps the handle straight home instead of sliding, since the briefing
    // panel is up and there is nobody watching the handle move.
    public void ResetLever()
    {
        transform.localPosition = startLocalPosition;
        upgradeSent = false;
    }
}
