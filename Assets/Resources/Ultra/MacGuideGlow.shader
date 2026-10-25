Shader "Ultra/MacGuideGlow" {
 Properties {
 _ColorP1 ("Player", Color) = (0,.5272732,1,0)
 _ColorP2 ("Opponent", Color) = (1,0,.01776171,0)
 _Strength ("Strength", Float) = .7
 _Swich ("Player side", Float) = 1
 Vector1_7e240758ebcf4a538357aeb063fe9d8a ("Alpha", Range(0,1)) = 1
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
 half4 _ColorP1,_ColorP2;
 float _Strength,_Swich,Vector1_7e240758ebcf4a538357aeb063fe9d8a;
 half4 frag(Varyings i) : SV_Target {
   half3 tint=saturate(lerp(_ColorP2.rgb,_ColorP1.rgb,saturate(_Swich)));
   return half4(tint,saturate(_Strength*Vector1_7e240758ebcf4a538357aeb063fe9d8a)*i.color.a*.12);
 }
 ENDHLSL
 }
 }
}
