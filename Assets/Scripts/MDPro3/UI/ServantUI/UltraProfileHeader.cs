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
            chrome=GetComponent<UltraChrome>();chrome.color=chrome.rimColor=Color.clear;chrome.rim=0;
            plate=R("Rounded profile plate",transform,.20f,0,.80f,1).gameObject.AddComponent<UltraProfilePlate>();plate.raycastTarget=false;
            var band=R("Name band",transform,.23f,.46f,.43f,.49f).gameObject.AddComponent<UltraChrome>();band.color=new Color(.21f,.20f,.20f);band.rimColor=Color.clear;band.rim=0;band.raycastTarget=false;
            var circle=R("Profile icon rim",transform,-.015f,-.17f,.29f,1.34f);circle.gameObject.AddComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.HeightControlsWidth;
            ring=circle.gameObject.AddComponent<UltraCircleGraphic>();ring.raycastTarget=false;
            var clip=R("Icon mask",circle,.065f,.065f,.87f,.87f);clip.gameObject.AddComponent<UltraCircleGraphic>();clip.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            portrait=R("Card portrait",clip,0,0,1,1).gameObject.AddComponent<RawImage>();portrait.raycastTarget=false;
            var label=R("Level",transform,.30f,.12f,.20f,.25f);level=label.gameObject.AddComponent<TextMeshProUGUI>();level.font=TMP_Settings.defaultFontAsset;level.fontSize=17;level.color=Color.white;level.raycastTarget=false;
            R("XP track",transform,.48f,.18f,.29f,.13f).gameObject.AddComponent<Image>().color=new Color(.20f,.20f,.20f);
            progress=R("XP fill",transform,.48f,.18f,0,.13f);progress.gameObject.AddComponent<Image>().color=new Color(.82f,.82f,.82f);
        }
        void Update()
        {
            if(portrait==null)return;
            int id=(int?)UltraAccount.Profile?["avatar"]??UltraProgress.Data.avatarCard;
            if(id!=shown){shown=id;Load(id).Forget();}
            level.text="Lv. "+UltraAccount.Level;progress.anchorMax=new Vector2(.48f+.29f*(UltraAccount.XP%500)/500f,.31f);
        }
        async UniTaskVoid Load(int id)
        {
            if(loaded!=0){CardImageLoader.ReleaseArt(loaded);loaded=0;}
            if(id==0){var s=await Program.items.TryLoadItemIconAsync(UltraAccount.ProfileMate.ToString(),Items.ItemType.Mate);if(this!=null && s!=null && shown==0){portrait.texture=s.texture;portrait.uvRect=new Rect(s.textureRect.x/s.texture.width,s.textureRect.y/s.texture.height,s.textureRect.width/s.texture.width,s.textureRect.height/s.texture.height);SampleAccent(s.texture,portrait.uvRect);}return;}
            var texture=await CardImageLoader.LoadArtAsync(id);
            if(this==null || id!=shown){CardImageLoader.ReleaseArt(id);return;}
            if(texture==null){CardImageLoader.ReleaseArt(id);return;}
            loaded=id;portrait.texture=texture;portrait.uvRect=new Rect(.15f,.08f,.70f,.84f);SampleAccent(texture,portrait.uvRect);
        }
        void SampleAccent(Texture texture,Rect uv)
        {
            var target=RenderTexture.GetTemporary(32,32);var old=RenderTexture.active;Texture2D sample=null;
            try{Graphics.Blit(texture,target,uv.size,uv.position);RenderTexture.active=target;sample=new Texture2D(32,32);sample.ReadPixels(new Rect(0,0,32,32),0,0);sample.Apply();float hue=UltraWallpaperTheme.DominantHue(sample.GetPixels32());SetAccent(Color.HSVToRGB(Mathf.Repeat(hue/360,1),.62f,.68f));}
            finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(target);if(sample!=null)Destroy(sample);}
        }
        void SetAccent(Color accent){plate.accent=accent;plate.SetVerticesDirty();ring.color=Color.Lerp(accent,Color.white,.25f);}
        void OnDestroy(){if(loaded!=0)CardImageLoader.ReleaseArt(loaded);}
    }
}
