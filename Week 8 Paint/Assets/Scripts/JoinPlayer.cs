using Fusion;
using UnityEngine;

// Goes on the Prototype Runner object in the scene.
// Fusion calls PlayerJoined on every client each time anyone joins the session,
// so without the LocalPlayer check each client would spawn an avatar for every
// player and we'd end up with players x players avatars instead of one each.
public class JoinPlayer : SimulationBehaviour, IPlayerJoined
{
    [SerializeField] private GameObject avatarPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        Debug.Log($"Player {player} joined the session.");

        // only spawn the avatar that belongs to this machine
        if (player != Runner.LocalPlayer) return;

        // inputAuthority hands control of this avatar to the player who spawned
        // it, which is what MoveAvatar's authority check reads later
        Runner.Spawn(avatarPrefab, inputAuthority: player);
    }
}
