Shader "Custom/GPUAudience"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

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
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            struct BoneWeightIndex
            {
                int4 Index;
                float4 Weight;
            };

            float4x4 ParentWorldMatrix;
            StructuredBuffer<float4x4> WorldMatrixList;
            
            StructuredBuffer<float4x4> BindPoseList;
            StructuredBuffer<BoneWeightIndex> BoneWeightIndexList;

            Varyings vert(Attributes IN)
            {
                float4 worldPos = mul(ParentWorldMatrix, float4(IN.positionOS.xyz, 1.0));

                Varyings OUT;
                OUT.positionHCS = mul(mul(UNITY_MATRIX_P, UNITY_MATRIX_V), worldPos);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 color = float4(1.0, 1.0, 1.0, 1.0);
                return color;
            }
            ENDHLSL
        }
    }
}
