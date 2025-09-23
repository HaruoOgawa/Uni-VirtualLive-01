Shader "Custom/GBufferTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "CustomGBufferGen" }

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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            struct FragOut
            {
                float4 col0 : SV_Target0;
                float4 col1 : SV_Target1;
                float4 col2 : SV_Target2;
                float4 col3 : SV_Target3;
                float4 col4 : SV_Target4;
            };

            FragOut frag (v2f i) : SV_Target
            {
                FragOut o;

                o.col0 = float4(1.0, 0.0, 0.0, 1.0);
                o.col1 = float4(0.0, 1.0, 0.0, 1.0);
                o.col2 = float4(0.0, 0.0, 1.0, 1.0);
                o.col3 = float4(1.0, 1.0, 0.0, 1.0);
                o.col4 = float4(0.0, 1.0, 1.0, 1.0);

                return o;
            }
            ENDCG
        }
    }
}
