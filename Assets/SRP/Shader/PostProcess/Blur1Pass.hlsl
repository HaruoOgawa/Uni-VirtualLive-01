#include "./PostProcessCommon.hlsl"

sampler2D _MainTex;
float4 _TexelSize;
int _IsXBlur;

float4 fragBlur1Pass(v2f i) : SV_Target
{
    float3 col = float3(0.0, 0.0, 0.0);
    
    float2 st = i.uv;
    st.y = 1.0 - st.y;

	// ƒKƒEƒXd‚İ‚ğŒvZ‚·‚é‚½‚ß‚ÌŒW”
    float weights[5] = { 0.227027, 0.316216, 0.070270, 0.002216, 0.000167 };

    float2 BlurDir = float2((_IsXBlur == 1) ? 1.0 : 0.0, (_IsXBlur == 1) ? 0.0 : 1.0);

    for (int i = -4; i <= 4; i++)
    {
        col += tex2D(_MainTex, st + _TexelSize.xy * i * BlurDir) * weights[abs(i)];
    }

    return float4(col, 1.0);

}