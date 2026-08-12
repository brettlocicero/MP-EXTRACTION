using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour, IDropHandler
{
    public InventoryItemUI currentItem;

    public void OnDrop(PointerEventData eventData)
    {
        if (!eventData.pointerDrag.TryGetComponent<InventoryItemUI>(out var droppedItem))
            return;

        if (droppedItem.transform.parent.TryGetComponent<ItemSlot>(out var sourceSlot))
            sourceSlot.currentItem = null;

        droppedItem.transform.SetParent(transform);
        droppedItem.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;

        droppedItem.Dropped = true;
        currentItem = droppedItem;
    }
}