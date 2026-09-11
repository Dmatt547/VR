using UnityEngine;

// Goes on any appliance the energy scanner can read. Owns the diagnostic state
// machine (Idle -> Scanning -> Leak Identified -> Upgrade Applied) along with the
// status light and audio that react to it. Both are side effects of
// HandleStateChangedEvent, so the appliance decides how it looks and sounds and
// the tool never touches the light or the audio source directly.
[RequireComponent(typeof(AudioSource))]
public class ScannableAppliance : MonoBehaviour
{
    [Header("Appliance")]
    [SerializeField] private string applianceName = "Appliance";
    [SerializeField] private float powerDrawWatts = 45f;

    public string ApplianceName => applianceName;

    // Source: Week 6 lecture slides, State Machines, and the Week 6 workshop project
    // (Fish.cs, FishState) - an enum of named states
    public enum ApplianceState { Idle, Scanning, LeakIdentified, UpgradeApplied }

    private ApplianceState state = ApplianceState.Idle;

    // Source: Week 6 workshop project (Fish.cs, IsActiveSwimmer) - a read-only
    // expression-bodied property lets other scripts check the state without being
    // able to change it; only SetState does that
    public ApplianceState State => state;

    [Header("Scanning")]
    [SerializeField] private float scanDuration = 1.5f;

    private float scanTimer;

    [Header("Status Light")]
    [SerializeField] private Renderer statusLightRenderer;
    [SerializeField] private Color scanningColour = Color.yellow;
    [SerializeField] private Color leakIdentifiedColour = Color.red;
    [SerializeField] private Color upgradeAppliedColour = Color.green;
    [SerializeField] private float emissionStrength = 3f;

    // Source: Week 4 lecture slides, Periodic Functions - the frequency term f in
    // y = (A sin(f t + p)) ^ n
    private const float PulseFrequency = 6f;

    private Material statusLightMaterial;

    [Header("Audio")]
    [SerializeField] private AudioClip stateChangeClip;
    [SerializeField] private float clipVolume = 0.8f;
    [SerializeField] private float scanPitch = 1f;
    [SerializeField] private float leakPitch = 0.7f;
    [SerializeField] private float upgradePitch = 1.3f;

    private AudioSource applianceAudio;

    private void Awake()
    {
        applianceAudio = GetComponent<AudioSource>();

        if (statusLightRenderer != null)
        {
            // Source: Week 4 workshop project (ElementPulse.cs) - Renderer.material
            // returns a per-object copy, so one appliance lighting up does not tint
            // every other object sharing the same material asset
            statusLightMaterial = statusLightRenderer.material;

            // Source: Week 4 workshop project (ElementPulse.cs) - emission has to be
            // enabled on the material or SetColor("_EmissionColor") has no visible effect
            statusLightMaterial.EnableKeyword("_EMISSION");
            statusLightMaterial.SetColor("_EmissionColor", Color.black);
        }
    }

    // Called by EnergyScannerTool. Returns true if a scan actually started, so
    // the tool can tell "started scanning" apart from "already diagnosed".
    public bool Scan()
    {
        // Source: Week 6 workshop project (Fish.cs) - each public entry point switches
        // on the current state, so the object itself owns the rule for when it may change
        if (state != ApplianceState.Idle) return false;

        SetState(ApplianceState.Scanning);
        return true;
    }

    // Called by ConfigLever once it has been pulled past its threshold. Only
    // acts if a leak has been identified first, so the appliance keeps the rule
    // for when it may advance and the lever only asks.
    public void ApplyUpgrade()
    {
        if (state != ApplianceState.LeakIdentified) return;

        SetState(ApplianceState.UpgradeApplied);
    }

    // Called by SessionManager when the player exits back to the start screen.
    // Clears the light and any half finished scan as well as the state itself,
    // so a reset partway through a scan does not leave the light pulsing.
    public void ResetAppliance()
    {
        state = ApplianceState.Idle;
        scanTimer = 0f;
        SetStatusLightColour(Color.black);
    }

    // Source: Week 6 workshop project (Fish.cs, SetState) - the only place the state
    // changes, with an equality guard so entry behaviour can never run twice
    private void SetState(ApplianceState newState)
    {
        if (state == newState) return;

        state = newState;
        HandleStateChangedEvent(newState);
    }

    // Source: Week 6 workshop project (Fish.cs, HandleStateChangedEvent) - switch on
    // the new state and run its entry actions once, rather than testing every frame
    private void HandleStateChangedEvent(ApplianceState newState)
    {
        switch (newState)
        {
            case ApplianceState.Idle:
                scanTimer = 0f;
                SetStatusLightColour(Color.black);
                break;

            case ApplianceState.Scanning:
                scanTimer = scanDuration;
                Debug.Log(applianceName + ": scanning...");
                PlayClip(scanPitch);
                break;

            case ApplianceState.LeakIdentified:
                Debug.Log(applianceName + " scanned: " + powerDrawWatts + "W");
                SetStatusLightColour(leakIdentifiedColour);
                PlayClip(leakPitch);
                break;

            case ApplianceState.UpgradeApplied:
                Debug.Log(applianceName + ": upgrade applied.");
                SetStatusLightColour(upgradeAppliedColour);
                PlayClip(upgradePitch);
                break;
        }
    }

    private void Update()
    {
        if (state != ApplianceState.Scanning) return;

        // Source: Week 5 lecture slides, Frame-Rate Independent Movement - Time.deltaTime
        // converts a per-frame value into a per-second one, so the scan lasts the same
        // real time regardless of frame rate
        scanTimer -= Time.deltaTime;
        PulseStatusLight();

        if (scanTimer <= 0f)
        {
            SetState(ApplianceState.LeakIdentified);
        }
    }

    private void PulseStatusLight()
    {
        if (statusLightMaterial == null) return;

        // Source: Week 4 lecture slides, Periodic Functions, as implemented in the Week 4
        // workshop project (ElementPulse.cs) - Mathf.Sin driven by Time.time, remapped from
        // -1..1 into 0..1 because emission brightness cannot go negative
        float wave = Mathf.Sin(Time.time * PulseFrequency);
        float brightness = (wave + 1f) * 0.5f;

        statusLightMaterial.SetColor("_EmissionColor", scanningColour * brightness * emissionStrength);
    }

    private void SetStatusLightColour(Color colour)
    {
        if (statusLightMaterial == null) return;

        // Source: Week 4 workshop project (ElementPulse.cs) - emission colour set by
        // multiplying a base colour by a strength value
        statusLightMaterial.SetColor("_EmissionColor", colour * emissionStrength);
    }

    private void PlayClip(float pitch)
    {
        if (stateChangeClip == null) return;

        // Source: Unity Scripting API (AudioSource.pitch) - not used in any weekly
        // project; pitch-shifting one clip avoids needing three separate audio assets
        applianceAudio.pitch = pitch;

        // Source: Week 4 workshop project (ElementSphere.cs) - PlayOneShot so overlapping
        // plays do not cut each other off
        applianceAudio.PlayOneShot(stateChangeClip, clipVolume);
    }
}
