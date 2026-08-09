using UnityEngine;

public class InventoryItem
{
    public ItemSO Data { get; }
    public Vector2Int Position { get; set; }
    public Vector2Int Size => Data.itemSize;

    public InventoryItem(ItemSO data)
    {
        Data = data;
    }
}