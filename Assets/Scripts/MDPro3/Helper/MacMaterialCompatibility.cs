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
                _ => null
            };
            if(replacement==null)return;
            var shader=Resources.Load<Shader>("Ultra/"+replacement);
            if(shader==null || !shader.isSupported){Debug.LogError("MAC_MATERIAL_SHADER_MISSING "+replacement);return;}
            material.shader=shader;
        }
#endif
    }
}
