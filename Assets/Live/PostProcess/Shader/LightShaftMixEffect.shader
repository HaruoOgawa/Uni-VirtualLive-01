Shader "SRP/LightShaftMixEffect"
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

            #include "../../../SRP/Shader/ShaderLibrary/UnityInput.hlsl"
            #include "../../../SRP/Shader/PostProcess/PostProcessCommon.hlsl"

            sampler2D _MainTex;
            sampler2D _LightShaftTex;

            float4 frag (v2f i) : SV_Target
            {
                float2 st = i.uv;
                st.y = 1.0 - st.y;

                float3 col = tex2D(_MainTex, st).rgb;
                float4 lightShaft = tex2D(_LightShaftTex, st);

                col += lightShaft.rgb;

                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}

