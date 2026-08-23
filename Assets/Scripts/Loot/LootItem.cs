using Unity.Netcode;
using UnityEngine;

public class LootItem : NetworkBehaviour, IInteractable
{
    [SerializeField] ItemSO item;
    [SerializeField] bool isFactory;

    ItemInstance instance;
    bool collected;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (!isFactory)
            Init();
    }

    public void Init(ItemInstance existingInstance = null)
    {
        instance = existingInstance ?? item.CreateInstance();
    }

    public void Interact()
    {
        CollectRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void CollectRpc(RpcParams rpcParams = default)
    {
        if (!isFactory && collected) return;

        if (!isFactory)
            collected = true;

        ItemInstance grantedInstance = isFactory ? item.CreateInstance() : instance;

        ulong collectorId = rpcParams.Receive.SenderClientId;
        GrantItemRpc(grantedInstance, RpcTarget.Single(collectorId, RpcTargetUse.Temp));

        if (!isFactory)
            NetworkObject.Despawn();
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void GrantItemRpc(ItemInstance itemInstance, RpcParams rpcParams)
    {
        InventoryManager.Instance.AddItem(itemInstance);
    }
}