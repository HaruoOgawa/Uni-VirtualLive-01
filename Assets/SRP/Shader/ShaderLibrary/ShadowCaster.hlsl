#include "UnityInput.hlsl"

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float3 worldNormal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
};

v2f shadowVert(appdata v)
{
    v2f o;
    o.vertex = mul(unity_MatrixVP, mul(unity_ObjectToWorld, v.vertex));
    
    return o;
}

float4 shadowFrag(v2f o) : SV_Target
{
    return float4(0.0, 0.0, 0.0, 0.0);

}