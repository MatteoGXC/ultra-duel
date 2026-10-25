Shader "Ultra/MacGuide" {
 Properties {
 _Texture2D ("Guide texture", 2D) = "black" {}
 _Opacity ("Opacity", Range(0,1)) = 1
 _Opacity02 ("Transition opacity", Float) = 0
 _ColorP1 ("Player", Color) = (0,.5272732,1,0)
 _ColorP2 ("Opponent", Color) = (1,0,.01776171,0)
 _Switch ("Player side", Float) = 1
 _Move01 ("Horizontal movement", Float) = 0
 _Move02 ("Vertical movement", Float) = 0
 _Scale ("Scale", Float) = 1
 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
 Cull Off
 ZWrite Off
 Blend SrcAlpha One
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
 float _Opacity,_Opacity02,_Switch,_Move01,_Move02,_Scale;
 half4 _ColorP1,_ColorP2;
 half4 frag(Varyings i) : SV_Target {
   float2 uv=(i.uv-.5)*max(.001,_Scale)+.5+float2(_Move01,_Move02);
   half4 texel=SAMPLE_TEXTURE2D(_Texture2D,sampler_Texture2D,uv);
   half mask=max(texel.r,max(texel.g,texel.b))*texel.a;
   half3 tint=saturate(lerp(_ColorP2.rgb,_ColorP1.rgb,saturate(_Switch)));
   return half4(tint,mask*saturate(max(_Opacity,_Opacity02))*i.color.a*.35);
 }
 ENDHLSL
 }
 }
}
