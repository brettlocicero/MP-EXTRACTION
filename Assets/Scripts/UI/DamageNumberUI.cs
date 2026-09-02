using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamageNumberUI : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TextMeshProUGUI damageText;
    [SerializeField] float lifetime = 0.8f;
    [SerializeField] float riseDistance = 40f;
    [SerializeField] float fadeStartDelay = 0.3f;

    [Header("Impact")]
    [SerializeField] float punchScale = 1.4f;
    [SerializeField] float punchDuration = 0.15f;
    [SerializeField] float settleScale = 1f;
    [SerializeField] float horizontalDrift = 15f;

    [Header("Big Hit Scaling")]
    [SerializeField] float bigHitDamageThreshold = 40f;
    [SerializeField] float bigHitMaxScale = 1.8f;
    [SerializeField] Color bigHitColor = Color.yellow;

    Transform followTarget;
    Camera followCamera;
    Vector3 screenOffset;
    Vector3 localHitOffset;
    Vector3 lastKnownWorldPos;

    public void Display(float damage, Transform target, Camera viewCamera, Vector3 hitPoint)
    {
        followTarget = target;
        followCamera = viewCamera;
        lastKnownWorldPos = hitPoint;

        if (followTarget != null)
            localHitOffset = followTarget.InverseTransformPoint(hitPoint);

        damageText.text = Mathf.RoundToInt(damage).ToString();

        Color startColor = damageText.color;
        startColor.a = 1f;
        damageText.color = damage >= bigHitDamageThreshold ? bigHitColor : startColor;

        float scaleMultiplier = damage >= bigHitDamageThreshold ? bigHitMaxScale : punchScale;

        transform.localScale = Vector3.zero;
        screenOffset = Vector3.zero;

        Vector3 driftTarget = new Vector3(Random.Range(-horizontalDrift, horizontalDrift), riseDistance, 0f);

        Sequence sequence = DOTween.Sequence();

        sequence.Append(transform.DOScale(scaleMultiplier, punchDuration).SetEase(Ease.OutBack));
        sequence.Append(transform.DOScale(settleScale, punchDuration * 0.6f).SetEase(Ease.InOutSine));

        sequence.Insert(0f, DOTween.To(() => screenOffset, x => screenOffset = x, driftTarget, lifetime).SetEase(Ease.OutCubic));

        sequence.Insert(fadeStartDelay, damageText.DOFade(0f, lifetime - fadeStartDelay));

        sequence.OnComplete(() => Destroy(gameObject));
    }

    void LateUpdate()
    {
        if (followTarget != null)
            lastKnownWorldPos = followTarget.TransformPoint(localHitOffset);

        Vector3 screenPos = followCamera.WorldToScreenPoint(lastKnownWorldPos);

        canvasGroup.alpha = screenPos.z >= 0 ? 1f : 0f;
        transform.position = screenPos + screenOffset;
    }
}