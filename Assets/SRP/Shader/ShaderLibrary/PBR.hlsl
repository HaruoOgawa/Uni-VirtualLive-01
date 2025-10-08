// 最低反射率
// 非金属でも0.04%は鏡面反射する
#define MIN_REFLECTIVITY 0.04

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
    float3 col = pbr.Albedo * OneMinusReflectivity;
    return col;
}

// スペキュラーBRDF(鏡面反射)
float3 CalcSpecularBRDF(PBRData pbr, LightData light, float NdL)
{
    // クックトランスモデルによるスペキュラーGGX計算
    //float D = CalcMicrofacet
    
    float3 col = float3(0.0, 0.0, 0.0);
    return col;
}

// 最適化されたスペキュラーBRDF(鏡面反射)
float3 CalcOptimizedSpecularBRDF(PBRData pbr, float NdH, float LdH)
{
    float3 specularColor = lerp(float3(MIN_REFLECTIVITY, MIN_REFLECTIVITY, MIN_REFLECTIVITY), pbr.Albedo, pbr.Metallic);
    
    float r = pbr.Roughness - MIN_REFLECTIVITY;
    
    float a = r * r;
    float a2 = max(0.001, a * a);
    float d = NdH * NdH * (a2 - 1.0) + 1.00001;
    float specularTerm = a / (max(0.32, LdH) * (1.5 + a) * d);
    
    float3 spec = specularTerm * specularColor;
    spec = max(float3(0.0, 0.0, 0.0), spec);
    
    return spec;
}

// 直接光のPBR
float3 ComputeDirectLight(PBRData pbr, LightData light)
{
    float3 l = normalize(-light.dir);
    float3 v = normalize(-pbr.ViewDir);
    float3 n = normalize(pbr.WorldNormal);
    float3 h = normalize(l + v);
    
    float NdH = clamp(dot(n, h), 0.0, 1.0);
    float LdH = clamp(dot(l, h), 0.0, 1.0);
    float NdL = clamp(dot(n, l), 0.0, 1.0);
    
    // 拡散反射
    float3 DiffuseCol = CalcDiffuseBRDF(pbr);
    
    // 鏡面反射
    //float3 SpecularCol = CalcSpecularBRDF(pbr, light, NdL);
    float3 SpecularCol = CalcOptimizedSpecularBRDF(pbr, NdH, LdH);
    
    // 結果を組み合わせる
    float3 ResultCol = NdL * (DiffuseCol + SpecularCol);
    
    return ResultCol;
}

// 間接光のPBR
float3 ComputeIndirectLight(PBRData pbr, LightData light)
{
    float3 ResultCol = float3(0.0, 0.0, 0.0);

    return ResultCol;
}