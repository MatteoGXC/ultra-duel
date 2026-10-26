using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public sealed class UltraPageTransition : MonoBehaviour
    {
        void Start()
        {
            var rect=(RectTransform)transform;
            var end=rect.anchoredPosition;
            // Unity's fake null does not trigger ??, so a missing group still falls through and throws.
            var group=gameObject.GetComponent<CanvasGroup>();
            if(group==null)group=gameObject.AddComponent<CanvasGroup>();
            if(group==null)return;
            group.alpha=0;group.blocksRaycasts=false;
            rect.localEulerAngles=Vector3.zero;
            rect.anchoredPosition=end+new Vector2(-70f,0f);
            rect.DOAnchorPos(end,.25f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
            group.DOFade(1,.25f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject).OnComplete(()=>group.blocksRaycasts=true);
        }
    }
    public static class UltraAdaptiveMaterial
    {
        static Material material;
        public static Material Get(Shader shader)=>material!=null?material:(material=new Material(shader));
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UltraCircleBack : UltraMesh
    {
        static Texture2D supplied;
        public override Texture mainTexture=>supplied!=null?supplied:(supplied=Resources.Load<Texture2D>("Ultra/UserBackIcon"));
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var c=r.center;float radius=Mathf.Min(r.width,r.height)*.46f;
            if(mainTexture==null)return;var texture=mainTexture;
            var uvCenter=new Vector2(161.5f/327f,127.5f/244f);
            vh.AddVert(c,color,uvCenter);
            for(int i=0;i<=64;i++)
            {
                float angle=i*Mathf.PI/32;var unit=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                vh.AddVert(c+unit*radius,color,uvCenter+Vector2.Scale(unit,new Vector2(83f/327f,83f/244f)));
                if(i>0)vh.AddTriangle(0,i,i+1);
            }
        }
    }
    public partial class MainMenuUI
    {
        Button RoundBack(Transform parent,UnityAction action,float x,float y,float w,float h)
        {
            var r=HomeRect("Back",parent,x,y,w,h);UltraUi.EnsureRenderer(r.gameObject);var icon=r.gameObject.AddComponent<UltraCircleBack>();
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=icon;button.onClick.AddListener(action);r.gameObject.AddComponent<UltraButtonMotion>();return button;
        }
        Button SquareBack(Transform parent,UnityAction action,float x,float y,float w,float h)
        {
            var r=HomeRect("Back",parent,x,y,w,h);
            UltraUi.EnsureRenderer(r.gameObject);
            var chrome=r.gameObject.AddComponent<UltraChrome>();
            chrome.cut=UltraCut.Small;chrome.color=UltraTheme.Panel;chrome.raycastTarget=true;
            var chevronHost=HomeRect("Chevron",r,.22f,.22f,.56f,.56f);
            UltraUi.EnsureRenderer(chevronHost.gameObject);
            var chevron=chevronHost.gameObject.AddComponent<UltraChevron>();
            chevron.color=UltraTheme.Fg;chevron.raycastTarget=false;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=chrome;button.onClick.AddListener(action);
            r.gameObject.AddComponent<UltraThemedButton>().Configure(UltraTier.Secondary);
            return button;
        }
    }
}
