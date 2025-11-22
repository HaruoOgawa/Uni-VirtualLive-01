Shader "CustomSRP/PlannerReflection"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white"
    }

    SubShader
    {
        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            sampler2D _BaseMap;

            Varyings vert(Attributes IN)
            {
                float4 ScreenPos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, float4(IN.positionOS.xyz, 1.0)) );
                
                Varyings OUT;
                OUT.positionHCS = ScreenPos;
                OUT.screenPos = ScreenPos;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 NdcUV = (IN.screenPos.xy / IN.screenPos.w) * 0.5 + 0.5;
                NdcUV.y = 1.0 - NdcUV.y;

                // NdcUV = NdcUV * 2.0 - 1.0;

                float4 col = tex2D(_BaseMap, NdcUV);

                // col.rgb = float3(NdcUV, 0.0);

                // if(length(NdcUV) < 0.5) col.rgb = float3(1.0, 1.0, 1.0);

                return col;
            }
            ENDHLSL
        }
    }
}
