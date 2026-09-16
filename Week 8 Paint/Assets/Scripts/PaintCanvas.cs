using Fusion;
using UnityEngine;

// Goes on the Picture cube, next to its Network Object.
// Builds the paint-by-numbers picture as a texture at runtime, and replicates
// colour changes to every other client.
//
// The picture is stored as two pieces of data rather than as pixels:
//   regionOfPixel - which numbered region each pixel belongs to (never changes)
//   regionColours - the colour currently painted into each region
// Repainting is then one entry in regionColours, not a search through 65,000
// pixels, and the whole picture only ever needs the region number sent over the
// network instead of an image.
public class PaintCanvas : NetworkBehaviour
{
    [SerializeField] private int width = 256;
    [SerializeField] private int height = 256;

    // three regions, which is the "count the number of colours in the picture"
    // part of the task. the starting shades are deliberately dull, so painting
    // over them is obvious
    [SerializeField]
    private Color[] regionColours =
    {
        new Color(0.85f, 0.85f, 0.85f),
        new Color(0.65f, 0.65f, 0.65f),
        new Color(0.45f, 0.45f, 0.45f)
    };

    private Texture2D picture;
    private int[] regionOfPixel;

    public int RegionCount => regionColours.Length;

    // Spawned rather than Start, so the texture exists before any RPC can
    // arrive from a client that is already painting
    public override void Spawned()
    {
        picture = new Texture2D(width, height, TextureFormat.ARGB32, false);

        // point filtering keeps hard edges between regions instead of blurring
        // them into each other
        picture.filterMode = FilterMode.Point;

        BuildRegions();
        Redraw();

        GetComponent<MeshRenderer>().material.mainTexture = picture;

        Debug.Log($"Picture built with {RegionCount} colours.");
    }

    // concentric rings, so the picture has a clear centre, middle and edge to
    // paint. distance is normalised to 0-1 first, so the pattern is the same
    // whatever resolution the texture is
    private void BuildRegions()
    {
        regionOfPixel = new int[width * height];

        Vector2 centre = new Vector2(width * 0.5f, height * 0.5f);
        float longestDistance = centre.magnitude;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), centre) / longestDistance;

                // Min guards the corners, where distance reaches exactly 1 and
                // would otherwise index one past the end of the array
                int region = Mathf.Min((int)(distance * RegionCount), RegionCount - 1);

                regionOfPixel[x + y * width] = region;
            }
        }
    }

    // pixel colours only live on the GPU once Apply is called, so every repaint
    // has to finish with it
    private void Redraw()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                picture.SetPixel(x, y, regionColours[regionOfPixel[x + y * width]]);
            }
        }

        picture.Apply();
    }

    // called by the brush with the texture coordinate it hit
    public void PaintAt(Vector2 textureCoordinate, Color colour)
    {
        if (regionOfPixel == null) return;

        int x = Mathf.Clamp((int)(textureCoordinate.x * width), 0, width - 1);
        int y = Mathf.Clamp((int)(textureCoordinate.y * height), 0, height - 1);

        int region = regionOfPixel[x + y * width];

        // already this colour, so there is nothing worth sending
        if (regionColours[region] == colour) return;

        RpcPaintRegion(region, colour);
    }

    // the colour change is a one-off event rather than a value that needs
    // syncing every tick, so it goes out as an RPC. RpcTargets.All means the
    // painter's own copy is repainted by the same call as everyone else's,
    // so every client runs identical code and cannot drift apart
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcPaintRegion(int region, Color colour)
    {
        if (region < 0 || region >= RegionCount) return;

        regionColours[region] = colour;
        Redraw();
    }
}
