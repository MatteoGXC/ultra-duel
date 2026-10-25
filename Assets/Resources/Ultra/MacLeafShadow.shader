Shader "Ultra/MacLeafShadow" {
 Properties {
 _Texture2D ("Shadow mask", 2D) = "black" {}
 _ShadowColor ("Shadow color", Color) = (0,0,0,0)
 _ShadowIntensity ("Shadow intensity", Range(-1,0)) = -.5
 _AddAlpha ("Additional alpha", Range(0,1)) = 1
 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
 Cull Off
 ZWrite Off
 Blend DstColor Zero
 Pass {
 Tags { "LightMode"="UniversalForward" }
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
 struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
 Varyings vert(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; o.color=i.color; return o; }
 TEXTURE2D(_Texture2D); SAMPLER(sampler_Texture2D);
 half4 _ShadowColor;float _ShadowIntensity,_AddAlpha;
 half4 frag(Varyings i) : SV_Target {
   half4 texel=SAMPLE_TEXTURE2D(_Texture2D,sampler_Texture2D,i.uv);
   half mask=texel.r*texel.a*saturate(-_ShadowIntensity)*_AddAlpha*i.color.a;
   return half4(lerp(half3(1,1,1),_ShadowColor.rgb,mask),1);
 }
 ENDHLSL
 }
 }
}
