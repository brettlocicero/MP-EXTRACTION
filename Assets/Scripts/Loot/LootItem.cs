using UnityEngine;

public class LootItem : MonoBehaviour, IInteractable
{
    [SerializeField] ItemSO item;
    [SerializeField] bool isFactory;
    ItemInstance instance;
    bool collected;

    void Awake()
    {
        if (!isFactory) Init();
    }

    public void Init(ItemInstance existingInstance = null)
    {
        instance = existingInstance ?? item.CreateInstance();
    }

    public void Interact()
    {
        if (collected) return;
        ItemInstance grantedInstance = isFactory ? item.CreateInstance() : instance;
        if (!InventoryManager.Instance.AddItem(grantedInstance)) return;
        if (!isFactory)
        {
            collected = true;
            Destroy(gameObject);
        }
    }
}
