using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;
    
    [SerializeField] ItemSO[] items;

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
}
