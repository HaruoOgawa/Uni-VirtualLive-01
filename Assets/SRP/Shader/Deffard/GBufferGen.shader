Shader "CustomSRP/GBufferGen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Smoothness("Smoothness", Float) = 0.0
        _Metallic("Metallic", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "CustomGBufferGen" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // UnityCG.cgincの代わりにUnityInput.hlslを使う。そうしないとPackagesフォルダをincludeしたときに重複定義でエラーになってしまう
            // このような書き方をしないと例えばPBR.hlslとかでリフレクションプローブのunity_SpecCube0が見えなくなる
            //#include "UnityCG.cginc"
            #include "../ShaderLibrary/UnityInput.hlsl"

            struct appdata
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;
                // NORMALセマンティックはfloat4でもコンパイルを通るが、wを0にするというのを明示的に書きたいのでfloat3と書く
                // セマンティックの型はメッシュデータの型と合わせる
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _Smoothness;
            float _Metallic;

            v2f vert (appdata v)
            {
                float4 pos = float4(v.vertex, 1.0);

                v2f o;
                o.vertex = mul(unity_MatrixVP, mul(unity_ObjectToWorld, pos));
                o.uv = v.uv;
                o.worldNormal = mul(unity_ObjectToWorld, float4(v.normal, 0.0));
                o.worldPos = (mul(unity_ObjectToWorld, pos)).xyz;
                return o;
            }

            struct FragOut
            {
                float4 col0 : SV_Target0; // Albedo.rgb   Roughness.a
                float4 col1 : SV_Target1; // WorldNormal.rgb Metallic.a
                float4 col2 : SV_Target2; // WorldPos.rgb    MaterialType.r
                float4 col3 : SV_Target3; // None.rgba
                float4 col4 : SV_Target4; // None.rgba
            };

            FragOut frag (v2f i) : SV_Target
            {
                float4 Albedo = _Color * tex2D(_MainTex, i.uv);
                float MatType = 1.0; // PBR
                float Roughness = 1.0 - _Smoothness;

                FragOut o;

                o.col0 = float4(Albedo.rgb, Roughness);
                o.col1 = float4(i.worldNormal.xyz, _Metallic);
                o.col2 = float4(i.worldPos, MatType);
                o.col3 = float4(0.0, 0.0, 0.0, 0.0);
                o.col4 = float4(0.0, 0.0, 0.0, 0.0);

                return o;
            }
            ENDHLSL
        }
    }
}
