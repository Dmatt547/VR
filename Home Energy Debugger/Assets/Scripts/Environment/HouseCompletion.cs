using UnityEngine;
using TMPro;

// Goes on the inspection panel. Watches every appliance and, the moment the last
// one is upgraded, brings the house lights up and shows a completion message.
// Acts only on the change rather than every frame, the same single gate idea the
// appliance state machine uses, so the chime plays once instead of continuously.
public class HouseCompletion : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private ScannableAppliance[] appliances;

    [Header("Completion")]
    [SerializeField] private GameObject[] houseLights;
    [SerializeField] private TMP_Text completionText;

    [Header("Audio")]
    [SerializeField] private AudioSource completionAudio;
    [SerializeField] private AudioClip completionClip;

    private bool wasComplete;

    private void Start()
    {
        SetComplete(false);
    }

    private void Update()
    {
        // Source: Week 6 workshop project (Fish.cs) - read each object's state through
        // its public property every frame rather than being told about it
        bool isComplete = appliances.Length > 0;

        foreach (ScannableAppliance appliance in appliances)
        {
            if (appliance.State != ScannableAppliance.ApplianceState.UpgradeApplied)
            {
                isComplete = false;
            }
        }

        // Source: Week 6 lecture slides, State Machines - only act on the transition,
        // so entering the completed state runs its effects exactly once
        if (isComplete == wasComplete) return;

        SetComplete(isComplete);
    }

    private void SetComplete(bool complete)
    {
        wasComplete = complete;

        // Source: Unity Scripting API (GameObject.SetActive) - not used in any weekly
        // project; switching the lights off entirely is cheaper than dimming them
        foreach (GameObject houseLight in houseLights)
        {
            if (houseLight != null) houseLight.SetActive(complete);
        }

        if (completionText != null)
        {
            if (complete)
            {
                completionText.text = "HOUSE COMPLETE\nEvery appliance upgraded.";
            }
            else
            {
                completionText.text = "";
            }
        }

        if (!complete) return;

        // Source: Week 4 workshop project (ElementSphere.cs) - PlayOneShot for a
        // one-off cue that does not interrupt anything already playing
        if (completionAudio != null && completionClip != null)
        {
            completionAudio.PlayOneShot(completionClip);
        }
    }
}