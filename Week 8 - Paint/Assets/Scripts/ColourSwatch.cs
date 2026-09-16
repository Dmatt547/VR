using UnityEngine;

// On each palette cube. The configuration interface: the brush reads this when its tip touches it.
public class ColourSwatch : MonoBehaviour
{
    public Color colour = Color.red;

    public Color Colour => colour;

    void Start()
    {
        GetComponent<Renderer>().material.color = colour;
    }
}
