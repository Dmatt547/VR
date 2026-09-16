using Fusion;
using UnityEngine;

// On the Prototype Runner. Spawns one avatar per player.
public class JoinPlayer : SimulationBehaviour, IPlayerJoined
{
    public GameObject avatarPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        Debug.Log($"Player {player} joined the game.");

        if (player == Runner.LocalPlayer)
        {
            Runner.Spawn(avatarPrefab, inputAuthority: player);
        }
    }
}
