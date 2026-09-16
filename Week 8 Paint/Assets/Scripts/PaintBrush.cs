using UnityEngine;

// Goes on the Brush prefab, next to its XR Grab Interactable.
// Fires a short ray out of the brush tip each frame and, when it lands on the
// picture, hands the texture coordinate it hit to the canvas.
//
// A ray is used rather than a collision, because RaycastHit reports the texture
// coordinate of the hit for free, which is exactly what the canvas needs to
// work out which region was touched.
public class PaintBrush : MonoBehaviour
{
    [Header("Brush")]
    [SerializeField] private Transform brushTip;

    // how far past the tip the brush paints, in metres. short, so the picture
    // has to actually be touched
    [SerializeField] private float reach = 0.05f;

    [SerializeField] private Color brushColour = Color.red;

    // shown on the brush head, so the player can see which colour is loaded
    [SerializeField] private Renderer brushHeadRenderer;

    private void Start()
    {
        ShowBrushColour();
    }

    // called by the colour swatches in the scene
    public void SetColour(Color colour)
    {
        brushColour = colour;
        ShowBrushColour();
    }

    private void ShowBrushColour()
    {
        if (brushHeadRenderer == null) return;

        brushHeadRenderer.material.color = brushColour;
    }

    private void Update()
    {
        if (brushTip == null) return;

        // out of the tip, along the brush
        if (!Physics.Raycast(brushTip.position, brushTip.forward, out RaycastHit hit, reach)) return;

        PaintCanvas canvas = hit.collider.GetComponent<PaintCanvas>();

        if (canvas == null) return;

        // textureCoord only works against a mesh collider, which is why the
        // picture cube needs one instead of the default box collider
        canvas.PaintAt(hit.textureCoord, brushColour);
    }

    // draws the brush's reach in the editor, so the tip can be positioned
    // without pressing play
    private void OnDrawGizmosSelected()
    {
        if (brushTip == null) return;

        Gizmos.color = brushColour;
        Gizmos.DrawLine(brushTip.position, brushTip.position + brushTip.forward * reach);
    }
}
