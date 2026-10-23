using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public sealed class UltraMatePreview : MonoBehaviour
    {
        GameObject stage;
        RenderTexture texture;
        Camera portraitCamera;
        float nextFrame;
        public async UniTask Load(int id)
        {
            Mate mate=null;
            try
            {
                mate=await ABLoader.LoadMateAsync(id);
                if(this==null){if(mate!=null)Destroy(mate.gameObject);return;}
                if(mate==null)return;
                stage=new GameObject("Profile mate stage");stage.transform.position=new Vector3(12000,12000,12000);
                mate.transform.SetParent(stage.transform,false);mate.transform.localPosition=Vector3.zero;
                mate.transform.localRotation=Quaternion.Euler(0,180,0);mate.parent=stage.transform;mate.gameObject.SetActive(true);mate.Play(Mate.MateAction.Entry);
                foreach(var t in stage.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
                foreach(var collider in stage.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                await UniTask.Delay(1000);
                if(this==null)return;
                var bounds=new Bounds(mate.transform.position+Vector3.up,Vector3.one);
                bool first=true;
                foreach(var renderer in mate.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if(!renderer.enabled || !renderer.gameObject.activeInHierarchy)continue;
                    var baked=new Mesh();renderer.BakeMesh(baked);baked.RecalculateBounds();var local=baked.bounds;
                    for(int corner=0;corner<8;corner++)
                    {
                        var v=local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                        v=renderer.transform.TransformPoint(v);
                        if(first){bounds=new Bounds(v,Vector3.zero);first=false;}else bounds.Encapsulate(v);
                    }
                    Destroy(baked);
                }
                if(first)foreach(var renderer in mate.GetComponentsInChildren<Renderer>())
                {
                    if(!renderer.enabled || !renderer.gameObject.activeInHierarchy)continue;
                    if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
                }
                Debug.Log("ULTRA_MATE_PREVIEW id="+id+" bounds="+bounds+" active="+mate.gameObject.activeInHierarchy);
                var cameraObject=new GameObject("Mate portrait camera");cameraObject.transform.SetParent(stage.transform,false);
                var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
                float aspect=Mathf.Clamp(((RectTransform)transform).rect.width/Mathf.Max(1,((RectTransform)transform).rect.height),.3f,3);
                float radius=bounds.extents.magnitude;
                var focus=bounds.center+Vector3.up*radius*.18f;
                camera.cullingMask=1<<29;camera.orthographic=true;camera.orthographicSize=Mathf.Max(radius*1.10f,bounds.extents.x/aspect*1.25f);
                camera.nearClipPlane=.01f;camera.farClipPlane=Mathf.Max(2000,bounds.size.magnitude*6);camera.depth=-20;
                camera.transform.position=focus+new Vector3(0,0,-Mathf.Max(10,bounds.size.magnitude*2));camera.transform.LookAt(focus);
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
                var lightObject=new GameObject("Mate portrait light");lightObject.transform.SetParent(stage.transform,false);
                lightObject.transform.rotation=Quaternion.Euler(35,-25,0);
                var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.cullingMask=1<<29;
                texture=new RenderTexture(Mathf.RoundToInt(1024*aspect),1024,24,RenderTextureFormat.ARGB32);texture.Create();camera.targetTexture=texture;portraitCamera=camera;camera.enabled=false;
                var image=gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
            }
            catch(Exception e){Debug.LogWarning("Profile mate preview: "+e.Message);if(mate!=null && stage==null)Destroy(mate.gameObject);}
        }
        void LateUpdate()
        {
            if(portraitCamera==null || texture==null || Time.unscaledTime<nextFrame)return;
            nextFrame=Time.unscaledTime+1f/30f;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(portraitCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=texture});
        }
        void OnDestroy()
        {
            if(portraitCamera!=null)portraitCamera.targetTexture=null;
            if(stage!=null)Destroy(stage);
            if(texture!=null){texture.Release();Destroy(texture);}
        }
    }
}
