using Steamworks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] string playerName = "Player";

    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>(
        "Player", 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        PlayerName.OnValueChanged += OnPlayerNameNetworkChanged;
        InstancedUIManager.Instance.SpawnPlayerNameplate(this);

        if (IsOwner)
        {
            string resolvedName = GetSteamOrFallbackName();
            SetPlayerNameServerRpc(resolvedName);
            GameManager.Instance.RegisterPlayer(playerController);
        }

        playerName = PlayerName.Value.ToString();
    }

    public override void OnNetworkDespawn()
    {
        PlayerName.OnValueChanged -= OnPlayerNameNetworkChanged;
    }

    void OnPlayerNameNetworkChanged(FixedString64Bytes oldName, FixedString64Bytes newName)
    {
        playerName = newName.ToString();
    }

    string GetSteamOrFallbackName()
    {
        if (SteamClient.IsValid)
        {
            return SteamClient.Name;
        }

        return $"Player #{OwnerClientId}";
    }

    [ServerRpc]
    void SetPlayerNameServerRpc(string nameToSet)
    {
        PlayerName.Value = nameToSet;
        playerName = nameToSet;
    }
}