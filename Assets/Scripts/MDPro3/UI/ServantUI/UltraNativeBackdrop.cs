using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    // Results sit over the 3D duel, so render the same native menu background
    // separately rather than exposing the duel field beneath a transparent page.
    [RequireComponent(typeof(RawImage))]
    public sealed class UltraNativeBackdrop : MonoBehaviour
    {
        Camera backdropCamera;
        RenderTexture texture;
        float nextFrame;
        void Start()
        {
            var source=Program.instance.camera_.camera2D;
            var holder=new GameObject("Native menu background camera");holder.transform.SetParent(transform,false);
            holder.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
            backdropCamera=holder.AddComponent<Camera>();backdropCamera.CopyFrom(source);backdropCamera.enabled=false;
            backdropCamera.cullingMask=1<<LayerMask.NameToLayer("2D");backdropCamera.clearFlags=CameraClearFlags.SolidColor;backdropCamera.backgroundColor=Color.black;
            var data=backdropCamera.GetUniversalAdditionalCameraData();var original=source.GetUniversalAdditionalCameraData();
            data.renderPostProcessing=original.renderPostProcessing;data.volumeLayerMask=original.volumeLayerMask;data.volumeTrigger=original.volumeTrigger;
            int width=Mathf.Min(1600,Screen.width),height=Mathf.RoundToInt(width/(float)Screen.width*Screen.height);
            texture=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);texture.Create();
            var image=GetComponent<RawImage>();image.texture=texture;image.color=Color.white;image.raycastTarget=true;
        }
        void LateUpdate()
        {
            if(backdropCamera==null || texture==null || Time.unscaledTime<nextFrame)return;
            nextFrame=Time.unscaledTime+1f/30;
            RenderPipeline.SubmitRenderRequest(backdropCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=texture});
        }
        void OnDestroy()
        {
            if(backdropCamera!=null){backdropCamera.targetTexture=null;Destroy(backdropCamera.gameObject);}
            if(texture!=null){texture.Release();Destroy(texture);}
        }
    }
}
