using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CanvasGroup inventoryCanvasGroup;
    [SerializeField] InventoryManager inventoryManager;
    [SerializeField] GridLayoutGroup gridLayout;
    [SerializeField] RectTransform itemParent;
    [SerializeField] GameObject slotPrefab;
    [SerializeField] InventoryItemUI itemPrefab;
    [SerializeField] ItemInfoPanel itemInfoPanel;

    readonly Dictionary<InventoryItem, InventoryItemUI> itemUIs = new();

    [HideInInspector] public bool inventoryOpen = false;

    public RectTransform ItemParent => itemParent;
    public ItemInfoPanel ItemInfoPanel => itemInfoPanel;

    void Start()
    {
        BuildGrid();

        foreach (InventoryItem item in inventoryManager.Items)
            HandleItemAdded(item);
    }

    void Update() 
    {
        bool tabPressed = InputManager.Actions.Player.Tab.WasPressedThisFrame();
        if (tabPressed) 
            ToggleInventory();
    }

    void OnEnable()
    {
        inventoryManager.OnItemAdded += HandleItemAdded;
        inventoryManager.OnItemRemoved += HandleItemRemoved;
        inventoryManager.OnItemMoved += RefreshItem;
    }

    void OnDisable()
    {
        inventoryManager.OnItemAdded -= HandleItemAdded;
        inventoryManager.OnItemRemoved -= HandleItemRemoved;
        inventoryManager.OnItemMoved -= RefreshItem;
    }

    void HandleItemAdded(InventoryItem item)
    {
        if (itemUIs.ContainsKey(item)) return;

        InventoryItemUI ui = Instantiate(itemPrefab, itemParent);
        ui.Initialize(item, this, gridLayout);
        itemUIs.Add(item, ui);
    }

    void HandleItemRemoved(InventoryItem item)
    {
        if (!itemUIs.TryGetValue(item, out InventoryItemUI ui)) return;

        Destroy(ui.gameObject);
        itemUIs.Remove(item);
    }

    public void TrackItem(InventoryItem item, InventoryItemUI ui)
    {
        itemUIs[item] = ui;
    }

    public void UntrackItem(InventoryItem item)
    {
        itemUIs.Remove(item);
    }

    void BuildGrid()
    {
        foreach (Transform child in gridLayout.transform)
            Destroy(child.gameObject);

        int count = inventoryManager.Width * inventoryManager.Height;

        for (int i = 0; i < count; i++)
        {
            Instantiate(slotPrefab, gridLayout.transform);
        }
    }

    public void RefreshItem(InventoryItem item)
    {
        if (!itemUIs.TryGetValue(item, out InventoryItemUI ui))
            return;

        ui.Refresh(gridLayout);
    }

    public void DragItem(InventoryItemUI itemUI, PointerEventData eventData, Vector2 dragOffset)
    {
        if (!TryGetPointerAnchoredPosition(eventData, out Vector2 pointerPosition))
            return;

        itemUI.SetAnchoredPosition(pointerPosition + dragOffset);
    }

    public bool DropItem(InventoryItemUI itemUI, PointerEventData eventData)
    {
        if (!TryGetPointerAnchoredPosition(eventData, out Vector2 pointerPosition))
            return false;

        Vector2 itemPosition = pointerPosition + itemUI.DragOffset;
        Vector2Int gridPosition = AnchoredPositionToGridPosition(itemPosition);

        if (!inventoryManager.ContainsItem(itemUI.Item))
        {
            TrackItem(itemUI.Item, itemUI);

            if (!inventoryManager.ReAddItem(itemUI.Item, gridPosition))
                return false;

            itemUI.Refresh(gridLayout);
            return true;
        }

        return inventoryManager.MoveItem(itemUI.Item, gridPosition);
    }

    public bool DropOnGrid(InventoryItemUI itemUI, PointerEventData eventData)
    {
        if (!TryGetPointerAnchoredPosition(eventData, out Vector2 pointerPosition))
            return false;

        Vector2 itemPosition = pointerPosition + itemUI.DragOffset;
        Vector2Int gridPosition = AnchoredPositionToGridPosition(itemPosition);

        bool success;

        if (!inventoryManager.ContainsItem(itemUI.Item))
        {
            TrackItem(itemUI.Item, itemUI);
            success = inventoryManager.ReAddItem(itemUI.Item, gridPosition);

            if (success)
                itemUI.Refresh(gridLayout);
        }
        else
        {
            success = inventoryManager.MoveItem(itemUI.Item, gridPosition);
        }

        if (success)
        {
            itemUI.transform.SetParent(itemParent);
            itemUI.ItemSlot = null;
            itemUI.Dropped = true;
        }

        return success;
    }

    public Vector2 GetDragOffset(InventoryItemUI itemUI, PointerEventData eventData)
    {
        if (!TryGetPointerAnchoredPosition(eventData, out Vector2 pointerPosition))
            return Vector2.zero;

        return itemUI.AnchoredPosition - pointerPosition;
    }

    bool TryGetPointerAnchoredPosition(PointerEventData eventData, out Vector2 anchoredPosition)
    {
        RectTransform parent = itemParent;
        Camera eventCamera = eventData.pressEventCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventCamera, out Vector2 localPoint))
        {
            anchoredPosition = Vector2.zero;
            return false;
        }

        Rect rect = parent.rect;
        Vector2 topLeft = new Vector2(
            -rect.width * parent.pivot.x,
            rect.height * (1f - parent.pivot.y)
        );

        anchoredPosition = localPoint - topLeft;
        return true;
    }

    Vector2Int AnchoredPositionToGridPosition(Vector2 anchoredPosition)
    {
        float stepX = gridLayout.cellSize.x + gridLayout.spacing.x;
        float stepY = gridLayout.cellSize.y + gridLayout.spacing.y;

        int x = Mathf.RoundToInt((anchoredPosition.x - gridLayout.padding.left) / stepX);
        int y = Mathf.RoundToInt((-anchoredPosition.y - gridLayout.padding.top) / stepY);

        return new Vector2Int(x, y);
    }
    
    public void ToggleInventory() 
    {
        inventoryOpen = !inventoryOpen;
        GameManager.Instance.LocalPlayer.SetInventoryCameraActive(inventoryOpen);

        if (inventoryOpen)
        {
            inventoryCanvasGroup.alpha = 1f;
            inventoryCanvasGroup.interactable = true;
            inventoryCanvasGroup.blocksRaycasts = true;

            UIPanelManager.Instance.PanelOpened(inventoryCanvasGroup.gameObject, lockMovement: false, lockSensitivity: true);
        }

        else
        {
            inventoryCanvasGroup.alpha = 0f;
            inventoryCanvasGroup.interactable = false;
            inventoryCanvasGroup.blocksRaycasts = false;

            UIPanelManager.Instance.PanelClosed(inventoryCanvasGroup.gameObject);
        }
    }
}
