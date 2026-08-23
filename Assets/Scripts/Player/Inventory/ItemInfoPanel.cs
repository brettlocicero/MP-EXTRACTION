using System.Text;
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

    public void DisplayItemPanel(InventoryItem item)
    {
        canvasGroup.alpha = 1f;

        itemNameText.text = item.Instance.customName;
        descriptionText.text = BuildDescriptionText(item);
    }

    // TODO: Update this function to account for the ItemInstance's stats, instead of the base data stats.
    string BuildDescriptionText(InventoryItem item)
    {
        if (item.Data is WeaponSO weapon)
        {
            StringBuilder sb = new StringBuilder();

            var damageRange = weapon.GetDamageRange();
            if (damageRange.Item1.Equals(damageRange.Item2))
                sb.AppendLine($"Damage <color=red>{damageRange.Item1}</color>");
            else
                sb.AppendLine($"Damage <color=red>{damageRange.Item1} - {damageRange.Item2}</color>");

            sb.AppendLine($"Range <color=green>{weapon.range}</color>");
            sb.AppendLine($"Attack Rate <color=orange>{weapon.attackRate}</color>");
            return sb.ToString();
        }

        return item.Data.description;
    }

    public void HideItemPanel()
    {
        canvasGroup.alpha = 0f;
    }
}