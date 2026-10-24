using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MDPro3
{
    // Tint the native background camera, retaining its original glow and motion.
    // A local volume at a private trigger keeps the duel and wallpaper unchanged.
    public sealed class UltraWallpaperTheme : MonoBehaviour
    {
        ChannelMixer adjustment;
        VolumeProfile profile;
        Transform trigger;
        Transform previousTrigger;
        UniversalAdditionalCameraData cameraData;
        string wallpaper;
        public string AppliedWallpaper { get; private set; }
        float nextCheck, targetHue;
        public float TargetHue => targetHue;
        public bool Ready => adjustment != null && AppliedWallpaper == wallpaper && wallpaper != null;

        void Start()
        {
            cameraData=Program.instance.camera_.camera2D.GetUniversalAdditionalCameraData();
            previousTrigger=cameraData.volumeTrigger;
            var holder=new GameObject("Wallpaper background color");holder.transform.SetParent(transform,false);
            holder.transform.position=new Vector3(28000,28000,28000);holder.layer=LayerMask.NameToLayer("2D");trigger=holder.transform;
            var bounds=holder.AddComponent<BoxCollider>();bounds.isTrigger=true;bounds.size=Vector3.one*2;
            var volume=holder.AddComponent<Volume>();volume.isGlobal=false;volume.blendDistance=0;volume.priority=100;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
            adjustment=profile.Add<ChannelMixer>();adjustment.SetAllOverridesTo(true);
            cameraData.volumeLayerMask|=1<<holder.layer;cameraData.volumeTrigger=trigger;cameraData.renderPostProcessing=true;
        }

        void Update()
        {
            if(adjustment==null)return;
            // The native blue diagonal bands become gray. Only red glow/sparks
            // carry the selected wallpaper's accent; black and white stay neutral.
            var accent=Color.HSVToRGB(Mathf.Repeat(targetHue/360f,1),.85f,1);
            const float blueBalance=.16f;
            adjustment.redOutRedIn.value=accent.r*100;
            adjustment.redOutGreenIn.value=(1-(1+blueBalance)*accent.r)*100;
            adjustment.redOutBlueIn.value=blueBalance*accent.r*100;
            adjustment.greenOutRedIn.value=accent.g*100;
            adjustment.greenOutGreenIn.value=(1-(1+blueBalance)*accent.g)*100;
            adjustment.greenOutBlueIn.value=blueBalance*accent.g*100;
            adjustment.blueOutRedIn.value=accent.b*100;
            adjustment.blueOutGreenIn.value=(1-(1+blueBalance)*accent.b)*100;
            adjustment.blueOutBlueIn.value=blueBalance*accent.b*100;
            if(Time.unscaledTime<nextCheck)return;
            nextCheck=Time.unscaledTime+1;
            var selected=Config.Get("Wallpaper",UIManager.DefaultWallpaper);
            if(selected!=wallpaper){wallpaper=selected;Refresh(selected).Forget();}
        }

        async UniTaskVoid Refresh(string selected)
        {
            try
            {
                var sprite=await Program.items.TryLoadItemIconAsync(selected,Items.ItemType.Wallpaper);
                if(this==null || selected!=wallpaper || sprite==null)return;
                var target=RenderTexture.GetTemporary(48,48,0,RenderTextureFormat.ARGB32);
                var previous=RenderTexture.active;
                Texture2D sample=null;
                try
                {
                    var rect=sprite.textureRect;
                    Graphics.Blit(sprite.texture,target,new Vector2(rect.width/sprite.texture.width,rect.height/sprite.texture.height),new Vector2(rect.x/sprite.texture.width,rect.y/sprite.texture.height));
                    RenderTexture.active=target;
                    sample=new Texture2D(48,48,TextureFormat.RGBA32,false);sample.ReadPixels(new Rect(0,0,48,48),0,0);sample.Apply();
                    targetHue=DominantHue(sample.GetPixels32());
                    AppliedWallpaper=selected;
                    Debug.Log("ULTRA_WALLPAPER_THEME "+selected+" hue="+targetHue);
                }
                finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);if(sample!=null)Destroy(sample);}
            }
            catch(Exception e){Debug.LogWarning("Wallpaper background color: "+e.Message);}
        }

        public static float DominantHue(Color32[] pixels)
        {
            var weights=new float[36];
            foreach(var pixel in pixels)
            {
                Color c=pixel;Color.RGBToHSV(c,out float h,out float s,out float v);
                if(s<.22f || v<.15f || c.a<.5f)continue;
                weights[Mathf.Min(35,(int)(h*36))]+=s*s*v*c.a;
            }
            int best=0;float bestWeight=0;
            for(int i=0;i<36;i++){float weight=weights[(i+35)%36]+weights[i]+weights[(i+1)%36];if(weight>bestWeight){best=i;bestWeight=weight;}}
            float x=0,y=0;
            for(int offset=-1;offset<=1;offset++)
            {
                int bin=(best+offset+36)%36;float angle=bin*10*Mathf.Deg2Rad;
                x+=Mathf.Cos(angle)*weights[bin];y+=Mathf.Sin(angle)*weights[bin];
            }
            float degrees=bestWeight==0?210:Mathf.Atan2(y,x)*Mathf.Rad2Deg;
            return Mathf.DeltaAngle(0,degrees);
        }

        void OnDestroy()
        {
            if(cameraData!=null && cameraData.volumeTrigger==trigger)cameraData.volumeTrigger=previousTrigger;
            if(profile!=null)Destroy(profile);
        }
    }
}
