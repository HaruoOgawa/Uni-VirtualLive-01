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
            #pragma fragment fragFXAA

            #include "../ShaderLibrary/UnityInput.hlsl"
            #include "./PostProcessCommon.hlsl"
            #include "./FXAA.hlsl"

            ENDHLSL
        }
    }
}
