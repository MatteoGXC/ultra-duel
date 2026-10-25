using Cysharp.Threading.Tasks;
using MDPro3.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public sealed class UltraCircleGraphic : Image
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;vh.AddVert(r.center,color,new Vector2(.5f,.5f));
            for(int i=0;i<=64;i++){float a=i*Mathf.PI/32;var p=new Vector2(Mathf.Cos(a),Mathf.Sin(a));vh.AddVert(r.center+Vector2.Scale(p,r.size*.5f),color,(p+Vector2.one)*.5f);if(i>0)vh.AddTriangle(0,i,i+1);}
        }
    }
    public sealed class UltraProfilePlate : MaskableGraphic
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
    public sealed class UltraProfileHeader : MonoBehaviour
    {
        RawImage portrait;
        UltraCircleGraphic ring;
        UltraChrome chrome;
        UltraProfilePlate plate;
        RectTransform progress;
        TextMeshProUGUI level;
        int shown=-1,loaded;
        static RectTransform R(string name,Transform parent,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.gameObject.layer=parent.gameObject.layer;r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }
        public void Initialize()
        {
            chrome = GetComponent<UltraChrome>();
            if (chrome != null) { chrome.color = chrome.rimColor = Color.clear; chrome.rim = 0; }
            var circle = R("Profile icon rim", transform, .025f, .08f, .0f, .84f);
            circle.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            ring = circle.gameObject.AddComponent<UltraCircleGraphic>();
            ring.raycastTarget = false;
            var clip = R("Icon mask", circle, .07f, .07f, .86f, .86f);
            clip.gameObject.AddComponent<UltraCircleGraphic>();
            clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            portrait = R("Card portrait", clip, 0, 0, 1, 1).gameObject.AddComponent<RawImage>();
            portrait.raycastTarget = false;
            var label = R("Level", transform, .30f, .08f, .62f, .28f);
            level = label.gameObject.AddComponent<TextMeshProUGUI>();
            level.font = Program.instance.ui_.tmpFont;
            level.fontSize = 18;
            level.color = UltraTheme.Mute;
            level.raycastTarget = false;
            UltraTheme.StyleEmphasis(level, 8f);
            var track = R("XP track", transform, .30f, .40f, .62f, .08f);
            track.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
            progress = R("XP fill", transform, .30f, .40f, 0f, .08f);
            var fill = progress.gameObject.AddComponent<Image>();
            fill.color = UltraTheme.Accent;
            fill.gameObject.AddComponent<UltraAccentFill>();
            UltraTheme.Changed += OnTheme;
            OnTheme();
        }
        void OnTheme() { if (ring != null) ring.color = UltraTheme.Accent; }
        void OnDestroy() { UltraTheme.Changed -= OnTheme; if (loaded != 0) CardImageLoader.ReleaseArt(loaded); }
        void Update()
        {
            if(portrait==null)return;
            int.TryParse(Config.Get("DuelFace0", "1010001"), out int id);
            if(id<1010000 || id>=1030000)id=1010001;
            if(id!=shown){shown=id;Load(id).Forget();}
            level.text = "LV.  " + UltraAccount.Level + "     " + (UltraAccount.XP % 500) + " / 500 XP";
            progress.anchorMax = new Vector2(.30f + .62f * (UltraAccount.XP % 500) / 500f, .48f);
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
}
