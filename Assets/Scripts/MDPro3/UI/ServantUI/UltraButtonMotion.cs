using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MDPro3.UI.ServantUI
{
    // Hover lifts over 160ms. Press scales to 95% in 70ms. Both ease out.
    public sealed class UltraButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
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
            lift = ((RectTransform)transform).DOAnchorPos(rest + new Vector2(0f, y), time).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }
        void Scale(float target, float time)
        {
            press?.Kill();
            press = DOTween.To(() => scale, value => { scale = value; transform.localScale = Vector3.one * value; }, target, time)
                .SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }
        public void OnPointerEnter(PointerEventData eventData) { hover = true; Lift(3f, 0.16f); }
        public void OnPointerExit(PointerEventData eventData) { hover = false; Lift(0f, 0.14f); Scale(1f, 0.14f); }
        public void OnPointerDown(PointerEventData eventData) => Scale(0.95f, 0.07f);
        public void OnPointerUp(PointerEventData eventData) => Scale(1f, 0.14f);
        public void OnSelect(BaseEventData eventData) => Lift(3f, 0.16f);
        public void OnDeselect(BaseEventData eventData) { if (!hover) Lift(0f, 0.14f); }
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
