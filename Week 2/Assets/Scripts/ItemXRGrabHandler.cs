using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Attach this alongside an XRGrabInteractable on the Item prefab.
// Lets a VR controller pick items off the conveyor belt: on grab, the item
// is removed from ConveyorBelt's tracked list so it stops being pushed;
// on release, the belt automatically re-detects it via OnCollisionEnter
// if it's set back down on the belt.
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class ItemXRGrabHandler : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        ConveyorBelt belt = FindFirstObjectByType<ConveyorBelt>();
        if (belt != null) belt.RemoveFromBelt(rb);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // No action needed: if dropped back on the belt, ConveyorBelt's OnCollisionEnter picks it up again automatically.
    }
}
