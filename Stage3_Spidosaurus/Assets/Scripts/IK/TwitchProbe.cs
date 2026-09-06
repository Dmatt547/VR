using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Diagnostic only. Measures per-frame movement at each stage of the pipeline so the source of
/// any jitter can be identified rather than guessed at.
///
/// Runs after the solver, so it observes the finished frame. Every reportInterval seconds it
/// prints the LARGEST single-frame change it saw in each quantity. Read it like this:
///
///   target moves ~0, body moves ~0, but TIP moves      -> the solver is not settling
///   target moves ~0, but BODY position or rotation move -> BodyGrounder or BodyDriver
///   TARGET moves                                        -> GaitStub is stepping or re-planning
///
/// Delete this component before submitting, or list it in the appendix as debug tooling.
/// </summary>
[DefaultExecutionOrder(300)]
public class TwitchProbe : MonoBehaviour
{
    public Transform body;
    public Transform hipBone;
    public List<FootEffector> feet = new List<FootEffector>();
    public float reportInterval = 1f;

    Vector3    lastBodyPos;
    Quaternion lastBodyRot, lastHipRot;
    Vector3[]  lastTarget, lastTip;
    bool       primed;

    float t;
    float mBodyPos, mBodyRot, mHipRot;
    float[] mTarget, mTip;

    void Start()
    {
        lastTarget = new Vector3[feet.Count];
        lastTip    = new Vector3[feet.Count];
        mTarget    = new float[feet.Count];
        mTip       = new float[feet.Count];
    }

    void LateUpdate()
    {
        if (body == null) return;

        if (primed)
        {
            mBodyPos = Mathf.Max(mBodyPos, Vector3.Distance(body.position, lastBodyPos) * 1000f);
            mBodyRot = Mathf.Max(mBodyRot, Quaternion.Angle(body.rotation, lastBodyRot));
            if (hipBone != null)
                mHipRot = Mathf.Max(mHipRot, Quaternion.Angle(hipBone.rotation, lastHipRot));

            for (int i = 0; i < feet.Count; i++)
            {
                var f = feet[i];
                if (f == null || f.target == null || f.tipBone == null) continue;
                mTarget[i] = Mathf.Max(mTarget[i], Vector3.Distance(f.target.position, lastTarget[i]) * 1000f);
                mTip[i]    = Mathf.Max(mTip[i],    Vector3.Distance(f.tipBone.position, lastTip[i])   * 1000f);
            }
        }

        lastBodyPos = body.position;
        lastBodyRot = body.rotation;
        if (hipBone != null) lastHipRot = hipBone.rotation;
        for (int i = 0; i < feet.Count; i++)
        {
            var f = feet[i];
            if (f == null || f.target == null || f.tipBone == null) continue;
            lastTarget[i] = f.target.position;
            lastTip[i]    = f.tipBone.position;
        }
        primed = true;

        t += Time.deltaTime;
        if (t < reportInterval) return;
        t = 0f;

        var sb = new StringBuilder();
        sb.Append($"[Probe] max move per frame over {reportInterval:F0}s -> ");
        sb.Append($"body pos {mBodyPos:F1} mm | body rot {mBodyRot:F2} deg | hip rot {mHipRot:F2} deg");
        for (int i = 0; i < feet.Count; i++)
        {
            if (feet[i] == null) continue;
            sb.Append($" || {feet[i].name}: target {mTarget[i]:F1} mm, tip {mTip[i]:F1} mm" +
                      $", planted {feet[i].isPlanted}");
        }
        Debug.Log(sb.ToString());

        mBodyPos = mBodyRot = mHipRot = 0f;
        for (int i = 0; i < feet.Count; i++) { mTarget[i] = 0f; mTip[i] = 0f; }
    }
}
