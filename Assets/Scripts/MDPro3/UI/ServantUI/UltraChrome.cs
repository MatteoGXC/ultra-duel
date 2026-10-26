using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UltraNavIcon : UltraMesh
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
    public enum UltraCut { Auto, Panel, Small, Flat, Tag }

    // Hanko ink panels: notched left edge and one clipped bottom-right corner.
    // Small controls keep only the clipped corner.
    [RequireComponent(typeof(CanvasRenderer))]
    public class UltraChrome : Image
    {
        public UltraCut cut = UltraCut.Auto;
        public float corner = 12, rim = 1.5f;
        public Color rimColor = new Color(236f / 255f, 230f / 255f, 218f / 255f, 0.16f);
        public bool followThemeRim = true;

        protected override void OnEnable()
        {
            UltraUi.EnsureRenderer(gameObject);
            base.OnEnable();
            UltraTheme.Changed += OnTheme;
            OnTheme();
        }
        protected override void OnDisable()
        {
            UltraTheme.Changed -= OnTheme;
            base.OnDisable();
        }
        void OnTheme()
        {
            if (!followThemeRim) return;
            rimColor = UltraTheme.Line;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            if (rect.width < 2f || rect.height < 2f) return;
            if (cut == UltraCut.Tag)
            {
                float chamfer = Mathf.Min(12f, rect.width * 0.35f, rect.height * 0.8f);
                Fan(vh, new[]
                {
                    new Vector2(rect.xMin + chamfer, rect.yMax),
                    new Vector2(rect.xMax, rect.yMax),
                    new Vector2(rect.xMax, rect.yMin),
                    new Vector2(rect.xMin, rect.yMin)
                }, color);
                return;
            }
            bool thin = rect.width < 22f || rect.height < 22f || cut == UltraCut.Flat;
            if (thin)
            {
                Fan(vh, new[] { new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax) }, color);
                return;
            }
            bool panel = cut != UltraCut.Small && rect.height > 48f && rect.width > 48f;
            float clip = Mathf.Min(panel ? 20f : 10f, rect.height * 0.34f, rect.width * 0.22f);
            var outer = Outline(rect, clip, panel);
            if (rim > 0.4f)
            {
                var innerRect = new Rect(rect.x + rim, rect.y + rim, Mathf.Max(1f, rect.width - rim * 2f), Mathf.Max(1f, rect.height - rim * 2f));
                var inner = Outline(innerRect, Mathf.Max(0f, clip - rim), panel);
                // The rim is a ring. Filling the whole outer shape and then a translucent
                // inner lets the rim color show through the caption and hides the label.
                Ring(vh, outer, inner, rimColor.a > 0.01f ? rimColor : UltraTheme.Line);
                Fan(vh, inner, color);
            }
            else Fan(vh, outer, color);
        }
        static void Ring(VertexHelper vh, Vector2[] outer, Vector2[] inner, Color tint)
        {
            int count = Mathf.Min(outer.Length, inner.Length);
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                int start = vh.currentVertCount;
                vh.AddVert(outer[i], tint, Vector2.zero);
                vh.AddVert(outer[next], tint, Vector2.zero);
                vh.AddVert(inner[next], tint, Vector2.zero);
                vh.AddVert(inner[i], tint, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }
        static Vector2[] Outline(Rect rect, float clip, bool panel)
        {
            var points = new System.Collections.Generic.List<Vector2>
            {
                new Vector2(rect.xMin, rect.yMax),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMax, rect.yMin + clip),
                new Vector2(rect.xMax - clip, rect.yMin),
                new Vector2(rect.xMin, rect.yMin)
            };
            if (panel)
            {
                float notch = Mathf.Min(9f, rect.width * 0.06f);
                points.Add(new Vector2(rect.xMin, rect.yMin + rect.height * 0.38f));
                points.Add(new Vector2(rect.xMin + notch, rect.yMin + rect.height * 0.43f));
                points.Add(new Vector2(rect.xMin + notch, rect.yMin + rect.height * 0.57f));
                points.Add(new Vector2(rect.xMin, rect.yMin + rect.height * 0.62f));
            }
            return points.ToArray();
        }
        static void Fan(VertexHelper vh, Vector2[] points, Color tint)
        {
            if (points.Length < 3) return;
            int start = vh.currentVertCount;
            Vector2 center = Vector2.zero;
            foreach (var point in points) center += point;
            center /= points.Length;
            vh.AddVert(center, tint, Vector2.zero);
            for (int i = 0; i < points.Length; i++) vh.AddVert(points[i], tint, Vector2.zero);
            for (int i = 0; i < points.Length; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + ((i + 1) % points.Length));
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public class UltraGem : Image
    {
        static Sprite gem;
        protected override void OnEnable()
        {
            UltraUi.EnsureRenderer(gameObject);
            base.OnEnable();
        }
        protected override void Awake()
        {
            base.Awake();
            if(gem==null){var texture=Resources.Load<Texture2D>("Ultra/Crystal");if(texture!=null)gem=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));}
            sprite=gem;preserveAspect=true;raycastTarget=false;
        }
    }
}
