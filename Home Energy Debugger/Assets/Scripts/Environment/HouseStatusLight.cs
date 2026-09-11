using UnityEngine;

// Goes on the inspection panel light. Shows the whole house rather than one
// appliance: red while any appliance still needs fixing, green once every one
// of them has been upgraded. Same emission approach the appliances use.
public class HouseStatusLight : MonoBehaviour
{
    [SerializeField] private ScannableAppliance[] appliances;
    [SerializeField] private Renderer lightRenderer;
    [SerializeField] private float emissionStrength = 3f;

    private Material lightMaterial;

    private void Awake()
    {
        // Source: Week 4 workshop project (ElementPulse.cs) - Renderer.material for a
        // per-object copy, with emission enabled so SetColor takes visible effect
        lightMaterial = lightRenderer.material;
        lightMaterial.EnableKeyword("_EMISSION");
    }

    private void Update()
    {
        // starting from the array length means an unassigned list stays red
        // rather than reporting the house as finished
        bool allFixed = appliances.Length > 0;

        // Source: Week 6 workshop project (Fish.cs, IsActiveSwimmer checks) - read each
        // object's state through its public property every frame rather than caching it,
        // so the light turns straight back to red the moment a new leak is scanned
        foreach (ScannableAppliance appliance in appliances)
        {
            if (appliance.State != ScannableAppliance.ApplianceState.UpgradeApplied)
            {
                allFixed = false;
            }
        }

        Color colour = Color.red;

        if (allFixed)
        {
            colour = Color.green;
        }

        // Source: Week 4 workshop project (ElementPulse.cs) - emission colour scaled by a
        // strength value
        lightMaterial.SetColor("_EmissionColor", colour * emissionStrength);
    }
}
