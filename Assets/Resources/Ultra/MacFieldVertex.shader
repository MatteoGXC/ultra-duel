Shader "Ultra/MacFieldVertex" {
 Properties { _Texture2D ("Artwork", 2D) = "white" {} }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
 Cull Back
 ZWrite On
 
 Pass {
 Tags { "LightMode"="UniversalForward" }
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_Texture2D); SAMPLER(sampler_Texture2D);
 struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
 struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
 Varyings vert(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; o.color=i.color; return o; }
 half4 frag(Varyings i) : SV_Target { return SAMPLE_TEXTURE2D(_Texture2D,sampler_Texture2D,i.uv) * i.color; }
 ENDHLSL
 }
 }
}
