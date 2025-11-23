Shader "Custom/LightShaft"
{
    Properties
    {
        _Height("Height", Float) = 1.0
        _Radius("Radius", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Tags{ "Queue" = "Transparent" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            float _Height;
            float _Radius;

            Varyings vert(Attributes IN)
            {
                float rate = IN.uv.y;

                float4 pos = float4(IN.positionOS.xyz, 1.0);

                pos.z *= _Height;
                pos.xy *= lerp(1.0, _Radius, rate);

                Varyings OUT;
                OUT.positionHCS = mul(UNITY_MATRIX_MVP, pos);
                OUT.uv = IN.uv;
                OUT.worldNormal = normalize(mul(unity_ObjectToWorld, float4(IN.normal, 0.0)));
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 col = float4(1.0, 1.0, 1.0, 1.0);

                /*// カラーデバッグ
                float rate = IN.uv.y;
                col.rgb = float3(rate, rate, rate);
                if(rate >= 0.0 && rate < 0.25) col.rgb = float3(1.0, 0.0, 0.0);
                else if(rate >= 0.75 && rate <= 1.0) col.rgb = float3(0.0, 0.0, 1.0);*/

                col.rgb = IN.worldNormal;

                return col;
            }
            ENDHLSL
        }
    }
}
