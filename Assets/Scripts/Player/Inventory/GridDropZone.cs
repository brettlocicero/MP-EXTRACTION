using UnityEngine;
using UnityEngine.EventSystems;

public class GridDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] InventoryUI inventoryUI;

    public void OnDrop(PointerEventData eventData)
    {
        if (!eventData.pointerDrag.TryGetComponent<InventoryItemUI>(out var droppedItem))
            return;

        inventoryUI.DropOnGrid(droppedItem, eventData);
    }
}