using UnityEngine;

// On the Brush. One ray from the tip serves both jobs: a swatch loads a colour,
// the picture takes one. RaycastHit.textureCoord needs the picture to have a Mesh Collider.
public class PaintBrush : MonoBehaviour
{
    public Transform brushTip;
    public Renderer brushHeadRenderer;
    public Color brushColour = Color.red;
    public float reach = 0.05f;

    void Start()
    {
        ShowBrushColour();
    }

    void Update()
    {
        if (brushTip == null)
            return;

        if (!Physics.Raycast(brushTip.position, brushTip.forward, out RaycastHit hit, reach))
            return;

        ColourSwatch swatch = hit.collider.GetComponent<ColourSwatch>();

        if (swatch != null)
        {
            brushColour = swatch.Colour;
            ShowBrushColour();
            return;
        }

        PaintCanvas canvas = hit.collider.GetComponent<PaintCanvas>();

        if (canvas == null)
            return;

        canvas.PaintAt(hit.textureCoord, brushColour);
    }

    private void ShowBrushColour()
    {
        if (brushHeadRenderer == null)
            return;

        brushHeadRenderer.material.color = brushColour;
    }

    // shows the brush reach in the editor
    private void OnDrawGizmosSelected()
    {
        if (brushTip == null)
            return;

        Gizmos.color = brushColour;
        Gizmos.DrawLine(brushTip.position, brushTip.position + brushTip.forward * reach);
    }
}
