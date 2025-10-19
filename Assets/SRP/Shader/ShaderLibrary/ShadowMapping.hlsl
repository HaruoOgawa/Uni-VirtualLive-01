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

float CalcShadow(sampler2D shadowMap, float2 shadowTexelSize, float4 lightProjPos, float3 normal, float3 lightDir)
{
    float3 lightNdc = lightProjPos.xyz / lightProjPos.w;
    
    float2 lightUV = lightNdc.xy * 0.5 + 0.5;
    
    // 左手系で手前が-1.0・奥が1.0になっているので深度の仕様「1.0(近い) ～ 0.0(遠い)」になるように補正
    float lightDist = lightNdc.z * 0.5 + 0.5;
    lightDist = 1.0 - lightDist;
    
    // 範囲外チェック
    bool outSide = (lightUV.x < 0.0 || lightUV.y < 0.0 || lightDist < 0.0) || (lightUV.x > 1.0 || lightUV.y > 1.0 || lightDist > 1.0);
    if (outSide) return 1.0;
    
    float2 moments = ComputePCF(shadowMap, lightUV, shadowTexelSize);
    
    // マッハバンド対策のShadow Bias
	// ShadowBiasとは深度のオフセットのこと
	// マッハバンドはShawMapの解像度により発生する。複数のフラグメントが光源から比較的離れている場合、深度マップから同じ値をサンプリングする可能性がある。
	// 光の入射角がオクルーダーの法線に対して斜めなとき、上記の理由から例えば少し深度が大きい隣の表面の深度をサンプリングしてしまい、結果ShadowMapの元の深度より大ききなってしまうことで縞々になる(大きいということは影になる, 黒色)
	// その対策でオクルーダーをほんの少しだけ手前にする。手前にすることでShadowmapよりも深度が小さくなるため影になりにくくなる
	// https://drive.google.com/file/d/1tyDT7xQVSYzKnZXt6vvDwt-rlWEjVGDP/view?usp=sharing
	// 床の法線とライト方向の成す角度が垂直になるほど、Biasを強くする
	// https://learnopengl.com/Advanced-Lighting/Shadows/Shadow-Mapping
    float ShadowBias = max(0.0, 0.001 * (1.0 - dot(normal, -lightDir)));
    
    //float distance = lightDist - ShadowBias;
    float distance = lightDist;
    
	// ShadowMapの深度よりも手前なので普通に描画する
    // Unity(DirectX系)は手前が1、後ろが0なのでOpenGLアプリとは逆になる
    if (distance >= moments.x)
    {
        return 1.0;
    }
	
    return 0.1;
}