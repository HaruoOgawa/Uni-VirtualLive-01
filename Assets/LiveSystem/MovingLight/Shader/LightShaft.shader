Shader "Custom/LightShaft"
{
    Properties
    {
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Power("Power", Float) = 1.0
        _Intensity("Intensity", Float) = 1.0
        _Height("Height", Float) = 10.0
        _BaseRadius("BaseRadius", Float) = 1.0
        _SpotAngle("SpotAngle", Range(0.0, 179.0)) = 45.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 localPos : TEXCOORD3;
                float3 localNormal : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Power;
                float _Intensity;
                float _Height;
                float _BaseRadius;
                float _SpotAngle;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                float rate = IN.uv.y;

                float4 pos = float4(IN.positionOS.xyz, 1.0);

                 // 正弦定理より半径を求める
                float height = _Height;

                float spotAngle = radians(_SpotAngle) * 0.5;
                float radius = (height / sin(3.1415 * 0.5 - spotAngle)) * sin(spotAngle);

                float sizeXY = _BaseRadius + radius * rate;

                float4x4 scaleMat = float4x4(
                    sizeXY, 0.0, 0.0, 0.0,
                    0.0, sizeXY, 0.0, 0.0,
                    0.0, 0.0, height, 0.0,
                    0.0, 0.0, 0.0,    1.0
                ); 

                pos = mul(scaleMat, pos);

                float3 normal = IN.normal;
                normal = normalize(mul(scaleMat, float4(normal, 0.0)).xyz);

                Varyings OUT;
                OUT.positionHCS = mul(UNITY_MATRIX_MVP, pos);
                OUT.uv = IN.uv;
                OUT.worldNormal = normalize(mul(unity_ObjectToWorld, float4(normal, 0.0)).xyz);
                OUT.worldPos = mul(unity_ObjectToWorld, pos).xyz;
                OUT.localPos = pos.xyz;
                OUT.localNormal = normal;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float rate = 1.0 - IN.uv.y;

                // 裏面描画で法線の向きが外を向いているのでViewDirは頂点座標からベクトルを出す
                float3 ViewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);
                // float3 ViewDir = normalize(IN.worldPos - _WorldSpaceCameraPos);
                ViewDir = normalize(mul(unity_WorldToObject, float4(ViewDir, 0.0)).xyz);

                float3 normal = IN.localNormal;

                float rim = pow(max(0.0, dot(normal, -ViewDir)), _Power);
                // float rim = pow(max(0.0, dot(normal.xy, -ViewDir.xy)), _Power);
                float mask = smoothstep(0.1, 1.0, rate);

                float4 col = _Color * _Intensity * rim * mask;

                /*float xylength = clamp(
                    length(mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1.0)).xy),
                    0.001,
                    1.0
                );
                col *= clamp(0.06 / xylength, 1.0, 2.0);*/

                col = clamp(col, 0.0, 3.0);

               return col;
            }
            ENDHLSL
        }
    }
}
