using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamageNumberUI : MonoBehaviour
{
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

    public void Display(float damage)
    {
        damageText.text = Mathf.RoundToInt(damage).ToString();

        Color startColor = damageText.color;
        startColor.a = 1f;
        damageText.color = damage >= bigHitDamageThreshold ? bigHitColor : startColor;

        float scaleMultiplier = damage >= bigHitDamageThreshold ? bigHitMaxScale : punchScale;

        transform.localScale = Vector3.zero;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + new Vector3(Random.Range(-horizontalDrift, horizontalDrift), riseDistance, 0f);

        Sequence sequence = DOTween.Sequence();

        // Punch in, then settle to a resting scale
        sequence.Append(transform.DOScale(scaleMultiplier, punchDuration).SetEase(Ease.OutBack));
        sequence.Append(transform.DOScale(settleScale, punchDuration * 0.6f).SetEase(Ease.InOutSine));

        // Rise runs across the whole lifetime, independent of the scale punch
        sequence.Join(transform.DOMove(endPosition, lifetime).SetEase(Ease.OutCubic));

        sequence.Insert(fadeStartDelay, damageText.DOFade(0f, lifetime - fadeStartDelay));

        sequence.OnComplete(() => Destroy(gameObject));
    }
}