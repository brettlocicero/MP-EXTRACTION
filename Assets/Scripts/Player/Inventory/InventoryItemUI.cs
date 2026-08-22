using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image icon;
    
    RectTransform rectTransform;
    InventoryUI inventoryUI;
    Vector2 dragStartPosition;
    CanvasGroup canvasGroup;

    public InventoryItem Item { get; private set; }
    public Vector2 AnchoredPosition => rectTransform.anchoredPosition;
    public Vector2 DragOffset { get; private set; }
    public bool Dropped { get; set; }
    public ItemSlot ItemSlot { get; set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Initialize(InventoryItem item, InventoryUI inventoryUI, GridLayoutGroup layout)
    {
        this.inventoryUI = inventoryUI;
        Initialize(item, layout);
    }

    public void Initialize(InventoryItem item, GridLayoutGroup layout)
    {
        Item = item;

        if (icon != null)
            icon.sprite = item.Data.icon;

        Refresh(layout);
    }

    public void Refresh(GridLayoutGroup layout)
    {
        float stepX = layout.cellSize.x + layout.spacing.x;
        float stepY = layout.cellSize.y + layout.spacing.y;

        // Position (accounts for padding and spacing)
        rectTransform.anchoredPosition = new Vector2(
            layout.padding.left + (Item.Position.x * stepX),
            -(layout.padding.top + (Item.Position.y * stepY))
        );

        // Size (accounts for spacing between occupied cells)
        rectTransform.sizeDelta = new Vector2(
            Item.Size.x * layout.cellSize.x +
            (Item.Size.x - 1) * layout.spacing.x,

            Item.Size.y * layout.cellSize.y +
            (Item.Size.y - 1) * layout.spacing.y
        );
    }

    public void SetAnchoredPosition(Vector2 position)
    {
        rectTransform.anchoredPosition = position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.SetParent(inventoryUI.ItemParent);
        transform.SetAsLastSibling();

        dragStartPosition = rectTransform.anchoredPosition;
        DragOffset = inventoryUI.GetDragOffset(this, eventData);

        canvasGroup.blocksRaycasts = false;
        Dropped = false;

        if (ItemSlot) 
            ItemSlot.ResetSlot();
    }

    public void OnDrag(PointerEventData eventData)
    {
        inventoryUI.DragItem(this, eventData, DragOffset);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!Dropped)
        {
            if (ItemSlot != null)
                ItemSlot.ReAcceptItem(this);
            else
                rectTransform.anchoredPosition = dragStartPosition;
        }

        canvasGroup.blocksRaycasts = true;
    }

    public void Untrack()
    {
        inventoryUI.UntrackItem(Item);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        inventoryUI.ItemInfoPanel.DisplayItemPanel(Item.Data);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inventoryUI.ItemInfoPanel.HideItemPanel();
    }
}
