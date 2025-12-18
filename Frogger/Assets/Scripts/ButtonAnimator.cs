using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Sway Settings")]
    public float swayAngle = 8f;
    public float swayDuration = 1.2f;
    public float randomStartOffset = 0.3f;

    [Header("Hover Settings")]
    public float hoverScale = 1.15f;
    public float scaleDuration = 0.25f;

    private RectTransform rect;
    private Tween swayTween;
    private Tween scaleTween;

    private Vector3 normalScale;

    void OnEnable()
    {
        rect = GetComponent<RectTransform>();
        normalScale = rect.localScale;

        StartSway();
    }

    void StartSway()
    {
        swayTween?.Kill();

        rect.localRotation = Quaternion.identity;

        swayTween = rect
            .DORotate(new Vector3(0, 0, swayAngle), swayDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetDelay(Random.Range(-randomStartOffset, randomStartOffset));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        scaleTween?.Kill();

        scaleTween = rect
            .DOScale(hoverScale, scaleDuration)
            .SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        scaleTween?.Kill();

        scaleTween = rect
            .DOScale(normalScale, scaleDuration)
            .SetEase(Ease.InOutSine);
    }

    void OnDisable()
    {
        swayTween?.Kill();
        scaleTween?.Kill();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        scaleTween?.Kill();
        scaleTween = rect
            .DOScale(normalScale, 0f)
            .SetEase(Ease.InOutSine);
    }
}
