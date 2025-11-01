Shader "MMDLib/BasicToon"
{
    Properties
    {
        _DiffuseFactor("DiffuseFactor", Color) = (1.0, 1.0, 1.0, 1.0)
        _AmbientFactor("AmbientFactor", Color) = (1.0, 1.0, 1.0, 1.0)
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
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _EdgeSize;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
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
                float4 color = float4(1.0, 1.0, 1.0, 1.0);
                return color;
            }
            ENDHLSL
        }
    }
}
