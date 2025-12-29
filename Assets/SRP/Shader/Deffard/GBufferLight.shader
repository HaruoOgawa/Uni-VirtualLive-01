Shader "CustomSRP/GBufferLight"
{
    Properties
    {
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

           HLSLPROGRAM
           
           #pragma multi_compile _LIGHT_DIRECTIONAL _LIGHT_POINT _LIGHT_SPOT
           
           #pragma vertex vert
           #pragma fragment frag

           // UnityCG.cgincの代わりにUnityInput.hlslを使う。そうしないとPackagesフォルダをincludeしたときに重複定義でエラーになってしまう
           // このような書き方をしないと例えばPBR.hlslとかでリフレクションプローブのunity_SpecCube0が見えなくなる
           //#include "UnityCG.cginc"
           #include "../ShaderLibrary/UnityInput.hlsl"
           #include "../ShaderLibrary/PBR.hlsl"
           #include "../ShaderLibrary/ShadowMapping.hlsl"

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
               float MaterialType;
               float3 Albedo;
               float Roughness;
               float Metallic;
               float3 WorldNormal;
               float3 WorldPos;
               float3 EmissiveColor;
           };

           sampler2D SRP_GBuffer_0;
           sampler2D SRP_GBuffer_1;
           sampler2D SRP_GBuffer_2;
           sampler2D SRP_GBuffer_3;
           sampler2D SRP_GBuffer_4;

           float4 SRP_Deferred_LightPos;
           float4 SRP_Deferred_LightColor;
           float4 SRP_Deferred_LightDir;
           float4 SRP_Deferred_SpotAngle;
           int    SRP_Deferred_DirectionalLightIndex;

           float4x4 SRP_Deferred_SpotLightViewProjMatrix;
           int SRP_Deferred_UseGobo;
           sampler2D SRP_Deferred_Gobo_Texture;

           #define MAX_MAIN_LIGHT_COUNT 4
           #define MAX_SUB_LIGHT_COUNT 64

           // シャドウマッピング
           sampler2D SRP_ShadowMap;
           float4 SRP_ShadowTexelSize;
           float4x4 SRP_DirectionLight_ViewProjMatrix_List[MAX_MAIN_LIGHT_COUNT];
           float4x4 SRP_DirectionLight_LightUVBiasMatrix_List[MAX_MAIN_LIGHT_COUNT];

           v2f vert (appdata v)
           {
               v2f o;
               
               #if defined(_LIGHT_DIRECTIONAL)
               o.vertex = v.vertex;
               o.lightWorldPos = float4(0.0, 0.0, 0.0, 0.0);
               #else
               o.vertex = mul(unity_MatrixVP, mul(unity_ObjectToWorld, v.vertex));
               o.lightWorldPos = mul(unity_ObjectToWorld, v.vertex);
               #endif

               o.projPos = o.vertex;

               return o;
           }

           GBufferData CreateGBufferData(float2 screenUV)
           {
               GBufferData data;

               float4 GBuffer_0 = tex2D(SRP_GBuffer_0, screenUV); // Albedo.rgb   Roughness.a
               float4 GBuffer_1 = tex2D(SRP_GBuffer_1, screenUV); // WorldNormal.rgb Metallic.a
               float4 GBuffer_2 = tex2D(SRP_GBuffer_2, screenUV); // WorldPos.rgb    MaterialType.r
               float4 GBuffer_3 = tex2D(SRP_GBuffer_3, screenUV); // IndirectCol.rgb None
               float4 GBuffer_4 = tex2D(SRP_GBuffer_4, screenUV); // EmissiveColor.rgba

               data.MaterialType = GBuffer_2.a;
               data.Albedo = GBuffer_0.rgb;
               data.Roughness = GBuffer_0.a;
               data.Metallic = GBuffer_1.a;
               data.WorldNormal = GBuffer_1.rgb;
               data.WorldPos = GBuffer_2.rgb;
               data.EmissiveColor = GBuffer_4.rgb;

               return data;
           }

           float Square(float val)
           {
               return pow(val, 2.0);
           }

           LightData CreateLightData(GBufferData gData)
           {
               LightData data;

               data.color = SRP_Deferred_LightColor.rgb;

               // ポイントライト・スポットライトの両方ともこれでライト方向を算出する
               // 以前はスポットライトのライト方向にスポットライトの方向ベクトルを使っていたが、それは間違い
               // スポットライトはポイントライトの球を扇形に切り取ったものとして捉える
               // スポットライトのlightDirは減衰に使用
               #if defined(_LIGHT_DIRECTIONAL)
                   data.dir = normalize(SRP_Deferred_LightPos.xyz);
                   data.attenuation = 1.0;
               #elif defined(_LIGHT_POINT) || defined(_LIGHT_SPOT)

                   float3 l2g = gData.WorldPos.xyz - SRP_Deferred_LightPos.xyz;
                   data.dir = normalize(l2g);

                   // 距離減衰
                   float distPow2 = max(dot(l2g, l2g), 0.001);
                   float distAttenuation = 1.0 / distPow2;

                   // 範囲減衰
                   // ポイントライト範囲の境界を自然に減衰させる
                   // 1. pow(distPow2 * SRP_Deferred_LightPos.w, 2.0)で 0 ～ 1 の線形な値を滑らかに上昇するようにする
                   // 2. -1.0をかけて反対の二次関数にして +1だけずらす
                   // 3. さらに2乗して滑らかにする
                   // N次関数は関数を滑らかにする
                   float rangeAttenuation = pow( saturate(1.0 - pow(distPow2 * SRP_Deferred_LightPos.w, 2.0) ) , 2.0);

                   // 角度減衰
                   // 光はまっすぐ進むので光の進行方向から角度が離れるほど減衰していくと考える
                   // https://catlikecoding.com/unity/tutorials/custom-srp/point-and-spot-lights/
                   float angleAttenuation = Square(
                          saturate(
                              dot(SRP_Deferred_LightDir.xyz, data.dir) *
                              SRP_Deferred_SpotAngle.x + SRP_Deferred_SpotAngle.y
                          )
                   );

                   // 減衰結果を組み合わせる
                   data.attenuation = distAttenuation * rangeAttenuation * angleAttenuation;
               #endif

               return data;
           }

           PBRData CreatePBRData(GBufferData gData)
           {
               PBRData pbr;
               pbr.Albedo = gData.Albedo.rgb;
               pbr.Metallic = gData.Metallic;
               pbr.Roughness = gData.Roughness;
               pbr.WorldNormal = gData.WorldNormal.rgb;
               pbr.ViewDir = normalize(gData.WorldPos.xyz - _WorldSpaceCameraPos);

               return pbr;
           }

           float CalcGoboAttenuation(float3 worldPos)
           {
               float4 LightProjPos = mul(SRP_Deferred_SpotLightViewProjMatrix, float4(worldPos, 1.0));
               float3 LightNDCPos = LightProjPos.xyz / LightProjPos.w;
               float2 LightScreenUV = LightNDCPos.xy * 0.5 + 0.5;

               float Gobo = tex2D(SRP_Deferred_Gobo_Texture, LightScreenUV).a;

               return Gobo;
           }

           float4 frag (v2f i) : SV_Target
           {
               float3 NDCPos = i.projPos.xyz / i.projPos.w;

               float2 screenUV = NDCPos.xy * 0.5 + 0.5;
               screenUV.y = 1.0 - screenUV.y;
               float depth = NDCPos.z;

               float3 col = float3(0.0, 0.0, 0.0);
               float alpha = 1.0;

               GBufferData gData = CreateGBufferData(screenUV);
               LightData light = CreateLightData(gData);
               PBRData pbr = CreatePBRData(gData);

               // シャドウマッピング
               float shadow = 1.0;
               if(SRP_Deferred_DirectionalLightIndex >= 0)
               {
                    float4 lightProjPos = mul(SRP_DirectionLight_ViewProjMatrix_List[SRP_Deferred_DirectionalLightIndex], float4(gData.WorldPos, 1.0));
                    float4x4 LightUVBiasMatrix = SRP_DirectionLight_LightUVBiasMatrix_List[SRP_Deferred_DirectionalLightIndex];
                    shadow = CalcShadow(SRP_ShadowMap, SRP_ShadowTexelSize.xy, lightProjPos, gData.WorldNormal, light.dir, LightUVBiasMatrix);
               }

               // PBR
               col = ComputeDirectLight(pbr, light) * shadow;

               // GoBoテクスチャ
               if(SRP_Deferred_UseGobo == 1)
               {
                   float GoboAtten = CalcGoboAttenuation(gData.WorldPos);
                   col *= GoboAtten;
                   alpha *= GoboAtten;
               }
               
               return float4(col, alpha);
           }
           ENDHLSL
        }
    }
}
