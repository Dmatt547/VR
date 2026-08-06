using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// goes on the flight marker prefab - one waypoint on an aircraft's route
// the user grabs these to sketch out the path the aircraft flies
[RequireComponent(typeof(XRGrabInteractable))]
public class FlightMarker : MonoBehaviour
{
    [SerializeField] private Color heldColour = new Color(1f, 1f, 0f, 0.6f);

    private XRGrabInteractable grabInteractable;
    private Renderer markerRenderer;
    private Color idleColour;

    // the aircraft reads this so it doesn't chase a marker the user is moving
    public bool IsHeld { get; private set; }

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        markerRenderer = GetComponent<Renderer>();

        // take the idle colour from the material, so the material controls
        // how the marker looks and the script only overrides it while held
        idleColour = markerRenderer.material.color;
    }

    // listen to the grab interactable's events instead of polling for input
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
        IsHeld = true;
        markerRenderer.material.color = heldColour;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        IsHeld = false;
        markerRenderer.material.color = idleColour;
    }
}
