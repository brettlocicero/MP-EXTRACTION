using UnityEngine;

public class PlayerItems : MonoBehaviour
{
    public SoulShardSO testSoulShard;
    ItemObject[] instancedItemObjects;

    public ItemInstance EquippedItem { get; private set; }

    void Start()
    {
        InitItemObjects();
    }

    void InitItemObjects()
    {
        instancedItemObjects = GetComponentsInChildren<ItemObject>(true);
    }

    public void EquipItem(InventoryItem inventoryItem)
    {
        foreach (ItemObject itemObj in instancedItemObjects)
        {
            bool isMatch = itemObj.ItemData.id == inventoryItem.Data.id;

            if (isMatch)
                itemObj.AssignInstance(inventoryItem.Instance);

            itemObj.gameObject.SetActive(isMatch);
        }
    }

    void Update()
    {
        bool pressedOne = InputManager.Actions.Player.Alpha1.WasPressedThisFrame();
        if (pressedOne)
        {
            InventoryItem item = InventoryManager.Instance.GetWeaponInventoryItemFromSlot(0);
            if (item != null)
            {
                EquipItem(item);
                EquippedItem = item.Instance;
                item.Instance.AddSoulShard(testSoulShard);
            }
        }

        bool pressedTwo = InputManager.Actions.Player.Alpha2.WasPressedThisFrame();
        if (pressedTwo)
        {
            InventoryItem item = InventoryManager.Instance.GetWeaponInventoryItemFromSlot(1);
            if (item != null)
            {
                EquipItem(item);
                EquippedItem = item.Instance;
            }
        }
    }
}