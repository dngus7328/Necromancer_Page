Shader "UI/RibelDeathIris"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Tint", Color) = (1,1,1,1)

        _Center ("Iris Center", Vector) = (0.5, 0.5, 0, 0)

        _Radius ("Iris Radius", Range(0, 1.5)) = 1.5

        _Softness ("Edge Softness", Range(0.001, 0.2)) = 0.02

        [HideInInspector]
        _StencilComp ("Stencil Comparison", Float) = 8

        [HideInInspector]
        _Stencil ("Stencil ID", Float) = 0

        [HideInInspector]
        _StencilOp ("Stencil Operation", Float) = 0

        [HideInInspector]
        _StencilWriteMask ("Stencil Write Mask", Float) = 255

        [HideInInspector]
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        [HideInInspector]
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        Blend SrcAlpha OneMinusSrcAlpha

        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;

            float4 _MainTex_ST;

            fixed4 _Color;

            float4 _Center;

            float _Radius;

            float _Softness;

            v2f vert(
                appdata_t v)
            {
                v2f output;

                output.worldPosition =
                    v.vertex;

                output.vertex =
                    UnityObjectToClipPos(
                        v.vertex
                    );

                output.texcoord =
                    TRANSFORM_TEX(
                        v.texcoord,
                        _MainTex
                    );

                output.color =
                    v.color *
                    _Color;

                return output;
            }

            fixed4 frag(
                v2f input) : SV_Target
            {
                fixed4 texColor =
                    tex2D(
                        _MainTex,
                        input.texcoord
                    );

                float2 uv =
                    input.texcoord;

                float2 delta =
                    uv -
                    _Center.xy;

                float distanceFromCenter =
                    length(
                        delta
                    );

                float blackAlpha =
                    smoothstep(
                        _Radius -
                        _Softness,
                        _Radius +
                        _Softness,
                        distanceFromCenter
                    );

                fixed4 result =
                    fixed4(
                        0,
                        0,
                        0,
                        blackAlpha
                    );

                result.a *=
                    input.color.a *
                    texColor.a;

                return result;
            }

            ENDCG
        }
    }
}