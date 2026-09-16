using Fusion;
using UnityEngine;

// On the Picture cube. The picture is stored as a region number per pixel plus one
// colour per region, so painting changes one array entry and sends one int over the network.
public class PaintCanvas : NetworkBehaviour
{
    public Color[] regionColours =
    {
        new Color(0.85f, 0.85f, 0.85f),
        new Color(0.65f, 0.65f, 0.65f),
        new Color(0.45f, 0.45f, 0.45f)
    };

    private Texture2D tex;
    private int width = 512;
    private int height = 512;

    private int[] regionOfPixel;

    public int RegionCount => regionColours.Length;

    public override void Spawned()
    {
        tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Point;

        BuildRegions();
        Redraw();

        Debug.Log($"Picture built with {RegionCount} colours.");
    }

    // concentric rings, one region per ring
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

                regionOfPixel[x + y * width] = Mathf.Min((int)(distance * RegionCount), RegionCount - 1);
            }
        }
    }

    private void Redraw()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color color = regionColours[regionOfPixel[x + y * width]];
                tex.SetPixel(x, y, color);
            }
        }

        GetComponent<MeshRenderer>().material.mainTexture = tex;
        tex.Apply();
    }

    // called by the brush with the texture coordinate it hit
    public void PaintAt(Vector2 textureCoordinate, Color colour)
    {
        if (regionOfPixel == null)
            return;

        int x = Mathf.Clamp((int)(textureCoordinate.x * width), 0, width - 1);
        int y = Mathf.Clamp((int)(textureCoordinate.y * height), 0, height - 1);

        int region = regionOfPixel[x + y * width];

        if (regionColours[region] == colour)
            return;

        RpcPaintRegion(region, colour);
    }

    // a colour change is a one-off event, so it goes out as an RPC rather than a synced value
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcPaintRegion(int region, Color colour)
    {
        if (region < 0 || region >= RegionCount)
            return;

        regionColours[region] = colour;
        Redraw();
    }
}
