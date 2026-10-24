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
            var end=rect.anchoredPosition3D;
            var group=gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;
            rect.anchoredPosition3D=end+new Vector3(-240,0,-360);rect.localEulerAngles=new Vector3(0,15,0);
            rect.DOAnchorPos3D(end,.5f).SetEase(Ease.OutQuart).SetUpdate(true).SetLink(gameObject);
            rect.DOLocalRotate(Vector3.zero,.5f).SetEase(Ease.OutQuart).SetUpdate(true).SetLink(gameObject);
            group.DOFade(1,.3f).SetDelay(.15f).SetUpdate(true).SetLink(gameObject).OnComplete(()=>group.blocksRaycasts=true);
        }
    }
    public static class UltraAdaptiveMaterial
    {
        static Material material;
        public static Material Get(Shader shader)=>material!=null?material:(material=new Material(shader));
    }
    public sealed class UltraCircleBack : MaskableGraphic
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
            var r=HomeRect("Back",parent,x,y,w,h);var icon=r.gameObject.AddComponent<UltraCircleBack>();
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=icon;button.onClick.AddListener(action);r.gameObject.AddComponent<UltraButtonMotion>();return button;
        }
    }
}
