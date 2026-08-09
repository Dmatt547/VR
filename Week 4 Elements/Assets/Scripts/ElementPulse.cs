using UnityEngine;

// goes on the element spheres that need to look alive - Flame, Ember, Spark,
// Storm and Glow.
//
// uses the periodic function from the week 4 lecture:
//
//     y = (A sin(f t + p)) ^ n
//
//   A = amplitude  - how big the pulse is
//   f = frequency  - how fast it repeats
//   p = phase      - where in the cycle it starts
//   n = sharpness  - higher powers flatten the troughs and sharpen the peaks,
//                    which is what turns a smooth glow into a sharp flicker
//
// t is Time.time, so the wave animates on its own without me having to track
// anything between frames
public class ElementPulse : MonoBehaviour
{
    [Header("Wave: (A sin(f t + p)) ^ n")]
    [SerializeField] private float amplitude = 1f;
    [SerializeField] private float frequency = 2f;
    [SerializeField] private float phase = 0f;
    [SerializeField] private int sharpness = 1;

    [Header("Scale Pulse")]
    [SerializeField] private bool pulseScale = true;
    // how much the sphere grows/shrinks, as a fraction of its normal size
    [SerializeField] private float scaleAmount = 0.05f;

    [Header("Emission Pulse")]
    [SerializeField] private bool pulseEmission = false;
    [SerializeField] private Color emissionColour = Color.white;
    [SerializeField] private float emissionStrength = 3f;

    private Vector3 startScale;
    private Material sphereMaterial;

    private void Awake()
    {
        startScale = transform.localScale;

        Renderer sphereRenderer = GetComponent<Renderer>();

        if (sphereRenderer != null)
        {
            // .material gives this object its own copy, so pulsing one sphere
            // doesn't pulse every sphere sharing the same material asset
            sphereMaterial = sphereRenderer.material;
            sphereMaterial.EnableKeyword("_EMISSION");
        }
    }

    private void Update()
    {
        float wave = CalculateWave(Time.time);

        if (pulseScale)
        {
            transform.localScale = startScale * (1f + wave * scaleAmount);
        }

        if (pulseEmission && sphereMaterial != null)
        {
            // the wave runs from -1 to 1 but brightness can't be negative, so
            // remap it into the 0 to 1 range first
            float brightness = (wave + 1f) * 0.5f;

            sphereMaterial.SetColor("_EmissionColor", emissionColour * brightness * emissionStrength);
        }
    }

    private float CalculateWave(float time)
    {
        float wave = amplitude * Mathf.Sin(frequency * time + phase);

        return Mathf.Pow(wave, sharpness);
    }
}
