using Unity.Netcode;
using UnityEngine;

public class LootItem : NetworkBehaviour, IInteractable
{
    [SerializeField] ItemSO item;
    [SerializeField] bool destroyOnPickup = true;
    
    bool collected = false;

    public void Interact()
    {
        CollectRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void CollectRpc(RpcParams rpcParams = default)
    {
        if (collected) return;

        collected = true;
        ulong collectorId = rpcParams.Receive.SenderClientId;

        GrantItemRpc(RpcTarget.Single(collectorId, RpcTargetUse.Temp));

        if (destroyOnPickup)
            GetComponent<NetworkObject>().Despawn();
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void GrantItemRpc(RpcParams rpcParams)
    {
        InventoryManager.Instance.AddItem(item);
    }
}