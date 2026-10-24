Shader "Ultra/ReferenceTransparency"
{
    Properties
    {
        [PerRendererData] _MainTex ("Artwork", 2D) = "white" {}
        _Background ("Background", Vector) = (0.12,0.12,0.12,0)
        _Tolerance ("Tolerance", Float) = 0.025
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; float4 position:TEXCOORD1; };
            sampler2D _MainTex;float4 _Background;float _Tolerance;float4 _ClipRect;
            Output vert(Input input) { Output o;o.position=input.vertex;o.vertex=UnityObjectToClipPos(input.vertex);o.uv=input.uv;o.color=input.color;return o; }
            fixed4 frag(Output i):SV_Target
            {
                fixed4 art=tex2D(_MainTex,i.uv);float3 delta=abs(art.rgb-_Background.rgb);
                art.a*=smoothstep(_Tolerance*.5,_Tolerance,max(delta.r,max(delta.g,delta.b)));
                art*=i.color;
                #ifdef UNITY_UI_CLIP_RECT
                art.a*=UnityGet2DClipping(i.position.xy,_ClipRect);
                #endif
                return art;
            }
            ENDCG
        }
    }
}
