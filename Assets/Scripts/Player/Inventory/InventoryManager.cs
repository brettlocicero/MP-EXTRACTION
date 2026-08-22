using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    
    [SerializeField] InventoryUI inventoryUI;

    [Header("Inventory Size")]
    [SerializeField] int width = 8;
    [SerializeField] int height = 6;

    [Header("Item Slots")]
    [SerializeField] ItemSlot[] weaponItemSlots;
    [SerializeField] ItemSlot[] ringItemSlots;

    InventoryGrid grid;

    readonly List<InventoryItem> items = new();

    public int Width => width;
    public int Height => height;

    public InventoryGrid Grid => grid;

    public IReadOnlyList<InventoryItem> Items => items;

    public event Action<InventoryItem> OnItemAdded;
    public event Action<InventoryItem> OnItemRemoved;
    public event Action<InventoryItem> OnItemMoved;
    public event Action OnInventoryChanged;

    void Awake()
    {
        Instance = this;
        grid = new InventoryGrid(width, height);
    }

    public bool AddItem(ItemInstance instance)
    {
        ItemSO itemData = ItemDatabase.Instance.GetItem(instance.baseItemId);

        if (!grid.FindSpace(itemData.itemSize, out Vector2Int position))
            return false;

        InventoryItem item = new InventoryItem(itemData, instance);

        grid.PlaceItem(item, position);

        items.Add(item);

        OnItemAdded?.Invoke(item);
        OnInventoryChanged?.Invoke();

        return true;
    }

    public bool AddItem(ItemInstance instance, Vector2Int position)
    {
        ItemSO itemData = ItemDatabase.Instance.GetItem(instance.baseItemId);

        if (!grid.CanPlaceItem(itemData.itemSize, position))
            return false;

        InventoryItem item = new InventoryItem(itemData, instance);

        grid.PlaceItem(item, position);

        items.Add(item);

        OnItemAdded?.Invoke(item);
        OnInventoryChanged?.Invoke();

        return true;
    }

    public bool RemoveItem(InventoryItem item)
    {
        if (!items.Contains(item))
            return false;

        grid.RemoveItem(item);

        items.Remove(item);

        OnItemRemoved?.Invoke(item);
        OnInventoryChanged?.Invoke();

        return true;
    }

    public bool MoveItem(InventoryItem item, Vector2Int newPosition)
    {
        if (!items.Contains(item))
            return false;

        Vector2Int oldPosition = item.Position;

        grid.RemoveItem(item);

        if (!grid.CanPlaceItem(item.Size, newPosition))
        {
            grid.PlaceItem(item, oldPosition);
            return false;
        }

        grid.PlaceItem(item, newPosition);

        OnItemMoved?.Invoke(item);
        OnInventoryChanged?.Invoke();

        return true;
    }

    public InventoryItem GetItem(Vector2Int position)
    {
        return grid.GetItem(position);
    }

    public bool IsOccupied(Vector2Int position)
    {
        return grid.GetItem(position) != null;
    }

    public bool ContainsItem(InventoryItem item)
    {
        return items.Contains(item);
    }

    public void ClearInventory()
    {
        foreach (InventoryItem item in new List<InventoryItem>(items))
        {
            RemoveItem(item);
        }
    }

    public bool HasRoomFor(ItemSO itemData)
    {
        return grid.FindSpace(itemData.itemSize, out _);
    }

    public Vector2Int? FindFreeSpace(ItemSO itemData)
    {
        if (grid.FindSpace(itemData.itemSize, out Vector2Int pos))
            return pos;

        return null;
    }

    public void PrintInventory()
    {
        Debug.Log(grid.ToString());
    }
    
    public bool IsInventoryOpen() 
    {
        return inventoryUI.inventoryOpen;
    }

    public bool ReAddItem(InventoryItem item, Vector2Int position)
    {
        if (items.Contains(item))
            return false;

        if (!grid.CanPlaceItem(item.Size, position))
            return false;

        grid.PlaceItem(item, position);
        items.Add(item);

        OnItemAdded?.Invoke(item);
        OnInventoryChanged?.Invoke();

        return true;
    }

    public ItemSlot GetWeaponSlot(int index)
    {
        return weaponItemSlots[index];
    }

    public ItemSO GetWeaponItemFromSlot(int index)
    {
        InventoryItemUI slotItem = weaponItemSlots[index].currentItem;
        if (slotItem)
            return slotItem.Item.Data;

        return null;
    }

    public InventoryItem GetWeaponInventoryItemFromSlot(int index)
    {
        InventoryItemUI slotItem = weaponItemSlots[index].currentItem;
        if (slotItem)
            return slotItem.Item;

        return null;
    }
}