Shader "Custom/GPUAudience"
{
    Properties
    {
        [HDR] _EmitColor("EmitColor", Color) = (0.0, 0.0, 0.0, 0.0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint vertexID : SV_VertexID;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            struct BoneWeightIndex
            {
                float4 Weight;
                int4 Index;
            };

            float4x4 ParentWorldMatrix;
            StructuredBuffer<float4x4> WorldMatrixList;
            
            StructuredBuffer<float4x4> InvBindPoseList;
            StructuredBuffer<BoneWeightIndex> BoneWeightIndexList;

            sampler2D _VAT;
            float2 _VAT_TexelSize;

            int _RowCount;
            float _StartTime;
            float _EndTime;
            int _NumOfFrame;

            float4 _EmitColor;

            float4 fetchElement(float JointIndex, int Offset, float v)
            {
                // テクスチャに焼いたデータを使う時はテクセルの中心からサンプリングすることを心がける(texelSizeX * 0.5 を足す)
                // これを足さない時の値はテクセルの左下、つまりテクセルとテクセルの境界線を差している
                // もしこのままだと補完時にとなりのテクセルの影響を受けて、意図しない値が返ってくることがある
                // この結果、ボーンの動きがブレたり、不安定になるといったことが起こる。
                // VATにはGL_RGBA32F, GL_FLOATのテクスチャに対して、GL_NEAREST・CLAMP_TO_EDGEのサンプラーを適応している
                // GL_NEARESTはサンプリングに最も近い値に補完して返す機能だが、テクセル境界のままだとこれで混ざってしまう
                // 用語整理 /////
                // - ピクセル: 画面上の最小単位(ディスプレイのドット)
                // - テクセル: テクスチャ画像内の最小単位(テクスチャのピクセル)
                ////////////////
                float texelSizeX = _VAT_TexelSize.x;

                float2 st = float2((float(JointIndex * 4 + Offset) + 0.5) * texelSizeX, v);

                float4 val = tex2Dlod(_VAT, float4(st, 0.0, 0.0));

                return val;
            }

            float4x4 GetSkinMatFromVAT(uint JointIndex, int FrameIndex)
            {
                float f_JointIndex = float(JointIndex);
    
                // テクスチャに焼いたデータを使う時はテクセルの中心からサンプリングすることを心がける(texelSizeX * 0.5 を足す)
                // これを足さない時の値はテクセルの左下、つまりテクセルとテクセルの境界線を差している
                // もしこのままだと補完時にとなりのテクセルの影響を受けて、意図しない値が返ってくることがある
                // この結果、ボーンの動きがブレたり、不安定になるといったことが起こる。
                // VATにはGL_RGBA32F, GL_FLOATのテクスチャに対して、GL_NEAREST・CLAMP_TO_EDGEのサンプラーを適応している
                // GL_NEARESTはサンプリングに最も近い値に補完して返す機能だが、テクセル境界のままだとこれで混ざってしまう
                // 用語整理 /////
                // - ピクセル: 画面上の最小単位(ディスプレイのドット)
                // - テクセル: テクスチャ画像内の最小単位(テクスチャのピクセル)
                ////////////////
                float texelSizeY = _VAT_TexelSize.y;

                float v = (float(FrameIndex) + 0.5) * texelSizeY;

                // C#は列優先・ShaderLabは行優先なので転置する
                float4x4 SkinMatrix = transpose(float4x4(
                    fetchElement(f_JointIndex, 0, v),
                    fetchElement(f_JointIndex, 1, v),
                    fetchElement(f_JointIndex, 2, v),
                    fetchElement(f_JointIndex, 3, v)
                ));
    
                return SkinMatrix;
            }

            float rand(float2 st)
            {
	            return frac(sin(dot(st, float2(12.9898, 78.233))) * 43758.5453123) * 2.0 - 1.0;
            }

            Varyings vert(Attributes IN, uint id : SV_InstanceID)
            {
                BoneWeightIndex weightIndex = BoneWeightIndexList[IN.vertexID];

                float yid = floor(float(id) / float(_RowCount));
                float xid = float(id) - yid * float(_RowCount);
                xid = xid - float(_RowCount) * 0.5;

                // フレームを計算
                float LocalTime = fmod(_Time.y + rand(float2(xid, yid) * 0.5), _EndTime);
                int CurrentFrame = int(floor((LocalTime / _EndTime) * float(_NumOfFrame)));

                float4x4 SkinMatrix =
                    weightIndex.Weight.x * mul(GetSkinMatFromVAT(weightIndex.Index.x, CurrentFrame), InvBindPoseList[weightIndex.Index.x]) +
                    weightIndex.Weight.y * mul(GetSkinMatFromVAT(weightIndex.Index.y, CurrentFrame), InvBindPoseList[weightIndex.Index.y]) +
                    weightIndex.Weight.z * mul(GetSkinMatFromVAT(weightIndex.Index.z, CurrentFrame), InvBindPoseList[weightIndex.Index.z]) +
                    weightIndex.Weight.w * mul(GetSkinMatFromVAT(weightIndex.Index.w, CurrentFrame), InvBindPoseList[weightIndex.Index.w]);

                float4x4 WorldMatrix = mul(ParentWorldMatrix, mul(WorldMatrixList[id], SkinMatrix));

                float4 worldPos = mul(WorldMatrix, float4(IN.positionOS.xyz, 1.0));

                Varyings OUT;
                OUT.positionHCS = mul(mul(UNITY_MATRIX_P, UNITY_MATRIX_V), worldPos);
                OUT.uv = IN.uv;
                OUT.worldNormal = normalize((mul(WorldMatrix, float4(IN.normal, 0.0))).xyz);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 col = float4(0.0, 0.0, 0.0, 1.0);
                col.rgb += _EmitColor.rgb;

                return col;
            }
            ENDHLSL
        }
    }
}
