using UnityEngine;

namespace MDPro3
{
    // Reconstructed legacy bundles contain exported constant-white shader stubs.
    // Rebind only those known field/card shaders to native Metal programs; keep
    // every material's artwork, meshes, animation and other shader properties.
    internal sealed class MacMaterialCompatibility : MonoBehaviour
    {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        private float nextCheck;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host=new GameObject("MacMaterialCompatibility");
            DontDestroyOnLoad(host);host.AddComponent<MacMaterialCompatibility>();
        }
        private void LateUpdate()
        {
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
