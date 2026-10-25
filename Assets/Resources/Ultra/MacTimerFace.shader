Shader "Ultra/MacTimerFace" {
 Properties {
 _Texture2D ("Timer face", 2D) = "black" {}
 _SwitchTurn ("Side", Range(0,1)) = 0
 _Active ("Active", Range(0,1)) = 1
 _MouseOver ("Hover", Range(0,1)) = 0
 _PressButton ("Pressed", Range(0,1)) = 0
 _ColorMouseOverNear ("Near hover", Color) = (0,.675,1,1)
 _ColorPlayNear ("Near active", Color) = (0,.36,1,0)
 _ColorMouseOverFar ("Far hover", Color) = (.55,0,0,1)
 _ColorPlayFar ("Far active", Color) = (.11,.04,.04,0)
 _InactiveIntensityNear ("Near inactive", Float) = .5
 _InactiveIntensityFar ("Far inactive", Float) = .5
 _ColorP1 ("Near trim", Color) = (.45,.83,1,0)
 _ColorP2 ("Far trim", Color) = (1,.46,.52,0)
 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
 Cull Off
 ZWrite Off
 Blend SrcAlpha OneMinusSrcAlpha
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
 float _SwitchTurn,_Active,_MouseOver,_PressButton,_InactiveIntensityNear,_InactiveIntensityFar;
 half4 _ColorMouseOverNear,_ColorPlayNear,_ColorMouseOverFar,_ColorPlayFar;
 half4 frag(Varyings i) : SV_Target {
   half4 texel=SAMPLE_TEXTURE2D(_Texture2D,sampler_Texture2D,i.uv);
   half3 baseTint=lerp(_ColorPlayNear.rgb,_ColorPlayFar.rgb,saturate(_SwitchTurn));
   half3 hoverTint=lerp(_ColorMouseOverNear.rgb,_ColorMouseOverFar.rgb,saturate(_SwitchTurn));
   half3 tint=lerp(baseTint,hoverTint,saturate(_MouseOver));
   half inactive=lerp(_InactiveIntensityNear,_InactiveIntensityFar,saturate(_SwitchTurn));
   half3 result=texel.rgb*lerp(inactive,1,saturate(_Active))+saturate(tint)*.22;
   return half4(saturate(result*(1-.15*saturate(_PressButton))),texel.a*i.color.a);
 }
 ENDHLSL
 }
 }
}
