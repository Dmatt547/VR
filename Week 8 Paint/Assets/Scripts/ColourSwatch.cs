using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Goes on each cube of the colour palette in the scene.
// This is the configuration interface for the week: poking or selecting a
// swatch loads that colour into the brush.
//
// Same event handler pattern as the element spheres in week 4 - the simple
// interactable raises selectEntered, this script just responds.
[RequireComponent(typeof(XRSimpleInteractable))]
public class ColourSwatch : MonoBehaviour
{
    [SerializeField] private Color colour = Color.red;
    [SerializeField] private PaintBrush brush;

    private XRSimpleInteractable swatchInteractable;

    private void Awake()
    {
        swatchInteractable = GetComponent<XRSimpleInteractable>();
    }

    private void Start()
    {
        // tint the swatch to match the colour it sets, so the palette is
        // readable without labels
        GetComponent<Renderer>().material.color = colour;
    }

    private void OnEnable()
    {
        swatchInteractable.selectEntered.AddListener(OnSelected);
    }

    private void OnDisable()
    {
        swatchInteractable.selectEntered.RemoveListener(OnSelected);
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        if (brush == null) return;

        brush.SetColour(colour);
    }
}
