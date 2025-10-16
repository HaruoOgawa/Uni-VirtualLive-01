Shader "SRP/BloomMix"
{
    Properties
    {
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

            #include "../ShaderLibrary/UnityInput.hlsl"
            #include "./PostProcessCommon.hlsl"

            sampler2D _MainTex;
            sampler2D _BloomImage;

            float4 frag (v2f i) : SV_Target
            {
                float2 st = i.uv;
                st.y = 1.0 - st.y;

                float3 col = tex2D(_MainTex, st).rgb;
                col += tex2D(_BloomImage, st).rgb;
                
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
