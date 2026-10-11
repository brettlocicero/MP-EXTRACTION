using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class RoomPortal : NetworkBehaviour, IInteractable
{
    readonly HashSet<ulong> chosen = new();

    readonly NetworkVariable<bool> unlocked = new(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    public void RelicChosen(ulong clientId)
    {
        chosen.Add(clientId);
        RefreshUnlocked();
    }

    void OnClientDisconnected(ulong clientId)
    {
        RefreshUnlocked();
    }

    void RefreshUnlocked()
    {
        unlocked.Value = NetworkManager.ConnectedClientsIds.All(chosen.Contains);
    }

    public void Interact()
    {
        InteractRpc();
    }

    public void Hover(string message)
    {
        InteractionText.Instance.ShowPopup(
            unlocked.Value ? "Press 'E' to enter the next room" : "Waiting for everyone to choose a relic..."
        );
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void InteractRpc()
    {
        if (unlocked.Value)
            RegionGenerator.Instance.SpawnNextRoom();
    }
}