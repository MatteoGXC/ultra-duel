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
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var c=r.center;float radius=Mathf.Min(r.width,r.height)*.46f;
            Disc(vh,c,radius,new Color(.72f,.75f,.76f));Disc(vh,c,radius-2,new Color(.005f,.008f,.012f));
            var a=c+new Vector2(radius*.28f,radius*.48f);var b=c+new Vector2(-radius*.30f,0);var d=c+new Vector2(radius*.28f,-radius*.48f);
            Line(vh,a,b,radius*.12f);Line(vh,b,d,radius*.12f);
        }
        static void Disc(VertexHelper vh,Vector2 c,float radius,Color color)
        {
            int start=vh.currentVertCount;vh.AddVert(c,color,Vector2.zero);
            for(int i=0;i<=48;i++){float a=i*Mathf.PI/24;vh.AddVert(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,color,Vector2.zero);if(i>0)vh.AddTriangle(start,start+i,start+i+1);}
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width)
        {
            int start=vh.currentVertCount;var n=Vector2.Perpendicular((b-a).normalized)*width;var lime=new Color(.78f,1,0);
            foreach(var p in new[]{a-n,a+n,b+n,b-n})vh.AddVert(p,lime,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
    public partial class MainMenuUI
    {
        Button RoundBack(Transform parent,UnityAction action,float x,float y,float w,float h)
        {
            var r=HomeRect("Back",parent,x,y,w,h);var icon=r.gameObject.AddComponent<UltraCircleBack>();
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=icon;button.onClick.AddListener(action);return button;
        }
    }
}
