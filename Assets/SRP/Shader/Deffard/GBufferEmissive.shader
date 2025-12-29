Shader "CustomSRP/GBufferEmissive"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Front

        Pass
        {
            Tags { "LightMode" = "GBufferEmissivePass" }

            // ZTest GEqual
            ZTest Always
            ZWrite Off
            ZClip false
            Cull Front
            Blend One One, Zero One
            BlendOp Add, Add

            HLSLPROGRAM
            
            #pragma multi_compile
            
            #pragma vertex vert
            #pragma fragment frag

           // UnityCG.cgincの代わりにUnityInput.hlslを使う。そうしないとPackagesフォルダをincludeしたときに重複定義でエラーになってしまう
           // このような書き方をしないと例えばPBR.hlslとかでリフレクションプローブのunity_SpecCube0が見えなくなる
           //#include "UnityCG.cginc"
           #include "../ShaderLibrary/UnityInput.hlsl"
           #include "../ShaderLibrary/PBR.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 projPos : TEXCOORD0;
                float4 lightWorldPos : TEXCOORD1;
            };

            sampler2D SRP_GBuffer_0;
            sampler2D SRP_GBuffer_1;
            sampler2D SRP_GBuffer_2;
            sampler2D SRP_GBuffer_3;
            sampler2D SRP_GBuffer_4;

            v2f vert (appdata v)
            {
                v2f o;
                
                o.vertex = v.vertex;
                o.lightWorldPos = float4(0.0, 0.0, 0.0, 0.0);
                o.projPos = o.vertex;
                return o;
            }

            float4 CreateEmissiveColor(float2 screenUV)
            {
                float4 EmissiveColor = tex2D(SRP_GBuffer_4, screenUV); // EmissiveColor.rgba

                return EmissiveColor;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 NDCPos = i.projPos.xyz / i.projPos.w;

                float2 screenUV = NDCPos.xy * 0.5 + 0.5;
                screenUV.y = 1.0 - screenUV.y;
                float depth = NDCPos.z;

                float3 col = float3(0.0, 0.0, 0.0);
                float alpha = 1.0;

                col = CreateEmissiveColor(screenUV).rgb;

                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
}

