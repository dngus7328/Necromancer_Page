Shader "UI/RibelDeathIris"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Center ("Center", Vector) = (0.5,0.5,0,0)
        _Radius ("Radius", Float) = 1.2
        _Softness ("Softness", Float) = 0.05
        _Aspect ("Aspect", Float) = 1.777777
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;

            float4 _Center;

            float _Radius;
            float _Softness;
            float _Aspect;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex =
                    UnityObjectToClipPos(
                        v.vertex
                    );

                o.uv =
                    v.uv;

                return o;
            }

            fixed4 frag(v2f i)
                : SV_Target
            {
                float2 delta =
                    i.uv -
                    _Center.xy;

                delta.x *=
                    _Aspect;

                float distanceFromCenter =
                    length(
                        delta
                    );

                float alpha =
                    smoothstep(
                        _Radius,
                        _Radius +
                        _Softness,
                        distanceFromCenter
                    );

                fixed4 color =
                    _Color;

                color.a *=
                    alpha;

                return color;
            }

            ENDCG
        }
    }
}