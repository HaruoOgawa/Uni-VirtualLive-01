Shader "MMDLib/BasicToon"
{
    Properties
    {
        _DiffuseFactor("DiffuseFactor", Color) = (1.0, 1.0, 1.0, 1.0)
        _AmbientFactor("AmbientFactor", Color) = (0.0, 0.0, 0.0, 0.0)
        _SpecularFactor("SpecularFactor", Color) = (1.0, 1.0, 1.0, 1.0)
        _EdgeColor("EdgeColor", Color) = (1.0, 1.0, 1.0, 1.0)

        _EdgeSize("EdgeSize", Float) = 1.0
        _SpecularIntensity("SpecularIntensity", Float) = 1.0

        _SphereMode("SphereMode", Int) = 0

        _MainTexture("MainTexture", 2D) = "white"{}
        _ToonTexture("ToonTexture", 2D) = "white"{}
        _SphereTexture("SphereTexture", 2D) = "white"{}

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Integer) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc("BlendSrc", Integer) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDst("BlendDst", Integer) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque"}

        Cull [_Cull]
        Blend [_BlendSrc] [_BlendDst]

        Pass
        {
            Tags{ "LightMode" = "SRPDefaultUnlit" }

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
                float3 WorldNormal : TEXCOORD1;
                float3 WorldPos : TEXCOORD2;
            };

            float _EdgeSize;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.WorldNormal = ( mul(unity_ObjectToWorld, float4(IN.normal,     0.0)) ).xyz;
                OUT.WorldPos =    ( mul(unity_ObjectToWorld, float4(IN.positionOS.xyz, 1.0)) ).xyz;
                return OUT;
            }
             
            float4 _DiffuseFactor;
            float4 _AmbientFactor;
            float4 _SpecularFactor;
            float4 _EdgeColor;
            float _SpecularIntensity;
            int _SphereMode;

            sampler2D _MainTexture;
            sampler2D _ToonTexture;
            sampler2D _SphereTexture;

            half4 frag(Varyings IN) : SV_Target
            {
                float4 col = float4(1.0, 1.0, 1.0, 1.0);

                float3 TmpLight = normalize(float3(1.0, -1.0, 1.0));

                float NdL = max(0.0, dot(IN.WorldNormal, -TmpLight));

                float3 v = normalize(_WorldSpaceCameraPos - IN.WorldPos.xyz);
	            float3 l = (-1.0) * TmpLight.xyz;
	            float3 h = normalize(v + l);

                float4 diffuseColor = _DiffuseFactor;

                // Ambient
                diffuseColor.rgb += _AmbientFactor.rgb;

                diffuseColor = clamp(diffuseColor, 0.0, 1.0);

                // MainTexture
                col = diffuseColor * tex2D(_MainTexture, IN.uv);

                // SphereMap
                float4 SphereColor = tex2D(_SphereTexture, IN.uv);

                if(_SphereMode == 1)
                {
                    col.rgb *= SphereColor.rgb;
                }
                else if(_SphereMode == 2)
                {
                    col.rgb += SphereColor.rgb;
                }

                // Toon
                float3 ToonColor = tex2D(_ToonTexture, IN.uv).rgb;
                col.rgb *= lerp(ToonColor, float3(1.0, 1.0, 1.0), clamp(NdL * 16.0 + 0.5, 0.0, 1.0));
                    
                // Specular

                return col;
            }
            ENDHLSL
        }
    }
}
