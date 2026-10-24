using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public class UltraNavIcon : MaskableGraphic
    {
        public int kind;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(kind==3){for(int i=0;i<3;i++)Line(vh,.12f,.25f+i*.25f,.88f,.25f+i*.25f);return;}
            if(kind==2)
            {
                Circle(vh,.37f,.70f,.16f);Circle(vh,.72f,.62f,.13f);
                Line(vh,.10f,.15f,.18f,.42f);Line(vh,.18f,.42f,.55f,.42f);Line(vh,.55f,.42f,.64f,.15f);
                Line(vh,.63f,.35f,.88f,.35f);Line(vh,.88f,.35f,.95f,.12f);return;
            }
            Line(vh,.10f,.18f,.90f,.18f);Line(vh,.90f,.18f,.90f,.82f);Line(vh,.90f,.82f,.10f,.82f);Line(vh,.10f,.82f,.10f,.18f);
            if(kind==0){Line(vh,.10f,.82f,.50f,.43f);Line(vh,.50f,.43f,.90f,.82f);}
            else for(int i=0;i<3;i++){float y=.32f+i*.19f;Line(vh,.22f,y,.30f,y);Line(vh,.42f,y,.76f,y);}
        }
        void Line(VertexHelper vh,float x,float y,float xx,float yy)
        {
            var r=rectTransform.rect;var a=new Vector2(r.x+x*r.width,r.y+y*r.height);var b=new Vector2(r.x+xx*r.width,r.y+yy*r.height);
            var n=Vector2.Perpendicular((b-a).normalized)*Mathf.Min(r.width,r.height)*.035f;int index=vh.currentVertCount;
            foreach(var p in new[]{a+n,b+n,b-n,a-n})vh.AddVert(p,Color.white,Vector2.zero);
            vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);
        }
        void Circle(VertexHelper vh,float x,float y,float radius)
        {
            var r=rectTransform.rect;int start=vh.currentVertCount;
            vh.AddVert(new Vector3(r.x+x*r.width,r.y+y*r.height),Color.white,Vector2.zero);
            for(int i=0;i<=20;i++){float angle=i*Mathf.PI/10;vh.AddVert(new Vector3(r.x+(x+radius*Mathf.Cos(angle))*r.width,r.y+(y+radius*Mathf.Sin(angle))*r.height),Color.white,Vector2.zero);if(i>0)vh.AddTriangle(start,start+i,start+i+1);}
        }
    }
    // Screen-space clipped corners and a thin rim, like the native duel menus.
    public class UltraChrome : Image
    {
        public float corner=12, rim=1;
        public Color rimColor=new Color(.52f,.61f,.68f,.7f);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Add(vh,r,rimColor);
            Add(vh,new Rect(r.x+rim,r.y+rim,Mathf.Max(0,r.width-2*rim),Mathf.Max(0,r.height-2*rim)),color);
        }
        void Add(VertexHelper vh,Rect r,Color tint)
        {
            int start=vh.currentVertCount;float c=Mathf.Min(corner,r.height*.28f,r.width*.15f);
            Vector2[] points={new Vector2(r.xMin+c,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax-c,r.yMax),new Vector2(r.xMin,r.yMax),new Vector2(r.xMin,r.yMin+c)};
            foreach(var p in points)vh.AddVert(p,tint,Vector2.zero);
            for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);
        }
    }
    public class UltraGem : Image
    {
        static Sprite gem;
        protected override void Awake()
        {
            base.Awake();
            if(gem==null){var texture=Resources.Load<Texture2D>("Ultra/Gem");if(texture!=null)gem=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));}
            sprite=gem;preserveAspect=true;raycastTarget=false;
        }
    }
}
