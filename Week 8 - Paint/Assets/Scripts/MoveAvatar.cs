using Fusion;
using UnityEngine;

// On the Avatar prefab root. Follows the XR Rig head; the Network Transform replicates it.
public class MoveAvatar : NetworkBehaviour
{
    public float heightOffset = -0.3f;

    private Transform headset;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
            return;

        headset = Camera.main.transform;

        // we are inside our own avatar, so hide it locally only
        foreach (Renderer bodyPart in GetComponentsInChildren<Renderer>())
        {
            bodyPart.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (headset == null)
            return;

        if (!Object.HasInputAuthority)
            return;

        transform.position = headset.position + new Vector3(0f, heightOffset, 0f);
        transform.rotation = Quaternion.Euler(0f, headset.eulerAngles.y, 0f);
    }
}
