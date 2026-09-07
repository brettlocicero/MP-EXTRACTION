using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemInfoPanel : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI itemNameText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] Canvas canvas;
    [SerializeField] Vector2 offset = new Vector2(16f, -16f);
    [SerializeField] GameObject shardsParent;
    [SerializeField] Image[] shards;

    RectTransform rectTransform;
    CanvasGroup canvasGroup;

    [Header("Detail card layout")]
    [SerializeField] float panelWidth = 380f;
    [SerializeField] float minimumHeight = 360f;
    [SerializeField] float screenPadding = 16f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        HideItemPanel();
    }

    void LateUpdate()
    {
        if (canvasGroup.alpha > 0f)
            PositionPanel();
    }

    void PositionPanel()
    {
        if (Mouse.current == null) return;

        RectTransform parent = (RectTransform)rectTransform.parent;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
            Mouse.current.position.ReadValue(), eventCamera, out Vector2 pointer))
            return;

        Rect bounds = parent.rect;
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        Vector2 position = pointer + offset;
        if (position.x + width > bounds.xMax - screenPadding)
            position.x = pointer.x - Mathf.Abs(offset.x) - width;
        if (position.y - height < bounds.yMin + screenPadding)
            position.y = pointer.y + Mathf.Abs(offset.y) + height;

        position.x = Mathf.Clamp(position.x, bounds.xMin + screenPadding,
            Mathf.Max(bounds.xMin + screenPadding, bounds.xMax - screenPadding - width));
        position.y = Mathf.Clamp(position.y, bounds.yMin + screenPadding + height,
            Mathf.Max(bounds.yMin + screenPadding + height, bounds.yMax - screenPadding));
        // Use parent-local coordinates, independent of the parent's pivot and anchors.
        rectTransform.localPosition = new Vector3(position.x, position.y, 0f);
    }

    public void DisplayItemPanel(InventoryItem item)
    {
        itemNameText.text = string.IsNullOrWhiteSpace(item.Instance.customName)
            ? item.Data.itemName : item.Instance.customName;
        descriptionText.text = BuildDescriptionText(item);

        foreach (Image shardImage in shards)
            shardImage.gameObject.SetActive(false);

        bool isWeapon = item.Data is WeaponSO;
        shardsParent.SetActive(isWeapon && item.Instance.soulShards.Count > 0);
        if (isWeapon)
        {
            int count = Mathf.Min(shards.Length, item.Instance.soulShards.Count);
            for (int i = 0; i < count; i++)
            {
                SoulShardSO shard = item.Instance.soulShards[i];
                if (shard == null) continue;
                shards[i].gameObject.SetActive(true);
                shards[i].sprite = shard.icon;
            }
        }

        ResizeToContent();
        rectTransform.SetAsLastSibling();
        PositionPanel();
        canvasGroup.alpha = 1f;
    }

    void ResizeToContent()
    {
        Rect bounds = ((RectTransform)rectTransform.parent).rect;
        float width = Mathf.Min(panelWidth, Mathf.Max(80f, bounds.width - screenPadding * 2f));
        float contentWidth = width - 40f;
        float titleHeight = itemNameText.GetPreferredValues(itemNameText.text, contentWidth, 0f).y;
        float descriptionHeight = descriptionText.GetPreferredValues(descriptionText.text, contentWidth, 0f).y;
        float footerHeight = shardsParent.activeSelf ? 76f : 24f;
        float bodyTop = 20f + titleHeight + 20f;
        float height = Mathf.Min(Mathf.Max(minimumHeight, bodyTop + descriptionHeight + footerHeight),
            Mathf.Max(80f, bounds.height - screenPadding * 2f));

        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.sizeDelta = new Vector2(width, height);
        itemNameText.rectTransform.sizeDelta = new Vector2(-40f, titleHeight);
        descriptionText.rectTransform.anchoredPosition = new Vector2(0f, -bodyTop);
        descriptionText.rectTransform.sizeDelta = new Vector2(-40f, Mathf.Max(0f, height - bodyTop - footerHeight));
    }

    // TODO: Update this function to account for the ItemInstance's stats, instead of the base data stats.
    string BuildDescriptionText(InventoryItem item)
    {
        if (item.Data is WeaponSO weapon)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(item.Data.description))
            {
                sb.AppendLine(item.Data.description);
                sb.AppendLine();
            }

            var damageRange = weapon.GetDamageRange();
            if (damageRange.Item1.Equals(damageRange.Item2))
                sb.AppendLine($"Damage <color=red>{damageRange.Item1}</color>");
            else
                sb.AppendLine($"Damage <color=red>{damageRange.Item1} - {damageRange.Item2}</color>");

            sb.AppendLine($"Range <color=green>{weapon.range}</color>");
            sb.AppendLine($"Attack Rate <color=orange>{weapon.attackRate}</color>");
            sb.AppendLine($"Soul shards  {item.Instance.soulShards.Count} / {weapon.maxSlots}");
            return sb.ToString();
        }

        return item.Data.description;
    }

    public void HideItemPanel()
    {
        canvasGroup.alpha = 0f;
    }
}