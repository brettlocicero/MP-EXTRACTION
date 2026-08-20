using Unity.Netcode;
using UnityEngine;

public class LootItem : NetworkBehaviour, IInteractable
{
    [SerializeField] ItemSO item;

    public void Interact()
    {
        CollectRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void CollectRpc(RpcParams rpcParams = default)
    {
        ulong collectorId = rpcParams.Receive.SenderClientId;
        GrantItemRpc(RpcTarget.Single(collectorId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void GrantItemRpc(RpcParams rpcParams)
    {
        InventoryManager.Instance.AddItem(item);
    }
}