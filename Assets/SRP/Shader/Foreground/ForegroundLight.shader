Shader "CustomSRP/ForegroundLight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PlannerReflectMap ("PlannerReflectMap", 2D) = "white" {}
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Smoothness("Smoothness", Float) = 0.0
        _Metallic("Metallic", Float) = 0.0

        [Toggle] _UsePlannerReflect("UsePlannerReflect", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags{ "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile USE_PLANNER_REFLECT

            // UnityCG.cgincの代わりにUnityInput.hlslを使う。そうしないとPackagesフォルダをincludeしたときに重複定義でエラーになってしまう
            // このような書き方をしないと例えばPBR.hlslとかでリフレクションプローブのunity_SpecCube0が見えなくなる
            //#include "UnityCG.cginc"
            #include "../ShaderLibrary/UnityInput.hlsl"
            #include "../ShaderLibrary/PBR.hlsl"
            #include "../ShaderLibrary/ShadowMapping.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 projPos : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _PlannerReflectMap;
            float4 _Color;
            float _Smoothness;
            float _Metallic;

            int _UsePlannerReflect;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(unity_MatrixVP, mul(unity_ObjectToWorld, v.vertex));
                o.uv = v.uv;
                o.worldNormal = mul(unity_ObjectToWorld, float4(v.normal, 0.0));
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.projPos = o.vertex;
                return o;
            }

            #define MAX_MAIN_LIGHT_COUNT 4
            #define MAX_SUB_LIGHT_COUNT 64

            int SRP_Foreground_MainLightCount;
            float4 SRP_Foreground_MainLightDirArray[MAX_MAIN_LIGHT_COUNT];
            float4 SRP_Foreground_MainLightColorArray[MAX_MAIN_LIGHT_COUNT];

            int SRP_Foreground_SubLightCount;
            float4 SRP_Foreground_SubLightPosArray[MAX_SUB_LIGHT_COUNT];
            float4 SRP_Foreground_SubLightColorArray[MAX_SUB_LIGHT_COUNT];
            float4 SRP_Foreground_SubLightDirArray[MAX_SUB_LIGHT_COUNT];
            float4 SRP_Foreground_SubLightAngleArray[MAX_SUB_LIGHT_COUNT];

            // シャドウマッピング
            sampler2D SRP_ShadowMap;
            float4 SRP_ShadowTexelSize;
            float4x4 SRP_DirectionLight_ViewProjMatrix_List[MAX_MAIN_LIGHT_COUNT];
            float4x4 SRP_DirectionLight_LightUVBiasMatrix_List[MAX_MAIN_LIGHT_COUNT];

            float Square(float val)
            {
                return pow(val, 2.0);
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 col = float3(0.0, 0.0, 0.0);
                float alpha = 1.0;

                float4 Albedo = _Color * tex2D(_MainTex, i.uv);
                float3 WorldNormal = i.worldNormal.xyz;

                PBRData pbr;
                pbr.Albedo = Albedo.rgb;
                pbr.Metallic = _Metallic;
                pbr.Roughness = 1.0 - _Smoothness;
                pbr.WorldNormal = WorldNormal;
                pbr.ViewDir = normalize(i.worldPos.xyz - _WorldSpaceCameraPos);

                {
                    // MainLight
                    for(int n = 0; n < min(MAX_MAIN_LIGHT_COUNT, SRP_Foreground_MainLightCount); n++)
                    {
                        float3 lightDir = SRP_Foreground_MainLightDirArray[n].xyz;
                        float3 lightColor = SRP_Foreground_MainLightColorArray[n].xyz;

                        LightData light;
                        light.dir = lightDir;
                        light.color = lightColor;
                        light.attenuation = 1.0;

                        // シャドウマッピング
                        float4 lightProjPos = mul(SRP_DirectionLight_ViewProjMatrix_List[n], float4(i.worldPos.xyz, 1.0));
                        float4x4 LightUVBiasMatrix = SRP_DirectionLight_LightUVBiasMatrix_List[n];
                        float shadow = CalcShadow(SRP_ShadowMap, SRP_ShadowTexelSize.xy, lightProjPos, WorldNormal, lightDir, LightUVBiasMatrix);

                        // PBR
                        col.rgb += ComputeDirectLight(pbr, light) * shadow;
                    }

                    // SubLight
                    for(int n = 0; n < min(8, unity_LightData.y); n++)
                    { 
                        int lightIndex = unity_LightIndices[n / 4][n % 4];

                        float3 l2g = i.worldPos.xyz - SRP_Foreground_SubLightPosArray[lightIndex].xyz;

                        // ポイントライト・スポットライトの両方ともこれでライト方向を算出する
                        // 以前はスポットライトのライト方向にスポットライトの方向ベクトルを使っていたが、それは間違い
                        // スポットライトはポイントライトの球を扇形に切り取ったものとして捉える
                        // スポットライトのlightDirは減衰に使用
                        float3 lightDir = normalize(l2g);
                        float3 lightColor = SRP_Foreground_SubLightColorArray[lightIndex].rgb;

                        // 距離減衰
                        float distPow2 = max(dot(l2g, l2g), 0.001);
                        float distAttenuation = 1.0f / distPow2;

                        // 範囲減衰
                        // ポイントライト範囲の境界を自然に減衰させる
                        // 1. pow(distPow2 * SRP_Foreground_SubLightPosArray[lightIndex].w, 2.0)で 0 ～ 1 の線形な値を滑らかに上昇するようにする
                        // 2. -1.0をかけて反対の二次関数にして +1だけずらす
                        // 3. さらに2乗して滑らかにする
                        // N次関数は関数を滑らかにする
                        float rangeAttenuation = pow( saturate(1.0 - pow(distPow2 * SRP_Foreground_SubLightPosArray[lightIndex].w, 2.0) ) , 2.0);
                        
                        // 角度減衰
                        // 光はまっすぐ進むので光の進行方向から角度が離れるほど減衰していくと考える
                        // https://catlikecoding.com/unity/tutorials/custom-srp/point-and-spot-lights/
                        float angleAttenuation = Square(
                            saturate(
                                dot(SRP_Foreground_SubLightDirArray[lightIndex].xyz, lightDir) *
                                SRP_Foreground_SubLightAngleArray[lightIndex].x + SRP_Foreground_SubLightAngleArray[lightIndex].y
                            )
                        );

                        // 複数の減衰を組み合わせる
                        float Attenuation = rangeAttenuation * distAttenuation * angleAttenuation;

                        LightData light;
                        light.dir = lightDir;
                        light.color = lightColor;
                        light.attenuation = Attenuation;

                        col.rgb += ComputeDirectLight(pbr, light);
                    }
                }

                // 間接照明
                if(_UsePlannerReflect == 1)
                {
                    col.rgb += ComputeIndirectLightByPlannerReflection(pbr, _PlannerReflectMap, i.projPos);
                }
                else
                {
                    col.rgb += ComputeIndirectLight(pbr);
                }

                return float4(col, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Tags{ "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #include "../ShaderLibrary/ShadowCaster.hlsl"
            ENDHLSL
        }
    }
}
