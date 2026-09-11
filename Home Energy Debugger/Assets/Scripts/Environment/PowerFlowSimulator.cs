using UnityEngine;

// Goes on the power flow indicator inside the inspection tube. Slides it back
// and forth on its own every frame with no player input, which is the
// Autonomous Simulation requirement. While any appliance is still leaking the
// flow is slow and dull, and once all of them are upgraded it speeds up and
// brightens, so the background reacts to what the player has done.
public class PowerFlowSimulator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private ScannableAppliance[] appliances;

    [Header("Flow Animation")]
    [SerializeField] private float amplitude = 0.3f;
    [SerializeField] private float normalSpeed = 1f;
    [SerializeField] private float upgradedSpeed = 3f;

    [Header("Glow")]
    [SerializeField] private Renderer flowRenderer;
    [SerializeField] private Color flowColour = Color.cyan;
    [SerializeField] private float dullStrength = 0.3f;
    [SerializeField] private float brightStrength = 4f;

    private Vector3 startLocalPosition;
    private Material flowMaterial;

    private void Awake()
    {
        // Source: Week 4 workshop project (ElementPulse.cs, startScale) - cache the
        // authored value in Awake so the wave oscillates around where the object was
        // placed rather than around zero
        startLocalPosition = transform.localPosition;

        // Source: Week 4 workshop project (ElementPulse.cs) - Renderer.material gives this
        // object its own material copy rather than editing the shared asset
        flowMaterial = flowRenderer.material;
        flowMaterial.EnableKeyword("_EMISSION");
    }

    private void Update()
    {
        // starting from the array length means an unassigned list reads as "not
        // finished" rather than silently reporting the house as fully upgraded
        bool allFixed = appliances.Length > 0;

        // Source: Week 6 workshop project (Fish.cs, IsActiveSwimmer checks) - read the
        // other object's state through its public property each frame instead of being
        // notified, so nothing has to remember to tell us
        foreach (ScannableAppliance appliance in appliances)
        {
            if (appliance.State != ScannableAppliance.ApplianceState.UpgradeApplied)
            {
                allFixed = false;
            }
        }

        float speed = normalSpeed;
        float strength = dullStrength;

        if (allFixed)
        {
            speed = upgradedSpeed;
            strength = brightStrength;
        }

        // Source: Week 4 lecture slides, Periodic Functions, as implemented in the Week 4
        // workshop project (ElementPulse.cs) - the same Mathf.Sin wave driven by Time.time,
        // applied to a position offset here instead of an emission colour
        float offset = Mathf.Sin(Time.time * speed) * amplitude;

        Vector3 newPosition = startLocalPosition;
        newPosition.z = startLocalPosition.z + offset;
        transform.localPosition = newPosition;

        // Source: Week 4 workshop project (ElementPulse.cs) - emission colour scaled by a
        // strength value to brighten or dim the object
        flowMaterial.SetColor("_EmissionColor", flowColour * strength);
    }
}
