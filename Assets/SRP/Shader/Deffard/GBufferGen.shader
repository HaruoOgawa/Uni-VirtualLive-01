Shader "CustomSRP/GBufferGen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Roughness("Roughness", Float) = 0.0
        _Metallic("Metallic", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "CustomGBufferGen" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _Roughness;
            float _Metallic;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = normalize((mul(UNITY_MATRIX_M, v.normal)).xyz);
                o.worldPos = (mul(UNITY_MATRIX_M, v.vertex)).xyz;
                return o;
            }

            struct FragOut
            {
                float4 col0 : SV_Target0; // BaseColor.rgb   Roughness.a
                float4 col1 : SV_Target1; // WorldNormal.rgb Metallic.a
                float4 col2 : SV_Target2; // WorldPos.rgb    MaterialType.r
                float4 col3 : SV_Target3; // None.rgba
                float4 col4 : SV_Target4; // None.rgba
            };

            FragOut frag (v2f i) : SV_Target
            {
                float4 BaseCol = _Color * tex2D(_MainTex, i.uv);
                fixed MatType = 1.0; // PBR

                FragOut o;

                o.col0 = float4(BaseCol.rgb, _Roughness);
                o.col1 = float4(i.worldNormal, _Metallic);
                o.col2 = float4(i.worldPos, MatType);
                o.col3 = float4(0.0, 0.0, 0.0, 0.0);
                o.col4 = float4(0.0, 0.0, 0.0, 0.0);

                return o;
            }
            ENDCG
        }
    }
}
