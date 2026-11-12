using Unity.Netcode;
using UnityEngine;

public class LootItem : NetworkBehaviour, IInteractable
{
    [SerializeField] ItemSO item;
    [SerializeField] bool isFactory;

    NetworkVariable<int> itemId = new(-1);

    ItemInstance instance;
    bool collected;

    public void SetItem(ItemSO newItem)
    {
        item = newItem;
        itemId.Value = newItem.id;
    }

    public override void OnNetworkSpawn()
    {
        // Prefab-assigned items never went through SetItem
        if (IsServer && itemId.Value == -1)
            itemId.Value = item.id;

        item = ItemDatabase.Instance.GetItem(itemId.Value);
        gameObject.name = item.itemName;

        if (IsServer && !isFactory)
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