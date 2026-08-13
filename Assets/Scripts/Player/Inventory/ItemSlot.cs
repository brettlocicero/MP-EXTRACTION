using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour, IDropHandler
{
    public InventoryItemUI currentItem;

    public void OnDrop(PointerEventData eventData)
    {
        if (currentItem != null) return;

        if (!eventData.pointerDrag.TryGetComponent<InventoryItemUI>(out var droppedItem))
            return;

        if (InventoryManager.Instance.ContainsItem(droppedItem.Item))
        {
            droppedItem.Untrack();
            InventoryManager.Instance.RemoveItem(droppedItem.Item);
        }

        if (droppedItem.transform.parent.TryGetComponent<ItemSlot>(out var sourceSlot))
            sourceSlot.currentItem = null;

        droppedItem.transform.SetParent(transform);
        droppedItem.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;

        droppedItem.Dropped = true;
        droppedItem.ItemSlot = this;
        currentItem = droppedItem;
    }

    public void ResetSlot()
    {
        currentItem = null;
    }

    public void ReAcceptItem(InventoryItemUI item)
    {
        item.transform.SetParent(transform);
        item.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
        item.Dropped = true;
        item.ItemSlot = this;
        currentItem = item;
    }
}