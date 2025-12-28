Shader "SRP/DiamondFlashEffect"
{
    Properties
    {
        _Rate("Rate", Range(0.0, 1.0)) = 0.0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vertPostProcess
            #pragma fragment frag

            #include "../../../SRP/Shader/ShaderLibrary/UnityInput.hlsl"
            #include "../../../SRP/Shader/PostProcess/PostProcessCommon.hlsl"

            sampler2D _MainTex;

            sampler2D SRP_GBuffer_0;
            sampler2D SRP_GBuffer_1;
            sampler2D SRP_GBuffer_2;
            sampler2D SRP_GBuffer_3;
            sampler2D SRP_GBuffer_4;

            float4 frag (v2f i) : SV_Target
            {
                float2 st = i.uv;
                st.y = 1.0 - st.y;

                float3 col = tex2D(_MainTex, st).rgb;
                // float3 col = tex2D(SRP_GBuffer_1, st).rgb;
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
