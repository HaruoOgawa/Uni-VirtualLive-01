Shader "Hidden/FullScreen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile UNITY_GAME_VIEW UNITY_SCENE_VIEW 

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                #if defined(UNITY_SCENE_VIEW)
                uv.y = 1.0 - uv.y;
                #endif

                fixed4 col = tex2D(_MainTex, uv);
                //col.rgb = float3(i.uv, 0.0);
                return col;
            }
            ENDCG
        }
    }
}
