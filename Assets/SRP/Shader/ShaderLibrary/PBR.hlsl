#ifndef SRP_CUSTOM_PBR
#define SRP_CUSTOM_PBR

// 最低反射率
// 非金属でも0.04%は鏡面反射する
#define MIN_REFLECTIVITY 0.04

#define PI 3.14159265

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
// mipindexを計算するPerceptualRoughnessToMipmapLevelを使うためにinclude
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"

// 以下定義やSAMPLE_TEXTURECUBE_LODはEntityLighting.hlsl経由で実装される
TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);

// PBR関連データ
struct PBRData
{
    float3 Albedo;
    float Metallic;
    float Roughness;
    float3 WorldNormal;
    float3 WorldPos;
    float3 ViewDir;
};

struct LightData
{
    float3 dir;
    float3 color;
    float attenuation;
};

struct PBRParam
{
    float3 Albedo;
    float Metallic;
    float Roughness;
    float NdH;
    float LdH;
    float NdL;
    float NdV;
    float VdH;
};

PBRParam CreatePBRParam(PBRData pbr, LightData light)
{
    // PBRParamを構築
    PBRParam param;
    
    float3 l = normalize(-light.dir);
    float3 v = normalize(-pbr.ViewDir);
    float3 n = normalize(pbr.WorldNormal);
    float3 h = normalize(l + v);
    
    float NdH = clamp(dot(n, h), 0.0, 1.0);
    float LdH = clamp(dot(l, h), 0.0, 1.0);
    float NdL = clamp(dot(n, l), 0.0, 1.0);
    float NdV = clamp(dot(n, v), 0.0, 1.0);
    float VdH = clamp(dot(v, h), 0.0, 1.0);
    
    param.Albedo = pbr.Albedo;
    param.Metallic = pbr.Metallic;
    
    // Roughnessが0.0の時の鏡面反射が消えてしまうので最小値を最小反射率にする
    param.Roughness = max(MIN_REFLECTIVITY, pbr.Roughness);
    
    param.NdH = NdH;
    param.LdH = LdH;
    param.NdL = NdL;
    param.NdV = NdV;
    param.VdH = VdH;
    
    return param;
}

// マイクロファセット(微小面法線分布関数)(Microfacet Distribution). Distributionは分布に意味
// 分布関数なので統計学的に求められた数式
// マイクロファセット → 微小平面
// 特定のハーフベクトル方向を向いたマイクロファセットの面積密度を表す
// つまりその方向を向いてるマイクロファセットが表面上にどれぐらい存在するか
// https://learnopengl.com/PBR/Theory#:~:text=GGX%20for%20G.-,Normal%20distribution%20function,-The%20normal%20distribution
float CalcMicrofacet(PBRParam param)
{
    float r = param.Roughness;
    //float a = r * r;
    float a = r;
    float a2 = max(0.001, a * a);
    
    float f = pow(param.NdH, 2.0) * (a2 - 1.0) + 1.0;
    
    return a2 / (PI * f * f);
}

// 幾何減衰項(Geometric Occlusion)
// マイクロファセットの微小平面が光の経路を遮断することにより失われてしまう光の減衰量を計算する関数
// → マイクロファセットの自己遮断・相互遮断によって反射に寄与できるマイクロファセットの割合を減衰させる関数
float CalcGeometricOcculusion(PBRParam param)
{
    //float k = param.Roughness * param.Roughness;
    float k = param.Roughness;

    // 実際の数式を簡略した方を使う
    float attenuationL = param.NdV / (param.NdV * (1.0 - k) + k);
    float attenuationV = param.NdL / (param.NdL * (1.0 - k) + k);

    return attenuationL * attenuationV;
}

// フレネル項
float3 CalcFrenelReflection(float3 Albedo, float Metallic, float NdV)
{
    // フレネル反射は視線ベクトルと法線の角度が大きいほど(斜めから見るほど)明るくなる現象
    float3 F0 = lerp(float3(MIN_REFLECTIVITY, MIN_REFLECTIVITY, MIN_REFLECTIVITY), Albedo, Metallic);
    return F0 + (1.0 - F0) * pow(1.0 - NdV, 5.0);
}

// ディフューズBRDF(拡散反射)
float3 CalcDiffuseBRDF(PBRData pbr)
{
    // どれだけ金属であるかを表すMetallic(0.0 ~ 1.0)の値を反転させたものを拡散反射色に乗算して金属であるほどこの色が乗らないようにする
    // 金属はほとんどが鏡面反射で拡散反射しなくなるのを表現する
    
    // 非金属の反射率は0.04(非金属でも0.04%は鏡面反射する)
    // これを考慮して拡散反射光が乗る割合の範囲を 0.0 ~ 0.96 にする
    // Metallicをひっくり返して金属であるほど拡散反射色が乗らないようにする
    float OneMinusReflectivity = (1.0 - MIN_REFLECTIVITY) - pbr.Metallic * (1.0 - MIN_REFLECTIVITY);
    
    // 最終結果
    // 最小値を0にしないと値がマイナスになって複数ライトを加算してもマイナスから復帰しなくて色がでなくなる
    float3 col = pbr.Albedo * OneMinusReflectivity;
    col.r = max(0.0, col.r);
    col.g = max(0.0, col.g);
    col.b = max(0.0, col.b);
    
    return col;
}

// スペキュラーBRDF(鏡面反射)
float3 CalcSpecularBRDF(PBRParam param)
{
    // クックトランスモデルによるスペキュラーGGX計算
    float  D = CalcMicrofacet(param); // 微小面法分布関数
    float  G = CalcGeometricOcculusion(param); // 幾何減衰項
    float3 F = CalcFrenelReflection(param.Albedo, param.Metallic, param.NdV); // フレネル項
    
    // 最小値を0にしないと値がマイナスになって複数ライトを加算してもマイナスから復帰しなくて色がでなくなる
    float3 spec = (D * G * F) / (4.0 * param.NdV * param.NdL);
    spec.r = max(0.0, spec.r);
    spec.g = max(0.0, spec.g);
    spec.b = max(0.0, spec.b);
    
    return spec;
}

// 最適化されたスペキュラーBRDF(鏡面反射)
float3 CalcOptimizedSpecularBRDF(PBRParam param)
{
    // Metallic 0 の時の色がMIN_REFLECTIVITYなのは、全く鏡面反射しなくても非金属の最低反射率0.04%だけでも光らせるため
    // 1の時はオブジェクト表面(吸収されない光)の色が鏡面反射光の色となる
    float3 specularColor = lerp(float3(MIN_REFLECTIVITY, MIN_REFLECTIVITY, MIN_REFLECTIVITY), param.Albedo, param.Metallic);
    
    float r = param.Roughness;
    float a = r * r;
    float a2 = max(0.001, a * a);
    float d = param.NdH * param.NdH * (a2 - 1.0) + 1.00001;
    float specularTerm = a / (max(0.32, param.LdH) * (1.5 + a) * d);
    
    // 最小値を0にしないと値がマイナスになって複数ライトを加算してもマイナスから復帰しなくて色がでなくなる
    float3 spec = specularTerm * specularColor;
    spec = max(float3(0.0, 0.0, 0.0), spec);
    
    spec.r = max(0.0, spec.r);
    spec.g = max(0.0, spec.g);
    spec.b = max(0.0, spec.b);
    
    return spec;
}

// 直接光のPBR
float3 ComputeDirectLight(PBRData pbr, LightData light)
{
    // PBRParamを構築
    PBRParam param = CreatePBRParam(pbr, light);
    
    // 拡散反射
    float3 DiffuseCol = CalcDiffuseBRDF(pbr);
    
    // 鏡面反射
    //float3 SpecularCol = CalcSpecularBRDF(param);
    float3 SpecularCol = CalcOptimizedSpecularBRDF(param);
    
    // 結果を組み合わせる
    float3 ResultCol = param.NdL * (DiffuseCol + SpecularCol) * light.color * light.attenuation;
    
    return ResultCol;
}


float3 CalcReflectionProbe(PBRData pbr)
{
    float3 v = normalize(pbr.ViewDir);
    float3 reflectV = reflect(v, pbr.WorldNormal);
    
    float mipindex = PerceptualRoughnessToMipmapLevel(pbr.Roughness);
    
    float4 col = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectV, mipindex);
    return col.rgb;
}

// 間接光のPBR
float3 ComputeIndirectLight(PBRData pbr)
{
    float3 ResultCol = float3(0.0, 0.0, 0.0);
    
    // リフレクションプローブによる間接照明
    ResultCol += CalcReflectionProbe(pbr);
    
    // フレネル反射
    float3 v = normalize(-pbr.ViewDir);
    float3 n = normalize(pbr.WorldNormal);
    
    float NdV = clamp(dot(n, v), 0.0, 1.0);
    
    ResultCol *= CalcFrenelReflection(pbr.Albedo, pbr.Metallic, NdV);
    
    return ResultCol;
}
#endif