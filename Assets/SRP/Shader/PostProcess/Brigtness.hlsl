#ifndef POSTPROCESS_BLOOM_BRIGTNESS
#define POSTPROCESS_BLOOM_BRIGTNESS

#include "./PostProcessCommon.hlsl"

sampler2D _MainTex;
float4 _TexelSize;
float _Threshold;
float _Intencity;

float4 fragBrigtness(v2f i) : SV_Target
{
    float2 st = i.uv;
    st.y = 1.0 - st.y;
        
    float4 BrigtnessCol = tex2D(_MainTex, st);
    BrigtnessCol.rgb = max(float3(0.0, 0.0, 0.0), BrigtnessCol.rgb - float3(_Threshold, _Threshold, _Threshold)) * _Intencity;

    return BrigtnessCol;

}
#endif // POSTPROCESS_BLOOM_BRIGTNESS