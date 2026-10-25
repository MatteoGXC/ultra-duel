using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    // A banner composition, rather than squeezing a portrait into a wide slot.
    // Its palette and constellation are specific to the equipped wallpaper.
    public sealed class UltraWallpaperBanner : MaskableGraphic
    {
        public Color accent=new Color(.4f,.65f,1);
        public int seed;
        float hue=-1;
        void Update()
        {
            var theme=Program.instance!=null && Program.instance.background_!=null?Program.instance.background_.GetComponent<UltraWallpaperTheme>():null;
            if(theme!=null && theme.Ready && theme.AppliedWallpaper==seed.ToString() && (accent!=theme.Accent || !Mathf.Approximately(hue,theme.TargetHue)))
            {hue=theme.TargetHue;accent=theme.Accent;SetVerticesDirty();}
        }
        void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color left,Color right)
        {
            int i=vh.currentVertCount;vh.AddVert(a,left,Vector2.zero);vh.AddVert(b,left,Vector2.zero);vh.AddVert(c,right,Vector2.zero);vh.AddVert(d,right,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Quad(vh,new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin),new Color(.025f,.035f,.06f),Color.Lerp(accent,Color.black,.60f));
            var random=new System.Random(seed);
            for(int n=0;n<6;n++)
            {
                float x=r.xMin+r.width*(.36f+n*.15f),w=r.width*.055f;
                Quad(vh,new Vector2(x,r.yMin),new Vector2(x+r.height*.65f,r.yMax),new Vector2(x+r.height*.65f+w,r.yMax),new Vector2(x+w,r.yMin),new Color(accent.r,accent.g,accent.b,.08f),new Color(accent.r,accent.g,accent.b,.28f));
            }
            var center=new Vector2(r.xMin+r.width*.84f,r.center.y);
            float radius=r.height*.39f;
            for(int n=0;n<48;n++)
            {
                float a=n*Mathf.PI/24,b=(n+1)*Mathf.PI/24;
                var v=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var w=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                Quad(vh,center+v*radius,center+v*(radius+1.5f),center+w*(radius+1.5f),center+w*radius,accent,accent);
            }
            for(int n=0;n<35;n++)
            {
                var p=new Vector2(r.xMin+(float)random.NextDouble()*r.width,r.yMin+(float)random.NextDouble()*r.height);float size=n%7==0?2.5f:1;
                Quad(vh,p+Vector2.left*size,p+Vector2.up*size,p+Vector2.right*size,p+Vector2.down*size,Color.Lerp(accent,Color.white,.5f),Color.Lerp(accent,Color.white,.5f));
            }
        }
    }
}
