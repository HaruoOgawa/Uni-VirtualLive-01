Shader "SRP/WhiteOutEffect"
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
            float _Rate;

            float4 frag (v2f i) : SV_Target
            {
                float2 st = i.uv;
                st.y = 1.0 - st.y;

                float3 col = tex2D(_MainTex, st).rgb;
                col = lerp(col, float3(1.0, 1.0, 1.0), _Rate);

                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
