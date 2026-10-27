using Cysharp.Threading.Tasks;
using DG.Tweening;
using MDPro3.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Tween = DG.Tweening.Tween;

namespace MDPro3.UI.ServantUI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UltraCircleGraphic : Image
    {
        protected override void OnEnable()
        {
            UltraUi.EnsureRenderer(gameObject);
            base.OnEnable();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;vh.AddVert(r.center,color,new Vector2(.5f,.5f));
            for(int i=0;i<=64;i++){float a=i*Mathf.PI/32;var p=new Vector2(Mathf.Cos(a),Mathf.Sin(a));vh.AddVert(r.center+Vector2.Scale(p,r.size*.5f),color,(p+Vector2.one)*.5f);if(i>0)vh.AddTriangle(0,i,i+1);}
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UltraProfilePlate : UltraMesh
    {
        public Color accent=Color.gray;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float edge=r.height*.09f;
            Round(vh,r,r.height*.13f,accent);
            Round(vh,new Rect(r.x+edge,r.y+edge,r.width-2*edge,r.height-2*edge),r.height*.07f,Color.black);
        }
        static void Round(VertexHelper vh,Rect r,float radius,Color tint)
        {
            int start=vh.currentVertCount;vh.AddVert(r.center,tint,Vector2.zero);
            var centers=new[]{new Vector2(r.xMax-radius,r.yMax-radius),new Vector2(r.xMin+radius,r.yMax-radius),new Vector2(r.xMin+radius,r.yMin+radius),new Vector2(r.xMax-radius,r.yMin+radius)};
            for(int corner=0;corner<4;corner++)for(int point=0;point<=8;point++)
            {
                float angle=(corner*90+point*90f/8)*Mathf.Deg2Rad;
                vh.AddVert(centers[corner]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,tint,Vector2.zero);
            }
            for(int i=1;i<36;i++)vh.AddTriangle(start,start+i,start+i+1);vh.AddTriangle(start,start+36,start+1);
        }
    }
    public sealed class UltraAngularBar : UltraMesh
    {
        public float amount;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (r.width < 4f || r.height < 2f) return;
            if (r.height <= 8f)
            {
                Color thinTrack = new Color(0.10f, 0.11f, 0.13f, 1f);
                Slash(vh, r.xMin, r.xMax, r.yMin, r.yMax, 0f, thinTrack, thinTrack);
                if (amount <= 0.001f) return;
                float fillEnd = Mathf.Lerp(r.xMin, r.xMax, Mathf.Clamp01(amount));
                Slash(vh, r.xMin, fillEnd, r.yMin, r.yMax, 0f, UltraTheme.Accent, UltraTheme.Highlight);
                return;
            }
            float slash = Mathf.Min(r.height * 1.2f, r.width * 0.08f);
            Color track = new Color(0.28f, 0.30f, 0.34f, 1f);
            Slash(vh, r.xMin, r.xMax, r.yMin, r.yMax, slash, track, track);
            if (amount <= 0.001f) return;
            float end = Mathf.Lerp(r.xMin, r.xMax, Mathf.Clamp01(amount));
            float fillSlash = Mathf.Min(slash, Mathf.Max(1f, (end - r.xMin) * 0.45f));
            Slash(vh, r.xMin, end, r.yMin, r.yMax, fillSlash, UltraTheme.Accent, UltraTheme.Highlight);
        }
        static void Slash(VertexHelper vh, float x0, float x1, float y0, float y1, float slash, Color left, Color right)
        {
            if (x1 - x0 < 1f) return;
            if (slash <= 0.01f)
            {
                int flat = vh.currentVertCount;
                vh.AddVert(new Vector3(x0, y0), left, Vector2.zero);
                vh.AddVert(new Vector3(x1, y0), right, Vector2.zero);
                vh.AddVert(new Vector3(x1, y1), right, Vector2.zero);
                vh.AddVert(new Vector3(x0, y1), left, Vector2.zero);
                vh.AddTriangle(flat, flat + 1, flat + 2);
                vh.AddTriangle(flat, flat + 2, flat + 3);
                return;
            }
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(x0 + slash, y0), left, Vector2.zero);
            vh.AddVert(new Vector3(x1, y0), right, Vector2.zero);
            vh.AddVert(new Vector3(x1 - slash, y1), right, Vector2.zero);
            vh.AddVert(new Vector3(x0, y1), left, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }
    public sealed class UltraProfileHeader : MonoBehaviour
    {
        RawImage portrait;
        UltraCircleGraphic ring;
        UltraAngularBar bar;
        TextMeshProUGUI level, xp;
        int shown=-1,loaded;
        bool counting;
        Tween countTween;
        static RectTransform R(string name,Transform parent,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.gameObject.layer=parent.gameObject.layer;r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }
        public void Initialize()
        {
            var circle = R("Profile icon rim", transform, 0f, 0f, 0f, 0f);
            circle.anchorMin = circle.anchorMax = new Vector2(0f, 0.5f);
            circle.pivot = new Vector2(0f, 0.5f);
            circle.anchoredPosition = new Vector2(12f, 0f);
            circle.sizeDelta = new Vector2(92f, 92f);
            UltraUi.EnsureRenderer(circle.gameObject);
            ring = circle.gameObject.AddComponent<UltraCircleGraphic>();
            ring.raycastTarget = false;
            var clip = R("Icon mask", circle, .055f, .055f, .89f, .89f);
            UltraUi.EnsureRenderer(clip.gameObject);
            clip.gameObject.AddComponent<UltraCircleGraphic>();
            clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var portraitObject = R("Card portrait", clip, 0, 0, 1, 1).gameObject;
            UltraUi.EnsureRenderer(portraitObject);
            portrait = portraitObject.AddComponent<RawImage>();
            portrait.raycastTarget = false;
            var label = R("Level", transform, .215f, .30f, .34f, .20f);
            level = UltraUi.Text(label.gameObject);
            if (level != null)
            {
                level.fontSize = 18;
                level.color = UltraTheme.Fg;
                level.alignment = TextAlignmentOptions.MidlineLeft;
                level.raycastTarget = false;
                level.richText = true;
                UltraTheme.StyleEmphasis(level, 4f);
            }
            var xpHost = R("XP", transform, .50f, .30f, .46f, .20f);
            xp = UltraUi.Text(xpHost.gameObject);
            if (xp != null)
            {
                xp.fontSize = 16;
                xp.color = UltraTheme.Fg;
                xp.alignment = TextAlignmentOptions.MidlineRight;
                xp.raycastTarget = false;
                UltraTheme.StyleEmphasis(xp, 8f);
            }
            var track = R("XP track", transform, .215f, .16f, .74f, 0f);
            track.anchorMin = new Vector2(.215f, .16f);
            track.anchorMax = new Vector2(.955f, .16f);
            track.pivot = new Vector2(0.5f, 0.5f);
            track.sizeDelta = new Vector2(0f, 4f);
            UltraUi.EnsureRenderer(track.gameObject);
            bar = track.gameObject.AddComponent<UltraAngularBar>();
            bar.raycastTarget = false;
            UltraTheme.Changed += OnTheme;
            OnTheme();
        }
        void OnTheme()
        {
            if (ring != null) ring.color = UltraTheme.Accent;
            if (bar != null) bar.SetVerticesDirty();
        }
        public void BeginIntro()
        {
            counting = true;
            int into = Mathf.Max(0, UltraAccount.XP % 500);
            if (bar != null) { bar.amount = 0f; bar.SetVerticesDirty(); }
            if (xp != null) xp.text = "0 / 500 XP";
            countTween?.Kill();
            countTween = DOVirtual.DelayedCall(0.65f, () => CountXp(0, into), true).SetLink(gameObject);
        }
        void CountXp(int from, int into)
        {
            float t = 0f;
            countTween = DOTween.To(() => t, value =>
            {
                t = value;
                float eased = UltraMotion.Ease.Evaluate(value);
                int n = Mathf.RoundToInt(Mathf.Lerp(from, into, eased));
                if (xp != null) xp.text = n.ToString("N0") + " / 500 XP";
                if (bar != null)
                {
                    bar.amount = Mathf.Lerp(from, into, eased) / 500f;
                    bar.SetVerticesDirty();
                }
            }, 1f, 1.2f).SetEase(Ease.Linear).SetUpdate(true).SetLink(gameObject).OnComplete(() => { counting = false; countTween = null; });
        }
        void OnDestroy() { UltraTheme.Changed -= OnTheme; if (loaded != 0) CardImageLoader.ReleaseArt(loaded); }
        void Update()
        {
            if(portrait==null)return;
            int.TryParse(Config.Get("DuelFace0", "1010001"), out int id);
            if(id<1010000 || id>=1030000)id=1010001;
            if(id!=shown){shown=id;Load(id).Forget();}
            int into = UltraAccount.XP % 500;
            if (level != null) level.text = "<color=#" + ColorUtility.ToHtmlStringRGB(UltraTheme.Mute) + ">LV.</color>  " + UltraAccount.Level;
            if (counting) return;
            if (xp != null && countTween == null) xp.text = into.ToString("N0") + " / 500 XP";
            if (bar != null && countTween == null && !Mathf.Approximately(bar.amount, into / 500f))
            {
                bar.amount = into / 500f;
                bar.SetVerticesDirty();
            }
        }
        async UniTaskVoid Load(int id)
        {
            if(loaded!=0){CardImageLoader.ReleaseArt(loaded);loaded=0;}
            var sprite=await Program.items.TryLoadItemIconAsync(id.ToString(),Items.ItemType.Face);
            if(this==null || shown!=id || sprite==null)return;
            portrait.texture=sprite.texture;
            portrait.uvRect=new Rect(sprite.textureRect.x/sprite.texture.width,sprite.textureRect.y/sprite.texture.height,sprite.textureRect.width/sprite.texture.width,sprite.textureRect.height/sprite.texture.height);
            OnTheme();
        }
    }

    public sealed class UltraCountPop : MonoBehaviour
    {
        TMP_Text label;
        Tween tick, pop;
        int shown, target = int.MinValue;
        bool booted;
        public bool Holding { get; private set; }

        public void HoldZero()
        {
            tick?.Kill();
            pop?.Kill();
            Holding = true;
            booted = false;
            shown = 0;
            target = int.MinValue;
            if (label == null) label = GetComponent<TMP_Text>();
            if (label != null) label.text = "0";
            transform.localScale = Vector3.one;
        }

        public void Set(int value, bool fromZero)
        {
            if (label == null) label = GetComponent<TMP_Text>();
            if (label == null) return;
            if (Holding && !fromZero) return;
            Holding = false;
            if (booted && !fromZero && value == target) return;
            int start = fromZero || !booted ? 0 : shown;
            target = value;
            booted = true;
            tick?.Kill();
            if (start == value)
            {
                shown = value;
                label.text = value.ToString("N0");
                if (fromZero) Pop();
                return;
            }
            float t = 0f;
            tick = DOTween.To(() => t, v =>
            {
                t = v;
                float eased = 1f - Mathf.Pow(1f - v, 3f);
                shown = Mathf.RoundToInt(Mathf.Lerp(start, value, eased));
                label.text = shown.ToString("N0");
            }, 1f, 0.9f).SetEase(Ease.Linear).SetUpdate(true).SetLink(gameObject);
            Pop();
        }

        void Pop()
        {
            pop?.Kill();
            transform.localScale = Vector3.one;
            pop = transform.DOScale(1.08f, 0.15f).SetLoops(2, LoopType.Yoyo).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
        }
    }
}
