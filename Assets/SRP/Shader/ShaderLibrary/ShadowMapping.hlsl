#include "UnityInput.hlsl"

float2 ComputePCF(sampler2D shadowMap, float2 uv, float2 shadowTexelSize)
{
    float2 moments = float2(0.0, 0.0);

    for (float x = -1.0; x <= 1.0; x++)
    {
        for (float y = -1.0; y <= 1.0; y++)
        {
            moments += tex2D(shadowMap, uv + float2(x, y) * shadowTexelSize).rg;
        }
    }
    
    moments /= 9.0;
    
    return moments;
}

float CalcShadow(sampler2D shadowMap, float2 shadowTexelSize, float4 lightProjPos, float3 normal, float3 lightDir, float4x4 LightUVBiasMatrix)
{
    float3 lightNdc = lightProjPos.xyz / lightProjPos.w;
    
    float2 lightUV = lightNdc.xy * 0.5 + 0.5;
    
    // 左手系で手前が-1.0・奥が1.0になっているので深度の仕様「1.0(近い) ～ 0.0(遠い)」になるように補正
    float lightDist = lightNdc.z * 0.5 + 0.5;
    lightDist = 1.0 - lightDist;
    
    // 範囲外チェック
    bool outSide = (lightUV.x < 0.0 || lightUV.y < 0.0 || lightDist < 0.0) || (lightUV.x > 1.0 || lightUV.y > 1.0 || lightDist > 1.0);
    if (outSide) return 1.0;
    
    //
    lightUV = ( mul(LightUVBiasMatrix, float4(lightNdc.xy, 0.0, 1.0f)) ).xy;
    lightUV = lightUV * 0.5 + 0.5;
    
    // PCF
    float2 moments = ComputePCF(shadowMap, lightUV, shadowTexelSize);
    
    // マッハバンド対策のShadow Bias
    float ShadowBias = 0.0001;
    
    float distance = lightDist + ShadowBias;
    
	// ShadowMapの深度よりも手前なので普通に描画する
    // Unity(DirectX系)は手前が1、後ろが0なのでOpenGLアプリとは逆になる
    if (distance >= moments.x)
    {
        return 1.0;
    }
	
    return 0.1;
}