using UnityEngine;

// goes on an empty child of the scalpel, with a Line Renderer on it
//
// the cutting plane is infinite and invisible, so without a preview you're guessing where
// the cut lands. this draws the same ray the scalpel uses (slide 27) and turns solid red
// the moment it's over something cuttable
[RequireComponent(typeof(LineRenderer))]
public class CutGuideLine : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private ScalpelTool scalpel;

    [Header("Colour")]
    [SerializeField] private Color idleColour = new Color(1f, 0.35f, 0.35f, 0.45f);
    [SerializeField] private Color targetColour = new Color(1f, 0.05f, 0.05f, 1f);

    [Header("Shape")]
    [SerializeField] private float lineWidth = 0.0035f;

    // two short cross strokes lying in the cutting plane, showing how the blade is rolled.
    // pointing at the object isn't enough - the roll decides which way the cut runs
    [SerializeField] private bool showPlaneHint = true;
    [SerializeField] private float planeHintSize = 0.05f;

    private LineRenderer guideLine;

    // a fresh Line Renderer defaults to a metre-wide white ribbon, which fills the Scene
    // view until Play mode starts. this clears it the moment the component is added
    private void Reset()
    {
        LineRenderer fresh = GetComponent<LineRenderer>();

        fresh.positionCount = 0;
        fresh.startWidth = lineWidth;
        fresh.endWidth = lineWidth;
        fresh.useWorldSpace = true;
    }

    private void Awake()
    {
        guideLine = GetComponent<LineRenderer>();

        if (scalpel == null) scalpel = GetComponentInParent<ScalpelTool>();
        if (scalpel == null) Debug.LogWarning("CutGuideLine can't find a ScalpelTool - assign one in the Inspector.");

        // world space, or the scalpel's own scale stretches the line
        guideLine.useWorldSpace = true;
        guideLine.startWidth = lineWidth;
        guideLine.endWidth = lineWidth;
        guideLine.numCapVertices = 2;
    }

    // LateUpdate so the line is drawn after the hand has finished moving the scalpel this
    // frame. in Update it would trail one frame behind
    private void LateUpdate()
    {
        if (scalpel == null || scalpel.BladeTip == null)
        {
            guideLine.enabled = false;
            return;
        }

        guideLine.enabled = true;

        Vector3 origin = scalpel.BladeTip.position;
        Vector3 direction = scalpel.BladeTip.forward;

        // ask the scalpel rather than running our own raycast. going through the same method
        // is what stops the preview and the actual cut ever disagreeing
        bool overTarget = scalpel.FindTarget(out float distance) != null;

        // R(t) = O + t * dr from slide 27, stopping at whatever it hits
        Vector3 end = origin + (direction * distance);

        if (showPlaneHint)
        {
            // a direction lying in the cutting plane - the cross product of two directions
            // gives a third at right angles to both
            Vector3 across = Vector3.Cross(scalpel.CutPlane.normal, direction).normalized * (planeHintSize * 0.5f);

            guideLine.positionCount = 6;
            guideLine.SetPosition(0, origin - across);
            guideLine.SetPosition(1, origin + across);
            guideLine.SetPosition(2, origin);
            guideLine.SetPosition(3, end);
            guideLine.SetPosition(4, end - across);
            guideLine.SetPosition(5, end + across);
        }
        else
        {
            guideLine.positionCount = 2;
            guideLine.SetPosition(0, origin);
            guideLine.SetPosition(1, end);
        }

        guideLine.startColor = overTarget ? targetColour : idleColour;
        guideLine.endColor = overTarget ? targetColour : idleColour;
    }
}
