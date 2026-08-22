using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/Items/ItemSO")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public int id;
    [TextArea] public string description;
    public Vector2Int itemSize;
    public Sprite icon;

    public virtual ItemInstance CreateInstance()
    {
        ItemInstance instance = new ItemInstance(id)
        {
            customName = itemName
        };

        return instance;
    }
}
