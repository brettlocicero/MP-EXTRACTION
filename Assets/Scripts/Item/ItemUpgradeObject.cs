using TMPro;
using UnityEngine;

public class ItemUpgradeObject : MonoBehaviour, IInteractable
{
    [SerializeField] CanvasGroup upgradePanelUI;
    [SerializeField] TextMeshProUGUI itemNameText;
    [SerializeField] TMP_InputField itemNameRenameField;

    bool isOpen = false;
    ItemInstance itemInstance;

    void Awake()
    {
        itemNameRenameField.onEndEdit.AddListener(RenameItemEvent);
    }

    public void Interact()
    {
        itemInstance = GameManager.Instance.GetLocalPlayerItems().EquippedItem;
        if (itemInstance == null) return;

        if (!isOpen)
        {
            upgradePanelUI.alpha = 1f;
            upgradePanelUI.blocksRaycasts = true;
            upgradePanelUI.interactable = true;

            UpdatePanelFromItem(itemInstance);
            UIPanelManager.Instance.PanelOpened(upgradePanelUI.gameObject, lockMovement: false, lockSensitivity: true);

            isOpen = true;
        }
    }

    void ClosePanel()
    {
        if (isOpen)
        {
            upgradePanelUI.alpha = 0f;
            upgradePanelUI.blocksRaycasts = false;
            upgradePanelUI.interactable = false;

            UIPanelManager.Instance.PanelClosed(upgradePanelUI.gameObject);

            isOpen = false;
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.LocalPlayer == null || !isOpen) return;
        
        float dist = Vector3.Distance(GameManager.Instance.LocalPlayer.transform.position, transform.position);
        if (dist >= 5f) ClosePanel();
    }

    public void UpdatePanelFromItem(ItemInstance itemInstance)
    {
        string displayName = GetDisplayName(itemInstance);

        itemNameText.text = displayName;
        itemNameRenameField.text = displayName;
    }

    string GetDisplayName(ItemInstance itemInstance)
    {
        if (!string.IsNullOrWhiteSpace(itemInstance.customName)) return itemInstance.customName;

        return ItemDatabase.Instance.GetItem(itemInstance.baseItemId).itemName;
    }

    void RenameItemEvent(string newName)
    {
        if (itemInstance == null) return;

        itemInstance.customName = newName;
        UpdatePanelFromItem(itemInstance);
    }

    void OnDestroy()
    {
        itemNameRenameField.onEndEdit.RemoveListener(RenameItemEvent);
    }
}