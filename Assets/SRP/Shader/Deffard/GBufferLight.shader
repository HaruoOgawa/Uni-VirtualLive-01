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

            // ‰ÁŽZ•`‰æ
            Blend One One
            BlendOp Add
            ZWrite Off
            ZTest Always

            CGPROGRAM
            
            #pragma multi_compile _LIGHT_DIRECTIONAL _LIGHT_POINT _LIGHT_SPOT
            
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 projPos : TEXCOORD0;
                float4 lightWorldPos : TEXCOORD1;
            };

            struct GBufferData
            {
                fixed MaterialType;
                float3 BaseColor;
                float Roughness;
                float Metallic;
                float3 WorldNormal;
                float3 WorldPos;
            };

            sampler2D SRP_GBuffer_0;
            sampler2D SRP_GBuffer_1;
            sampler2D SRP_GBuffer_2;
            sampler2D SRP_GBuffer_3;
            sampler2D SRP_GBuffer_4;

            float4 SRP_LightPos;
            float4 SRP_LightColor;

            v2f vert (appdata v)
            {
                v2f o;
                
                #if defined(_LIGHT_DIRECTIONAL)
                o.vertex = v.vertex;
                o.lightWorldPos = float4(0.0, 0.0, 0.0, 0.0);
                #else
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.lightWorldPos = mul(UNITY_MATRIX_M, v.vertex);
                #endif

                o.projPos = o.vertex;
                return o;
            }

            GBufferData CreateGBufferData(float2 screenUV)
            {
                GBufferData data;

                float4 GBuffer_0 = tex2D(SRP_GBuffer_0, screenUV); // BaseColor.rgb   Roughness.a
                float4 GBuffer_1 = tex2D(SRP_GBuffer_1, screenUV); // WorldNormal.rgb Metallic.a
                float4 GBuffer_2 = tex2D(SRP_GBuffer_2, screenUV); // WorldPos.rgb    MaterialType.r
                float4 GBuffer_3 = tex2D(SRP_GBuffer_3, screenUV); // None.rgba
                float4 GBuffer_4 = tex2D(SRP_GBuffer_4, screenUV); // None.rgba

                data.MaterialType = GBuffer_2.a;
                data.BaseColor = GBuffer_0.rgb;
                data.Roughness = GBuffer_0.a;
                data.Metallic = GBuffer_1.a;
                data.WorldNormal = GBuffer_1.rgb;
                data.WorldPos = GBuffer_2.rgb;

                return data;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 NDCPos = i.projPos.xyy / i.projPos.w;

                float2 screenUV = NDCPos.xy * 0.5 + 0.5;
                screenUV.y = 1.0 - screenUV.y;
                float depth = NDCPos.z;

                float3 col = float3(0.0, 0.0, 0.0);
                float alpha = 1.0;

                GBufferData data = CreateGBufferData(screenUV);

                #if defined(_LIGHT_DIRECTIONAL)
                float diffuse = max(0.0, dot(data.WorldNormal, SRP_LightPos.xyz));
                col = SRP_LightColor.rgb * diffuse;
                #elif defined(_LIGHT_POINT)
                col = float3(1.0, 0.0, 0.0);
                #elif defined(_LIGHT_SPOT)
                col = float3(0.0, 0.0, 1.0);
                #endif

                return float4(col, alpha);
            }
            ENDCG
        }
    }
}
