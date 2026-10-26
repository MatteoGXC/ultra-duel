using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MDPro3.UI.ServantUI
{
    // screens.css: --t-fast 140ms, --t-med 200ms, --t-press 70ms, cubic-bezier(.2,.9,.25,1).
    public static class UltraMotion
    {
        public const float Fast = 0.14f;
        public const float Med = 0.20f;
        public const float Press = 0.07f;
        public static readonly AnimationCurve Ease = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 4.5f),
            new Keyframe(0.35f, 0.86f, 1.1f, 0.45f),
            new Keyframe(1f, 1f, 0.15f, 0f));
    }

    // Buttons lift 2px over 140ms and press to 96%. Mode cards pass a taller lift and a softer press.
    public sealed class UltraButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public float liftPixels = 2f;
        public float pressScale = 0.96f;
        Tween lift, press;
        Vector2 rest;
        float scale = 1f;
        bool hover, captured;

        void Capture()
        {
            if (captured) return;
            rest = ((RectTransform)transform).anchoredPosition;
            captured = true;
        }
        void Lift(float y, float time)
        {
            Capture();
            lift?.Kill();
            lift = ((RectTransform)transform).DOAnchorPos(rest + new Vector2(0f, y), time).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
        }
        void Scale(float target, float time)
        {
            press?.Kill();
            press = DOTween.To(() => scale, value => { scale = value; transform.localScale = Vector3.one * value; }, target, time)
                .SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
        }
        public void OnPointerEnter(PointerEventData eventData) { hover = true; Lift(liftPixels, UltraMotion.Fast); }
        public void OnPointerExit(PointerEventData eventData) { hover = false; Lift(0f, UltraMotion.Fast); Scale(1f, UltraMotion.Fast); }
        public void OnPointerDown(PointerEventData eventData) => Scale(pressScale, UltraMotion.Press);
        public void OnPointerUp(PointerEventData eventData) => Scale(1f, UltraMotion.Fast);
        public void OnSelect(BaseEventData eventData) => Lift(liftPixels, UltraMotion.Fast);
        public void OnDeselect(BaseEventData eventData) { if (!hover) Lift(0f, UltraMotion.Fast); }
        void OnDisable()
        {
            lift?.Kill();
            press?.Kill();
            scale = 1f;
            transform.localScale = Vector3.one;
            if (captured) ((RectTransform)transform).anchoredPosition = rest;
            hover = false;
        }
    }
}
