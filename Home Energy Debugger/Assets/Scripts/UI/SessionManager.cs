using UnityEngine;

// Owns the start screen and the session it gates. The briefing panel is the
// only thing showing when the scene loads, the inspection tools stay switched
// off until Start Inspection is pressed, and pressing Return To Briefing puts
// everything back as it was on load. Same discipline the appliance state
// machine uses: one method changes the session state and everything else here
// is a side effect of that change.
public class SessionManager : MonoBehaviour
{
    [Header("Panel States")]
    [SerializeField] private GameObject briefingRoot;
    [SerializeField] private GameObject activeBarRoot;

    [Header("Gated During Briefing")]
    // the energy scanner above all: without it nothing can be scanned and the
    // lever has nothing to upgrade, which makes it a single sufficient gate
    [SerializeField] private GameObject[] sessionObjects;
    [SerializeField] private GameObject locomotionRoot;

    [Header("Reset Targets")]
    [SerializeField] private ScannableAppliance[] appliances;
    [SerializeField] private ConfigLever[] levers;
    [SerializeField] private ScannerDock[] docks;
    // objects whose world pose is restored on reset, so the scanner returns to
    // the bench instead of staying wherever it was dropped
    [SerializeField] private Transform[] poseResetObjects;

    private Vector3[] startPositions;
    private Quaternion[] startRotations;

    private void Awake()
    {
        CachePoses();

        // the briefing is the load state, so the scene always opens on it no
        // matter which panel was left visible while editing
        SetSessionRunning(false);
    }

    private void CachePoses()
    {
        if (poseResetObjects == null) return;

        startPositions = new Vector3[poseResetObjects.Length];
        startRotations = new Quaternion[poseResetObjects.Length];

        for (int i = 0; i < poseResetObjects.Length; i++)
        {
            if (poseResetObjects[i] == null) continue;

            // Source: Week 3 lecture slides, Transforms (slide 32) - transform.position is
            // world space, captured before anything moves so the reset restores the
            // authored layout rather than a position relative to a parent
            startPositions[i] = poseResetObjects[i].position;
            startRotations[i] = poseResetObjects[i].rotation;
        }
    }

    // hooked to the Start Inspection button's OnClick in the Inspector
    public void StartSession()
    {
        SetSessionRunning(true);
        Debug.Log("Session: inspection started.");
    }

    // hooked to the Return To Briefing button's OnClick in the Inspector
    public void ReturnToBriefing()
    {
        SetSessionRunning(false);
        ResetSession();
        Debug.Log("Session: returned to briefing, all appliances reset.");
    }

    // Source: Week 6 lecture slides, State Machines, and the Week 6 workshop project
    // (Fish.cs, SetState) - the same single gate idea as ScannableAppliance.SetState, so
    // the panel, the tools and locomotion can never disagree about the session state
    private void SetSessionRunning(bool running)
    {
        // Source: Unity Scripting API (GameObject.SetActive) - not used in any weekly
        // project; switching whole objects off removes them and their components from the
        // scene, which makes the scanner genuinely absent rather than merely ignored
        if (briefingRoot != null) briefingRoot.SetActive(!running);
        if (activeBarRoot != null) activeBarRoot.SetActive(running);
        if (locomotionRoot != null) locomotionRoot.SetActive(running);

        if (sessionObjects == null) return;

        foreach (GameObject sessionObject in sessionObjects)
        {
            if (sessionObject != null) sessionObject.SetActive(running);
        }
    }

    // each object is asked to reset itself rather than having its fields
    // written from here, so it keeps the rule for what returning to the start
    // actually means for it
    private void ResetSession()
    {
        if (appliances != null)
        {
            foreach (ScannableAppliance appliance in appliances)
            {
                if (appliance != null) appliance.ResetAppliance();
            }
        }

        if (levers != null)
        {
            foreach (ConfigLever lever in levers)
            {
                if (lever != null) lever.ResetLever();
            }
        }

        // the docks are cleared before the poses are restored, or the dock keeps
        // re-snapping the scanner to the cradle and the pose reset silently fails
        if (docks != null)
        {
            foreach (ScannerDock dock in docks)
            {
                if (dock != null) dock.ResetDock();
            }
        }

        RestorePoses();
    }

    // runs after SetSessionRunning(false) has deactivated the tool, which makes
    // XR Interaction Toolkit cancel any grab on it first, so the pose is
    // restored on an object nobody is holding
    private void RestorePoses()
    {
        if (poseResetObjects == null || startPositions == null) return;

        for (int i = 0; i < poseResetObjects.Length; i++)
        {
            if (poseResetObjects[i] == null) continue;

            Rigidbody body = poseResetObjects[i].GetComponent<Rigidbody>();

            if (body != null)
            {
                // Source: Week 2 workshop project (Bead.cs, FreezeInPlace) - clearing both
                // velocities stops the object drifting the instant it is put back
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            // Source: Unity Scripting API (Transform.SetPositionAndRotation) - not used in
            // any weekly project
            poseResetObjects[i].SetPositionAndRotation(startPositions[i], startRotations[i]);
        }
    }
}
