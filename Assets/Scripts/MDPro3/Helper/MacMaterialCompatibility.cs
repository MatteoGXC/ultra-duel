using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MDPro3
{
    // Reconstructed legacy bundles contain exported constant-white shader stubs.
    // Rebind only those known field/card shaders to native Metal programs; keep
    // every material's artwork, meshes, animation and other shader properties.
    internal sealed class MacMaterialCompatibility : MonoBehaviour
    {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        private float nextCheck;
        private Volume intelDuelVolume;
        private VolumeProfile intelDuelProfile;
        private UniversalAdditionalCameraData intelMain, intelUi;
        private bool mainPostProcessing, uiPostProcessing;
        private readonly bool intelProcess=RuntimeInformation.ProcessArchitecture==Architecture.X64;

        private void UpdateIntelDuelBloom()
        {
            if(!intelProcess)return;
            bool inDuel=Program.instance!=null && Program.instance.ocgcore!=null
                && Program.instance.currentServant==Program.instance.ocgcore;
            // Intel Metal can produce a completely black HDR post-process frame,
            // including the overlay UI. Keep field animations and restore the
            // authored menu effects when leaving the duel camera stack.
            if(inDuel && intelMain==null)
            {
                intelMain=Program.instance.camera_.cameraMain.GetUniversalAdditionalCameraData();
                intelUi=Program.instance.camera_.cameraUI.GetUniversalAdditionalCameraData();
                mainPostProcessing=intelMain.renderPostProcessing;
                uiPostProcessing=intelUi.renderPostProcessing;
                Debug.Log("MAC_INTEL_DUEL_POST_PROCESS_COMPATIBILITY_ENABLED");
            }
            if(intelMain!=null)intelMain.renderPostProcessing=inDuel?false:mainPostProcessing;
            if(intelUi!=null)intelUi.renderPostProcessing=inDuel?false:uiPostProcessing;
            if(inDuel && intelDuelVolume==null)
            {
                var holder=new GameObject("Intel duel HDR compatibility");holder.transform.SetParent(transform,false);
                intelDuelProfile=ScriptableObject.CreateInstance<VolumeProfile>();
                intelDuelProfile.Add<Bloom>().intensity.Override(0);
                intelDuelVolume=holder.AddComponent<Volume>();intelDuelVolume.isGlobal=true;
                intelDuelVolume.priority=10000;intelDuelVolume.sharedProfile=intelDuelProfile;
                var camera=Program.instance.camera_.cameraMain;
                if(camera!=null)camera.GetUniversalAdditionalCameraData().volumeLayerMask|=1<<holder.layer;
                Debug.Log("MAC_INTEL_DUEL_BLOOM_COMPATIBILITY_ENABLED");
            }
            if(intelDuelVolume!=null)intelDuelVolume.weight=inDuel?1:0;
        }
        private void OnDestroy(){if(intelDuelProfile!=null)Destroy(intelDuelProfile);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host=new GameObject("MacMaterialCompatibility");
            DontDestroyOnLoad(host);host.AddComponent<MacMaterialCompatibility>();
        }
        private void LateUpdate()
        {
            // The Intel HDR path over-amplifies legacy field effects. Preserve their
            // animations and limit this bloom override to the 3D duel camera.
            UpdateIntelDuelBloom();
            if(Time.unscaledTime<nextCheck)return;
            nextCheck=Time.unscaledTime+0.25f;
            foreach(var material in Resources.FindObjectsOfTypeAll<Material>())Repair(material);
        }
        internal static void Repair(Material material)
        {
            if(material==null || material.shader==null)return;
            string replacement=material.shader.name switch
            {
                "Shader Graphs/UnlitRecieveShadow" => "MacField",
                "Shader Graphs/UnlitRecieveShadow_vc" => "MacFieldVertex",
                "Shader Graphs/UnlitRecieveShadow_alpha" => "MacFieldAlpha",
                "Shader Graphs/NormalStyle3D" => "MacCard",
                "Shader Graphs/PlayableGuide_c001_chousei" => "MacGuide",
                "Shader Graphs/PlayableGuide_c001_change" => "MacGuide",
                "Shader Graphs/PlayableGuide_c001_Luminous" => "MacGuideGlow",
                "Shader Graphs/fxs_BG_leafShadow_01" => "MacLeafShadow",
                "Shader Graphs/fxs_BG_leafShadow_02" => "MacLeafShadow",
                "fxs_BG_leafShadow_01" => "MacLeafShadow",
                "Shader Graphs/Timer_c001_01" => "MacTimerFace",
                "Shader Graphs/Timer_c001_02" => "MacTimerRing",
                "Shader Graphs/Timer_c001_03" => "MacTimerFace",
                _ => null
            };
            if(replacement==null && RuntimeInformation.ProcessArchitecture==Architecture.X64)
            {
                // These exported model shaders also contain unusable Intel Metal
                // variants. Keep their original UVs/textures and animated meshes.
                replacement=material.shader.name switch
                {
                    "Shader Graphs/GraveCommon01" => "MacField",
                    "Shader Graphs/Unlit_simpleAlpha" => "MacFieldAlpha",
                    "Shader Graphs/AvatarShader1" => "MacField",
                    _ => null
                };
                if(material.shader.name=="Shader Graphs/AvatarShader1"
                    && material.GetTexture("_Texture2D") is Texture artwork
                    && artwork.name.ToLowerInvariant().Contains("glass"))replacement="MacFieldAlpha";
            }
            if(replacement==null)return;
            var shader=Resources.Load<Shader>("Ultra/"+replacement);
            if(shader==null || !shader.isSupported){Debug.LogError("MAC_MATERIAL_SHADER_MISSING "+replacement);return;}
            Texture guideTexture=null;
            if(replacement=="MacGuide" && material.HasProperty("_Texture2D"))
            {
                guideTexture=material.GetTexture("_Texture2D");
                const string transitionTexture="_SampleTexture2D_72db3480b1bdea8583e6b7743c79a377_Texture_1_Texture2D";
                if(guideTexture==null && material.HasProperty(transitionTexture))guideTexture=material.GetTexture(transitionTexture);
            }
            Texture loadingTexture=null;
            if(replacement=="MacCard" && material.GetTexture("_MainTex")==null && material.HasProperty("_LoadingTex"))
                loadingTexture=material.GetTexture("_LoadingTex");
            material.shader=shader;
            if(loadingTexture!=null)material.SetTexture("_MainTex",loadingTexture);
            if(guideTexture!=null)material.SetTexture("_Texture2D",guideTexture);
            if(replacement=="MacGuide" || replacement=="MacGuideGlow")material.renderQueue=3000;
            if(replacement=="MacLeafShadow")material.renderQueue=2995;
            if(replacement=="MacTimerFace")material.renderQueue=3000;
            if(replacement=="MacTimerRing")material.renderQueue=3001;
        }
#endif
    }
}
