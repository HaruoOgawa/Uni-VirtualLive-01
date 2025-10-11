Shader "CustomSRP/GBufferIndirectLight"
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
            Tags { "LightMode" = "CustomGBufferIndirectLight" }

            // ZTest GEqual
            ZTest Always
            ZWrite Off
            ZClip false
            Cull Front
            Blend One One, Zero One
            BlendOp Add, Add

            HLSLPROGRAM
            
            #pragma multi_compile
            
            #pragma vertex vert
            #pragma fragment frag

           // UnityCG.cgincの代わりにUnityInput.hlslを使う。そうしないとPackagesフォルダをincludeしたときに重複定義でエラーになってしまう
           // このような書き方をしないと例えばPBR.hlslとかでリフレクションプローブのunity_SpecCube0が見えなくなる
           //#include "UnityCG.cginc"
           #include "../ShaderLibrary/UnityInput.hlsl"
           #include "../ShaderLibrary/PBR.hlsl"

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
                o.lightWorldPos = float4(0.0, 0.0, 0.0, 0.0);
                o.projPos = o.vertex;
                return o;
            }

            GBufferData CreateGBufferData(float2 screenUV)
            {
                GBufferData data;

                float4 GBuffer_0 = tex2D(SRP_GBuffer_0, screenUV); // Albedo.rgb   Roughness.a
                float4 GBuffer_1 = tex2D(SRP_GBuffer_1, screenUV); // WorldNormal.rgb Metallic.a
                float4 GBuffer_2 = tex2D(SRP_GBuffer_2, screenUV); // WorldPos.rgb    MaterialType.r
                float4 GBuffer_3 = tex2D(SRP_GBuffer_3, screenUV); // None.rgba
                float4 GBuffer_4 = tex2D(SRP_GBuffer_4, screenUV); // None.rgba

                data.MaterialType = GBuffer_2.a;
                data.Albedo = GBuffer_0.rgb;
                data.Roughness = GBuffer_0.a;
                data.Metallic = GBuffer_1.a;
                data.WorldNormal = GBuffer_1.rgb;
                data.WorldPos = GBuffer_2.rgb;

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

            float4 frag (v2f i) : SV_Target
            {
                float3 NDCPos = i.projPos.xyy / i.projPos.w;

                float2 screenUV = NDCPos.xy * 0.5 + 0.5;
                screenUV.y = 1.0 - screenUV.y;
                float depth = NDCPos.z;

                float3 col = float3(0.0, 0.0, 0.0);
                float alpha = 1.0;

                GBufferData gData = CreateGBufferData(screenUV);
                PBRData pbr = CreatePBRData(gData);

                col = ComputeIndirectLight(pbr);

                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
}
