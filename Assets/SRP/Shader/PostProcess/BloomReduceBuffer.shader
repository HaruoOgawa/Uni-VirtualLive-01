Shader "SRP/BloomReduceBuffer"
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
            #pragma fragment fragReduceBuffer

            #include "../ShaderLibrary/UnityInput.hlsl"
            #include "./PostProcessCommon.hlsl"
            #include "./ReduceBuffer.hlsl"

            ENDHLSL
        }
    }
}