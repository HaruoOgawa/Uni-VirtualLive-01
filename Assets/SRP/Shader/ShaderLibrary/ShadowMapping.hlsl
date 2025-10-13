#include "UnityInput.hlsl"

float CalcShadow(sampler2D shadowMap, float4 lightProjPos, float3 normal, float3 lightDir)
{
    float3 lightNdc = lightProjPos.xyz / lightProjPos.w;
    
    float2 lightUV = lightNdc.xy * 0.5 + 0.5;
    float lightDist = lightNdc.z;
    
    float Result = 1.0;
    
    return Result;
}