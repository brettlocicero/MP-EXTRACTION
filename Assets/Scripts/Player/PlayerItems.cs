using UnityEngine;

public class PlayerItems : MonoBehaviour
{
    ItemObject[] instancedItemObjects;

    void Start()
    {
        InitItemObjects();
    }

    void InitItemObjects()
    {
        instancedItemObjects = GetComponentsInChildren<ItemObject>(true);
    }

    public void EquipItem(ItemSO item)
    {
        foreach (ItemObject itemObj in instancedItemObjects)
        {
            itemObj.gameObject.SetActive(itemObj.ItemData == item);
        }
    }

    void Update()
    {
        bool pressedOne = InputManager.Actions.Player.Alpha1.WasPressedThisFrame();
        if (pressedOne)
        {
            ItemSO item = InventoryManager.Instance.GetWeaponItemFromSlot(0);
            if (item)
                EquipItem(item);
        }

        bool pressedTwo = InputManager.Actions.Player.Alpha2.WasPressedThisFrame();
        if (pressedTwo)
        {
            ItemSO item = InventoryManager.Instance.GetWeaponItemFromSlot(1);
            if (item)
                EquipItem(item);
        }
    }
}
