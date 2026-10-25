using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public class UltraNavIcon : MaskableGraphic
    {
        public int kind;
        static Texture2D supplied;
        public override Texture mainTexture=>kind<3?(supplied!=null?supplied:(supplied=Resources.Load<Texture2D>("Ultra/UserTopIcons"))):base.mainTexture;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(kind<3 && mainTexture!=null)
            {
                var crops=new[]{new Rect(82,64,188,188),new Rect(355,64,188,188),new Rect(629,68,188,188)};
                var crop=crops[kind];
                // UVs refer to the original image, independent of Unity's NPOT resize.
                UltraReferenceArt.Quad(vh,rectTransform.rect,new Rect(crop.x/894f,crop.y/334f,crop.width/894f,crop.height/334f),color);return;
            }
            Quad(vh,.02f,.02f,.96f,.96f,new Color(.42f,.42f,.42f));
            var light=new Color(.85f,.85f,.85f);var dark=new Color(.33f,.33f,.33f);
            if(kind==3){for(int i=0;i<3;i++)Quad(vh,.20f,.23f+i*.23f,.60f,.075f,light);return;}
            if(kind==2)
            {
                Disc(vh,.39f,.64f,.17f,light);Disc(vh,.73f,.59f,.125f,new Color(.68f,.68f,.68f));
                Quad(vh,.18f,.18f,.45f,.29f,light);Quad(vh,.63f,.28f,.24f,.22f,new Color(.68f,.68f,.68f));return;
            }
            Quad(vh,.14f,.25f,.72f,.50f,light);
            if(kind==0){Triangle(vh,new Vector2(.16f,.73f),new Vector2(.84f,.73f),new Vector2(.50f,.40f),new Color(.65f,.65f,.65f));}
            else for(int i=0;i<3;i++){float y=.34f+i*.16f;Disc(vh,.25f,y,.035f,dark);Quad(vh,.39f,y-.025f,.35f,.05f,dark);}
        }
        void Quad(VertexHelper vh,float x,float y,float w,float h,Color tint)
        {
            var r=rectTransform.rect;int n=vh.currentVertCount;
            foreach(var p in new[]{new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h)})vh.AddVert(r.position+Vector2.Scale(p,r.size),tint,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color tint)
        {
            var r=rectTransform.rect;int n=vh.currentVertCount;foreach(var p in new[]{a,b,c})vh.AddVert(r.position+Vector2.Scale(p,r.size),tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);
        }
        void Disc(VertexHelper vh,float x,float y,float radius,Color tint)
        {
            for(int i=0;i<32;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;Triangle(vh,new Vector2(x,y),new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),new Vector2(x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius),tint);}
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
            Vector2[] points={new Vector2(r.xMin+c,r.yMin),new Vector2(r.xMax-c,r.yMin),new Vector2(r.xMax,r.yMin+c),new Vector2(r.xMax,r.yMax-c*.4f),new Vector2(r.xMax-c*.4f,r.yMax),new Vector2(r.xMin+c,r.yMax),new Vector2(r.xMin,r.yMax-c),new Vector2(r.xMin,r.yMin+c*.4f)};
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
            if(gem==null){var texture=Resources.Load<Texture2D>("Ultra/Crystal");if(texture!=null)gem=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));}
            sprite=gem;preserveAspect=true;raycastTarget=false;
        }
    }
}
