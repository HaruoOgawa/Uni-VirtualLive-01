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
        Cull Front

        Pass
        {
            Tags { "LightMode" = "CustomGBufferLight" }

            // ZTest GEqual
            ZTest Always
            ZWrite Off
            ZClip false
            Cull Front
            Blend One One, Zero One
            BlendOp Add, Add

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

            float4 SRP_Deferred_LightPos;
            float4 SRP_Deferred_LightColor;

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

            struct LightData
            {
                bool enabled;
                float3 dir;
                float3 color;
                float attenuation;
            };

            LightData CreateLightData(GBufferData gData)
            {
                LightData data;

                data.color = SRP_Deferred_LightColor.rgb;

                data.enabled = true;

                // ポイントライト・スポットライトの両方ともこれでライト方向を算出する
                // 以前はスポットライトのライト方向にスポットライトの方向ベクトルを使っていたが、それは間違い
                // スポットライトはポイントライトの球を扇形に切り取ったものとして捉える
                // スポットライトのlightDirは減衰に使用
                #if defined(_LIGHT_DIRECTIONAL)
                data.dir = normalize(SRP_Deferred_LightPos.xyz);
                data.attenuation = 1.0;
                #elif defined(_LIGHT_POINT)
                float3 l2g = gData.WorldPos.xyz - SRP_Deferred_LightPos.xyz;
                
                // 距離による減衰
                float distPow2 = max(dot(l2g, l2g), 0.001);

                // ポイントライト範囲の境界を自然に減衰させる
                // 1. pow(distPow2 * SRP_Deferred_LightPos.w, 2.0)で 0 ～ 1 の線形な値を滑らかに上昇するようにする
                // 2. -1.0をかけて反対の二次関数にして +1だけずらす
                // 3. さらに2乗して滑らかにする
                // N次関数は関数を滑らかにする
                float rangeAtten = pow( saturate(1.0 - pow(distPow2 * SRP_Deferred_LightPos.w, 2.0) ) , 2.0);

                data.dir = normalize(l2g);
                // 距離減衰と範囲減衰の結果を組み合わせる
                data.attenuation = rangeAtten * 1.0f / distPow2;
                #elif defined(_LIGHT_SPOT)
                data.dir = normalize(gData.WorldPos.xyz - SRP_Deferred_LightPos.xyz);
                data.attenuation = 1.0;
                #endif

                return data;
            }

            float3 ComputeLight(float3 worldNormal, float3 lightDir, float3 color, float attenuation)
            {
                float3 col = float3(1.0, 1.0, 1.0);

                // どれぐらい光が当たっているかのdotは同じ方向のベクトルに対して行うのでlData.dirは反転する
                // 元のライト方向だと真正面から当たっているときにちょうどベクトルが反対で-1になってしまう
                float diffuse = max(0.0, dot(worldNormal, -lightDir));
                col = color * diffuse * attenuation;

                return col;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 NDCPos = i.projPos.xyy / i.projPos.w;

                float2 screenUV = NDCPos.xy * 0.5 + 0.5;
                screenUV.y = 1.0 - screenUV.y;
                float depth = NDCPos.z;

                float3 col = float3(0.0, 0.0, 0.0);
                float alpha = 1.0;

                GBufferData gData = CreateGBufferData(screenUV);
                LightData lData = CreateLightData(gData);

                col = ComputeLight(gData.WorldNormal, lData.dir, lData.color, lData.attenuation);

                return float4(col, alpha);
            }
            ENDCG
        }
    }
}
