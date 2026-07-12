using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/Items/ItemSO")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
}
