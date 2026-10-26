using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    // Display the supplied pixels directly; UV windows leave source images untouched.
    public static class UltraReferenceArt
    {
        public static void Quad(VertexHelper vh,Rect rect,Rect uv,Color tint)
        {
            int n=vh.currentVertCount;
            vh.AddVert(new Vector2(rect.xMin,rect.yMin),tint,new Vector2(uv.xMin,uv.yMin));
            vh.AddVert(new Vector2(rect.xMax,rect.yMin),tint,new Vector2(uv.xMax,uv.yMin));
            vh.AddVert(new Vector2(rect.xMax,rect.yMax),tint,new Vector2(uv.xMax,uv.yMax));
            vh.AddVert(new Vector2(rect.xMin,rect.yMax),tint,new Vector2(uv.xMin,uv.yMax));
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UltraNativeTitle : UltraMesh
    {
        static Texture2D artwork;
        public override Texture mainTexture=>artwork!=null?artwork:(artwork=Resources.Load<Texture2D>("Ultra/OriginalTitleLogo"));
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var texture=mainTexture;if(texture==null)return;var r=rectTransform.rect;
            // Emblem only. The lower band of the source is the hollow silver triangle.
            UltraReferenceArt.Quad(vh,r,new Rect(0,.67f,1,.33f),color);
        }
    }
}
