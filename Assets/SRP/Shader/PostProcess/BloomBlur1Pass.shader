Shader "SRP/BloomBlur1Pass"
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
            #pragma fragment fragBlur1Pass

            #include "../ShaderLibrary/UnityInput.hlsl"
            #include "./PostProcessCommon.hlsl"
            #include "./Blur1Pass.hlsl"

            ENDHLSL
        }
    }
}