struct appdata
{
    float3 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
};

v2f vertPostProcess(appdata v)
{
    v2f o;
    o.vertex = float4(v.vertex, 1.0);
    o.uv = v.uv;

    return o;
}