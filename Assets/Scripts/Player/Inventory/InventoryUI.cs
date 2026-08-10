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

    readonly Dictionary<InventoryItem, InventoryItemUI> itemUIs = new();
    
    [HideInInspector] public bool inventoryOpen = false;

    void Start()
    {
        BuildGrid();
        Refresh();
    }
    
    void Update() 
    {
        bool tabPressed = InputManager.Actions.Player.Tab.WasPressedThisFrame();
        if (tabPressed) 
            ToggleInventory();
    }

    void OnEnable()
    {
        inventoryManager.OnInventoryChanged += Refresh;
    }

    void OnDisable()
    {
        inventoryManager.OnInventoryChanged -= Refresh;
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

    public void Refresh()
    {
        foreach (var ui in itemUIs.Values)
        {
            if (ui != null)
                Destroy(ui.gameObject);
        }

        itemUIs.Clear();

        foreach (InventoryItem item in inventoryManager.Items)
        {
            InventoryItemUI ui = Instantiate(itemPrefab, itemParent);
            ui.Initialize(item, this, gridLayout);
            itemUIs.Add(item, ui);
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

        return inventoryManager.MoveItem(itemUI.Item, gridPosition);
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
        
        if (inventoryOpen)
        {
            inventoryCanvasGroup.alpha = 1f;
            inventoryCanvasGroup.interactable = true;
            inventoryCanvasGroup.blocksRaycasts = true;
            CursorManager.UnlockCursor();
        }

        else
        {
            inventoryCanvasGroup.alpha = 0f;
            inventoryCanvasGroup.interactable = false;
            inventoryCanvasGroup.blocksRaycasts = false;
            CursorManager.LockCursor();
        }
    }
}
