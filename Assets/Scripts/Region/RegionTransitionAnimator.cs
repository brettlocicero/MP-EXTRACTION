using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class RegionTransitionAnimator : MonoBehaviour
{
    [SerializeField] CanvasGroup ingameUI;
    [SerializeField] CanvasGroup regionEntrance;
    [SerializeField] Image screenCover;

    [Header("Ingame UI")]
    [SerializeField] float ingameUIFadeOutDuration = 1f;
    [SerializeField] float ingameUIFadeInStart = 9.016666f;
    [SerializeField] float ingameUIFadeInDuration = 1f;

    [Header("Region Entrance")]
    [SerializeField] float regionEntranceFadeInStart = 3.0166667f;
    [SerializeField] float regionEntranceFadeInDuration = 1f;
    [SerializeField] float regionEntranceFadeOutStart = 7.016667f;
    [SerializeField] float regionEntranceFadeOutDuration = 3f;

    [Header("Screen Cover")]
    [SerializeField] float screenCoverFadeInDuration = 1f;
    [SerializeField] float screenCoverFadeOutStart = 2.5f;
    [SerializeField] float screenCoverFadeOutDuration = 1.5f;

    Sequence transitionSequence;

    public float PlayDuration => Mathf.Max(
        ingameUIFadeInStart + ingameUIFadeInDuration,
        regionEntranceFadeOutStart + regionEntranceFadeOutDuration,
        screenCoverFadeOutStart + screenCoverFadeOutDuration
    );

    public Sequence PlayTransition()
    {
        transitionSequence?.Kill();

        ingameUI.alpha = 1f;
        regionEntrance.alpha = 0f;

        Color coverColor = screenCover.color;
        coverColor.a = 0f;
        screenCover.color = coverColor;

        transitionSequence = DOTween.Sequence();

        transitionSequence.Insert(0f, ingameUI.DOFade(0f, ingameUIFadeOutDuration).SetEase(Ease.Linear));
        transitionSequence.Insert(ingameUIFadeInStart, ingameUI.DOFade(1f, ingameUIFadeInDuration).SetEase(Ease.Linear));

        transitionSequence.Insert(regionEntranceFadeInStart, regionEntrance.DOFade(1f, regionEntranceFadeInDuration).SetEase(Ease.Linear));
        transitionSequence.Insert(regionEntranceFadeOutStart, regionEntrance.DOFade(0f, regionEntranceFadeOutDuration).SetEase(Ease.Linear));

        transitionSequence.Insert(0f, screenCover.DOFade(1f, screenCoverFadeInDuration).SetEase(Ease.Linear));
        transitionSequence.Insert(screenCoverFadeOutStart, screenCover.DOFade(0f, screenCoverFadeOutDuration).SetEase(Ease.Linear));

        transitionSequence.SetUpdate(true);
        return transitionSequence;
    }
}