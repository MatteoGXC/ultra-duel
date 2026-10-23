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
    public class UltraScreenBackdrop : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Quad(vh,r,new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1),new Color(.008f,.009f,.02f,1f),new Color(.008f,.009f,.02f,1f));
            Quad(vh,r,new Vector2(.0f,0),new Vector2(.40f,0),new Vector2(.98f,1),new Vector2(.58f,1),new Color(.12f,.07f,.22f),new Color(.13f,.18f,.32f));
            Quad(vh,r,new Vector2(.28f,0),new Vector2(.56f,0),new Vector2(1,1),new Vector2(.84f,1),new Color(.045f,.025f,.085f),new Color(.04f,.06f,.11f));
            for(float y=0;y<r.height;y+=4)
                Quad(vh,r,new Vector2(0,y/r.height),new Vector2(1,y/r.height),new Vector2(1,(y+1)/r.height),new Vector2(0,(y+1)/r.height),new Color(.65f,.75f,1,.035f),new Color(.65f,.75f,1,.035f));
            for(int i=0;i<36;i++)
            {
                float x=((i*47)%97)/100f,y=((i*29)%97)/100f;
                Quad(vh,r,new Vector2(x,y),new Vector2(x+.0012f,y),new Vector2(x+.0012f,y+.002f),new Vector2(x,y+.002f),new Color(.5f,.4f,1,.55f),new Color(.5f,.4f,1,.55f));
            }
        }
        static void Quad(VertexHelper vh,Rect r,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color low,Color high)
        {
            int start=vh.currentVertCount;
            var points=new[]{a,b,c,d};
            for(int i=0;i<4;i++)vh.AddVert(new Vector3(r.x+points[i].x*r.width,r.y+points[i].y*r.height),i<2?low:high,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
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
    public class UltraGem : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Vector2[] p={new Vector2(.5f,1),new Vector2(.92f,.30f),new Vector2(.5f,0),new Vector2(.08f,.30f),new Vector2(.5f,.38f)};
            Color[] c={new Color(.78f,.95f,1),new Color(.1f,.6f,1),new Color(.82f,.2f,.95f),new Color(.35f,.3f,.85f),Color.white};
            for(int i=0;i<p.Length;i++)vh.AddVert(new Vector3(r.x+p[i].x*r.width,r.y+p[i].y*r.height),c[i],Vector2.zero);
            vh.AddTriangle(0,1,4);vh.AddTriangle(1,2,4);vh.AddTriangle(2,3,4);vh.AddTriangle(3,0,4);
        }
    }
}
