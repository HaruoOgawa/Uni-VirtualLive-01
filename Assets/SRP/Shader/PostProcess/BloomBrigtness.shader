Shader "SRP/BloomBrigtness"
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
            #pragma fragment fragBrigtness

            #include "../ShaderLibrary/UnityInput.hlsl"
            #include "./PostProcessCommon.hlsl"
            #include "./Brigtness.hlsl"
            ENDHLSL
        }
    }
}
