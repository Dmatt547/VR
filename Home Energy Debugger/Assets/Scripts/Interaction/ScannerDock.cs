using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Goes on the wall cradle the scanner is returned to at the end of a job.
// Seating the scanner here is the player arranging an object into a particular
// configuration, and the cradle is only reachable by carrying the tool across
// the room from the bench, so the response is driven by where the player has
// moved. Docking writes an inspection report; lifting it back out clears it.
[RequireComponent(typeof(Collider))]
public class ScannerDock : MonoBehaviour
{
    [Header("Scanner")]
    [SerializeField] private XRGrabInteractable scanner;
    // where the scanner is held once seated, so it sits neatly in the cradle
    [SerializeField] private Transform dockPoint;

    [Header("Report")]
    [SerializeField] private ScannableAppliance[] appliances;

    // Source: Unity TextMeshPro documentation - not used in any weekly project; TMP_Text
    // is the shared base type for the world space and UI text components, so either fits
    [SerializeField] private TMP_Text reportText;

    // the scanner is usually carried in while still held, so being inside the
    // cradle and being docked are two different things
    private bool scannerInside;
    private bool docked;

    // Source: Week 2 workshop project (ColorSelector.cs) - OnTriggerEnter fires once when
    // another collider enters a collider marked Is Trigger, not every frame it stays
    private void OnTriggerEnter(Collider other)
    {
        if (IsScanner(other)) scannerInside = true;
    }

    // Source: Week 2 workshop project (ConveyorBelt.cs) - the enter/exit pairing, used
    // there with OnCollisionEnter and OnCollisionExit to track what is on the belt
    private void OnTriggerExit(Collider other)
    {
        if (!IsScanner(other)) return;

        scannerInside = false;

        if (docked) Undock();
    }

    // Source: Week 2 workshop project (ColorSelector.cs) - the collider that enters
    // belongs to a child mesh, so walk up to the parent to identify what owns it
    private bool IsScanner(Collider other)
    {
        return other.GetComponentInParent<XRGrabInteractable>() == scanner;
    }

    private void Update()
    {
        if (docked)
        {
            // Source: Week 5 workshop project (CuttableObject.cs) - isSelected reports
            // whether the interactable is currently held
            if (scanner.isSelected)
            {
                Undock();
                return;
            }

            // Source: Week 3 lecture slides, Transforms - rewriting the transform every
            // frame holds the object in place, the same way ConfigLever clamps its handle
            // back onto the rail, so gravity cannot pull it out of the cradle
            scanner.transform.SetPositionAndRotation(dockPoint.position, dockPoint.rotation);
            return;
        }

        // inside the cradle and no longer in the player's hand, so it has been
        // put away rather than carried through
        if (scannerInside && !scanner.isSelected) Dock();
    }

    private void Dock()
    {
        docked = true;

        // Source: Unity Scripting API (Transform.SetPositionAndRotation) - not used in any
        // weekly project; sets both in one call instead of assigning them separately
        scanner.transform.SetPositionAndRotation(dockPoint.position, dockPoint.rotation);

        // Source: Week 6 workshop project (Fish.cs, IsActiveSwimmer checks) - read each
        // object's state when needed rather than keeping a running total that could drift
        int upgraded = 0;

        foreach (ScannableAppliance appliance in appliances)
        {
            if (appliance.State == ScannableAppliance.ApplianceState.UpgradeApplied)
            {
                upgraded++;
            }
        }

        string report = "Inspection report: " + upgraded + " of " + appliances.Length + " appliances upgraded.";

        if (upgraded == appliances.Length) report += " House complete.";

        if (reportText != null) reportText.text = report;

        Debug.Log("Scanner docked. " + report);
    }

    private void Undock()
    {
        docked = false;

        if (reportText != null) reportText.text = "";
    }

    // Called by SessionManager when the player exits back to the start screen.
    // Clears the dock before the scanner is moved, so Update stops holding it
    // in the cradle and the pose reset can take effect.
    public void ResetDock()
    {
        docked = false;
        scannerInside = false;

        if (reportText != null) reportText.text = "";
    }
}
