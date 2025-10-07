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
    float e = 0.04;
    
    // Metallicをひっくり返して金属であるほど拡散反射色が乗らないようにする
    float OneMinusReflectivity = (1.0 - e) - pbr.Metallic * (1.0 - e);
    
    // 最終結果
    float3 col = pbr.Albedo * OneMinusReflectivity;
    return col;
}

// スペキュラーBRDF(鏡面反射)
float3 CalcSpecularBRDF(PBRData pbr, LightData light, float NdL)
{
    float3 col = float3(0.0, 0.0, 0.0);
    return col;
}

// 直接光のPBR
float3 ComputeDirectLight(PBRData pbr, LightData light)
{
    float3 ResultCol = float3(0.0, 0.0, 0.0);

    //
    float NdL = max(0.0, dot(pbr.WorldNormal, -light.dir));
    
    // 拡散反射
    float3 DiffuseCol = CalcDiffuseBRDF(pbr);
    
    // 鏡面反射
    float3 SpecularCol = CalcSpecularBRDF(pbr, light, NdL);
    
    // 結果を組み合わせる
    ResultCol = NdL * (DiffuseCol + SpecularCol);
    
    return ResultCol;
}

// 間接光のPBR
float3 ComputeIndirectLight(PBRData pbr, LightData light)
{
    float3 ResultCol = float3(0.0, 0.0, 0.0);

    return ResultCol;
}