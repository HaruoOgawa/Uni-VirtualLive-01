Shader "SRP/FXAA_PostProcess"
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
            #include "./FXAA.hlsl"

            sampler2D _MainTex;

            float4 frag (v2f i) : SV_Target
            {
                float2 st = i.uv;
                st.y = 1.0 - st.y;

                float4 col = tex2D(_MainTex, st);
                // just invert the colors
                col.rgb = 1 - col.rgb;

                return col;
            }
            ENDHLSL
        }
    }
}
