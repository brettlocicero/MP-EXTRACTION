using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] Image icon;
    
    bool dropped = false;
    RectTransform rectTransform;
    InventoryUI inventoryUI;
    GridLayoutGroup layout;
    Vector2 dragStartPosition;
    CanvasGroup canvasGroup;

    public InventoryItem Item { get; private set; }
    public Vector2 AnchoredPosition => rectTransform.anchoredPosition;
    public Vector2 DragOffset { get; private set; }
    public bool Dropped { get; set; }

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
        this.layout = layout;

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
    }

    public void OnDrag(PointerEventData eventData)
    {
        inventoryUI.DragItem(this, eventData, DragOffset);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!inventoryUI.DropItem(this, eventData) && !Dropped)
        {
            rectTransform.anchoredPosition = dragStartPosition;
        }

        canvasGroup.blocksRaycasts = true;
    }
}
