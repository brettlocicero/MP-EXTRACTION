using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemInfoPanel : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI itemNameText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] Canvas canvas;
    [SerializeField] Vector2 offset = new Vector2(16f, -16f);

    RectTransform rectTransform;
    CanvasGroup canvasGroup;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void LateUpdate()
    {
        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rectTransform.parent, screenPosition, eventCamera, out Vector2 localPoint))
            return;

        rectTransform.anchoredPosition = localPoint + offset;
    }

    public void DisplayItemPanel(ItemSO item)
    {
        canvasGroup.alpha = 1f;

        itemNameText.text = item.itemName;
        descriptionText.text = item.description;
    }

    public void HideItemPanel()
    {
        canvasGroup.alpha = 0f;
    }
}