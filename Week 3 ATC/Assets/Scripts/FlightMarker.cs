using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// goes on the flight marker prefab - one waypoint on an aircraft's route
// the user grabs these to sketch out the path the aircraft flies
[RequireComponent(typeof(XRGrabInteractable))]
public class FlightMarker : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Renderer markerRenderer;
    [SerializeField] private Color idleColour = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color heldColour = new Color(1f, 1f, 0f, 0.6f);

    private XRGrabInteractable grabInteractable;
    private bool isHeld = false;

    // the aircraft reads this so it doesn't chase a marker the user is still moving
    public bool IsHeld => isHeld;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (markerRenderer == null)
        {
            markerRenderer = GetComponent<Renderer>();
        }

        SetColour(idleColour);
    }

    // same pattern as the bead gun - listen to the grab interactable's events
    // instead of polling the controller for input every frame
    private void OnEnable()
    {
        if (grabInteractable == null) return;

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        if (grabInteractable == null) return;

        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;
        SetColour(heldColour);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
        SetColour(idleColour);
    }

    private void SetColour(Color colour)
    {
        if (markerRenderer != null)
        {
            markerRenderer.material.color = colour;
        }
    }
}
