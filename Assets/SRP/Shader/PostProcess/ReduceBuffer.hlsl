#include "./PostProcessCommon.hlsl"

sampler2D _MainTex;
float4 _TexelSize;

float4 fragReduceBuffer(v2f i) : SV_Target
{
    float3 col = float3(0.0, 0.0, 0.0);
    
    float2 st = i.uv;
    st.y = 1.0 - st.y;
	
    col += tex2D(_MainTex, st);

	// 2x2ピクセルを平均化して縮小
    col += tex2D(_MainTex, st + _TexelSize.xy * float2(-0.5, -0.5));
    col += tex2D(_MainTex, st + _TexelSize.xy * float2(-0.5, 0.5));
    col += tex2D(_MainTex, st + _TexelSize.xy * float2(0.5, -0.5));
    col += tex2D(_MainTex, st + _TexelSize.xy * float2(0.5, 0.5));

	// ピクセルの平均をとる
    col *= 0.2;

    return float4(col, 1.0);
}