using UnityEngine;

public class InventoryItem
{
    public ItemSO Data { get; }
    public ItemInstance Instance { get; }
    public Vector2Int Position { get; set; }
    public Vector2Int Size => Data.itemSize;

    public InventoryItem(ItemSO data, ItemInstance instance)
    {
        Data = data;
        Instance = instance;
    }
}