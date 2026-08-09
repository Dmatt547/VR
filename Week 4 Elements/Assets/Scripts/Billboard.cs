using UnityEngine;

// goes on the text label above each element sphere
// world space text only reads properly when it's facing you, so this turns the
// label towards the headset every frame
public class Billboard : MonoBehaviour
{
    // keep the label upright by ignoring the camera's height, otherwise the
    // text tilts when you look down at the bottom shelf
    [SerializeField] private bool stayUpright = true;

    private Transform headset;

    private void Start()
    {
        // the main camera is the head of the XR Origin rig
        if (Camera.main != null)
        {
            headset = Camera.main.transform;
        }
    }

    // LateUpdate rather than Update, so the label turns after the rig has
    // finished moving for this frame
    private void LateUpdate()
    {
        if (headset == null) return;

        Vector3 directionToHeadset = transform.position - headset.position;

        if (stayUpright)
        {
            directionToHeadset.y = 0f;
        }

        if (directionToHeadset == Vector3.zero) return;

        transform.rotation = Quaternion.LookRotation(directionToHeadset);
    }
}
