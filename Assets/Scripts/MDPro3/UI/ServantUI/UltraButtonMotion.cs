using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MDPro3.UI.ServantUI
{
    public sealed class UltraButtonMotion : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler
    {
        Tween tween;
        bool hover;
        float animationScale=1;
        void Scale(float scale){tween?.Kill();tween=DOTween.To(()=>animationScale,x=>{animationScale=x;transform.localScale=Vector3.one*x;},scale,.16f).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);}
        public void OnPointerEnter(PointerEventData e){hover=true;Scale(1.035f);}
        public void OnPointerExit(PointerEventData e){hover=false;Scale(1);}
        public void OnPointerDown(PointerEventData e)=>Scale(.97f);
        public void OnPointerUp(PointerEventData e)=>Scale(hover?1.035f:1);
        public void OnSelect(BaseEventData e)=>Scale(1.035f);
        public void OnDeselect(BaseEventData e)=>Scale(1);
        void OnDisable(){tween?.Kill();animationScale=1;transform.localScale=Vector3.one;}
    }
}
