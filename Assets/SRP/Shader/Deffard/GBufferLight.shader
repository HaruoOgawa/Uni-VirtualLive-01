Shader "CustomSRP/GBufferLight"
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
            Tags { "LightMode" = "CustomGBufferLight" }

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

            sampler2D SRP_GBuffer_0;
            sampler2D SRP_GBuffer_1;
            sampler2D SRP_GBuffer_2;
            sampler2D SRP_GBuffer_3;
            sampler2D SRP_GBuffer_4;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = float4(1.0, 1.0, 1.0, 1.0);
                col += tex2D(SRP_GBuffer_0, i.uv);
                col += tex2D(SRP_GBuffer_1, i.uv);
                col += tex2D(SRP_GBuffer_2, i.uv);
                col += tex2D(SRP_GBuffer_3, i.uv);
                col += tex2D(SRP_GBuffer_4, i.uv);
                col = float4(i.uv, 0.0, 1.0);

                return col;
            }
            ENDCG
        }
    }
}
