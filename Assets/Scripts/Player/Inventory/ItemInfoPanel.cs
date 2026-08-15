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

    public void DisplayItemPanel(ItemSO item)
    {
        canvasGroup.alpha = 1f;

        itemNameText.text = item.itemName;
        descriptionText.text = BuildDescriptionText(item);
    }

    string BuildDescriptionText(ItemSO item)
    {
        if (item is WeaponSO weapon)
        {
            StringBuilder sb = new StringBuilder();

            var damageRange = weapon.GetDamageRange();
            if (damageRange.Item1.Equals(damageRange.Item2))
                sb.AppendLine($"Damage {damageRange.Item1}");
            else
                sb.AppendLine($"Damage {damageRange.Item1} - {damageRange.Item2}");

            sb.AppendLine($"Range {weapon.range}");
            sb.AppendLine($"Attack Rate {weapon.attackRate}");
            return sb.ToString();
        }

        return item.description;
    }

    public void HideItemPanel()
    {
        canvasGroup.alpha = 0f;
    }
}