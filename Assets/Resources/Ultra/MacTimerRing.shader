Shader "Ultra/MacTimerRing" {
 Properties {
 _MaxTime ("Time remaining", Range(0,1)) = 1
 _AddTime ("Bonus time", Range(0,1)) = 0
 _Active ("Active", Range(0,1)) = 1
 _MaxTimeColor01 ("Time edge", Color) = (.21,.64,1,0)
 _MaxTimeColor02 ("Time inner", Color) = (0,.41,1,0)
 _AddTimeColorBar01 ("Bonus edge", Color) = (1,.8,0,0)
 _AddTimeColor02 ("Bonus inner", Color) = (1,.8,0,0)
 _SwitchAddToMax ("Use normal time", Range(0,1)) = 1
 _BarBaseColor ("Empty ring", Color) = (.19,.19,.19,0)
 _BarTickness ("Inner radius", Range(0,1)) = .8
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
 float _MaxTime,_AddTime,_Active,_SwitchAddToMax,_BarTickness;
 half4 _MaxTimeColor01,_MaxTimeColor02,_AddTimeColorBar01,_AddTimeColor02,_BarBaseColor;
 half4 frag(Varyings i) : SV_Target {
   float2 p=(i.uv-.5)*2;float radius=length(p);float inner=clamp(_BarTickness,.65,.95);
   clip(1-radius);clip(radius-inner);
   float angle=frac(atan2(p.x,p.y)/6.2831853+1);
   float remaining=saturate(lerp(_AddTime,_MaxTime,_SwitchAddToMax));
   half fill=step(angle,remaining);
   half edge=saturate((radius-inner)/(1-inner));
   half3 normal=lerp(_MaxTimeColor02.rgb,_MaxTimeColor01.rgb,edge);
   half3 bonus=lerp(_AddTimeColor02.rgb,_AddTimeColorBar01.rgb,edge);
   half3 tint=lerp(bonus,normal,saturate(_SwitchAddToMax));
   return half4(saturate(lerp(_BarBaseColor.rgb,tint,fill)*lerp(.5,1,saturate(_Active))),i.color.a);
 }
 ENDHLSL
 }
 }
}
