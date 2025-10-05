Shader "CustomSRP/ForegroundLight"
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
            Tags{ "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            // unity_LightDataとunity_LightIndicesはRendererListDesc.rendererConfigurationにPerObjectDataを設定したうえで
            // さらにこのようにシェーダーに宣言を書かないと使えない(ビルトイン変数のように勝手に用意してはくれない)
            half4 unity_LightData;
            half4 unity_LightIndices[2];

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = mul(UNITY_MATRIX_M, float4(v.normal, 0.0));
                o.worldPos = mul(UNITY_MATRIX_M, v.vertex);
                return o;
            }

            #define MAX_MAIN_LIGHT_COUNT 8
            #define MAX_SUB_LIGHT_COUNT 64

            int SRP_Foreground_MainLightCount;
            float4 SRP_Foreground_MainLightDirArray[MAX_MAIN_LIGHT_COUNT];
            float4 SRP_Foreground_MainLightColorArray[MAX_MAIN_LIGHT_COUNT];

            int SRP_Foreground_SubLightCount;
            float4 SRP_Foreground_SubLightPosArray[MAX_SUB_LIGHT_COUNT];
            float4 SRP_Foreground_SubLightColorArray[MAX_SUB_LIGHT_COUNT];
            float4 SRP_Foreground_SubLightDirArray[MAX_SUB_LIGHT_COUNT];
            float4 SRP_Foreground_SubLightAngleArray[MAX_SUB_LIGHT_COUNT];

            float Square(float val)
            {
                return pow(val, 2.0);
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
                float3 col = float3(0.0, 0.0, 0.0);
                float alpha = 1.0;

                float3 N = i.worldNormal.xyz;

                {
                    // MainLight
                    for(int n = 0; n < SRP_Foreground_MainLightCount; n++)
                    {
                        float3 lightDir = SRP_Foreground_MainLightDirArray[n].xyz;
                        float3 lightColor = SRP_Foreground_MainLightColorArray[n].xyz;

                        col.rgb += ComputeLight(N, lightDir, lightColor, 1.0);
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

                        col.rgb += ComputeLight(N, lightDir, lightColor, Attenuation);
                    }
                }

                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
}
