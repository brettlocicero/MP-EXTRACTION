using Unity.Netcode;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [SerializeField] ItemSO[] items;
    [SerializeField] LootItem itemDropPrefab;

    public void Awake()
    {
        Instance = this;
    }

    public ItemSO GetItem(int itemId)
    {
        foreach (ItemSO item in items)
        {
            if (item.id.Equals(itemId))
            {
                return item;
            }
        }

        Debug.LogError($"Item ID {itemId} not found in the database!");
        return null;
    }

    public LootItem SpawnItemDrop(ItemSO item, Vector3 position)
    {
        LootItem drop = Instantiate(itemDropPrefab, position, Quaternion.identity);
        drop.SetItem(item);
        drop.GetComponent<NetworkObject>().Spawn();

        return drop;
    }
}