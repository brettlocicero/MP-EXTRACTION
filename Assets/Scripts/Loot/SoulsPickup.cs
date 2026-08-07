using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class SoulsPickup : NetworkBehaviour
{
    int soulsValue;
    bool collected = false;

    public void Init(int amount) => soulsValue = amount;

    void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<NetworkObject>(out var networkObject) || !networkObject.IsOwner) return;

        if (other.TryGetComponent<PlayerCurrency>(out var currency))
            CollectRpc(networkObject.OwnerClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void CollectRpc(ulong clientId)
    {
        if (collected) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

        if (!client.PlayerObject.TryGetComponent<PlayerCurrency>(out var currency)) return;

        collected = true;
        currency.AddSouls(soulsValue);
        GetComponent<NetworkObject>().Despawn();
    }
}