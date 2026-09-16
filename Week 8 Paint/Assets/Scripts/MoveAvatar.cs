using Fusion;
using UnityEngine;

// Goes on the root of the Avatar prefab, next to its Network Object and
// Network Transform.
// The avatar is the body other players see. On this machine it copies the XR
// Rig's head each frame; the Network Transform then replicates that position
// out to everyone else, so their copy of this avatar moves with our headset.
public class MoveAvatar : NetworkBehaviour
{
    // the head is roughly at eye height, the capsule body hangs below it
    [SerializeField] private float heightOffset = -0.3f;

    private Transform headset;

    // worked out once, in Spawned. Object is only valid between spawn and
    // despawn, so reading it every frame throws the moment the session shuts
    // down, or on any copy of this script that never got spawned
    private bool isMine;

    // Spawned rather than Start, so this only runs once Fusion has finished
    // networking the object and HasInputAuthority is actually valid
    public override void Spawned()
    {
        // every client runs this script on every avatar, so the imposters
        // representing other players simply switch themselves off here
        isMine = Object.HasInputAuthority;

        if (!isMine) return;

        // the main camera is the head of the XR Origin rig
        if (Camera.main != null)
        {
            headset = Camera.main.transform;
        }

        // our own avatar sits on our head, so locally we'd be staring at the
        // inside of it. hiding the renderers only affects this machine - the
        // transform still replicates, so everyone else sees us normally
        foreach (Renderer bodyPart in GetComponentsInChildren<Renderer>())
        {
            bodyPart.enabled = false;
        }
    }

    // LateUpdate, so the avatar follows after the rig has finished moving for
    // this frame
    private void LateUpdate()
    {
        if (!isMine || headset == null) return;

        transform.position = headset.position + new Vector3(0f, heightOffset, 0f);

        // only the heading matters - copying the full head rotation would tip
        // the body over every time the player looks down
        transform.rotation = Quaternion.Euler(0f, headset.eulerAngles.y, 0f);
    }
}
