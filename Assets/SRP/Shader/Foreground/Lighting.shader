Shader "Unlit/Lighting"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags{
                "LightMode" = "SRPDefaultUnlit"
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

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

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = mul(UNITY_MATRIX_M, float4(v.normal, 0.0));
                o.worldPos = mul(UNITY_MATRIX_M, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float3 L = _WorldSpaceLightPos0.xyz;
                float3 N = i.worldNormal.xyz;
                float3 V = (-1.0) * normalize(i.worldPos - _WorldSpaceCameraPos);
                float3 H = N + V;

                float3 DiffuseCol = max(0.0, dot(L, N)) * _LightColor0.rgb;
                float3 SpecularCol = pow(max(0.0, dot(H, L)), 10.0) * float3(1.0, 1.0, 1.0);

                col.rgb =  DiffuseCol;
                return col;
            }
            ENDCG
        }
    }
}
