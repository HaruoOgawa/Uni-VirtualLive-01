using binary;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Profiling.Memory.Experimental;
using static TreeEditor.TextureAtlas;

namespace mmdlib
{
    public class CPmxModel
    {
        SPmxMetaData m_MetaData = new SPmxMetaData();
        CPmxMesh m_PmxMesh = null;
        List<CPmxTexture> m_PmxTextureList = new List<CPmxTexture>();
        List<CPmxMaterial> m_PmxMaterialList = new List<CPmxMaterial>();
        List<CPmxBone> m_PmxBoneList = new List<CPmxBone>();
        Dictionary<string, CPmxMorphTarget> m_PmxVertexMorphList = new Dictionary<string, CPmxMorphTarget>();
        List<SPmxRigidbody> m_PmxRigidbodyList = new List<SPmxRigidbody>();
        List<SPmxJoint> m_PmxJointList = new List<SPmxJoint>();

        public bool Analyse(string fileName)
        {
            CBinaryReader Analyser = new CBinaryReader();
            if (!Analyser.Init(fileName)) return false;

            // ヘッダが『PMX 』かどうか
            string header = "";
            if (!Analyser.GetString(ref header, 4)) return false;
            if (header != "PMX ")
            {
                Debug.LogError("Header is not pmx");
                return false;
            }

            // Version
            if (Analyser.GetPointer()[0] != 0x00 || Analyser.GetPointer()[1] != 0x00 || Analyser.GetPointer()[2] != 0x00 || Analyser.GetPointer()[3] != 0x40) return false;
            if (!Analyser.Skip(4)) return false;

            // メタデータ
            if (!AnalyseMetaData(ref Analyser))
            {
                Debug.LogError("[Error] Pmx AnalyseMetaData Error\n");

                return false;
            }

            // Mesh
            if (!AnalyseMesh(ref Analyser))
            {
                Debug.LogError("[Error] Pmx AnalyseMesh Error\n");

                return false;
            }

            // Texture
            if (!AnalyseTexture(ref Analyser))
            {
                Debug.LogError("[Error] Pmx AnalyseTexture Error\n");

                return false;
            }

            // Material
            if (!AnalyseMaterial(ref Analyser))
            {
                Debug.LogError("[Error] Pmx AnalyseMaterial Error\n");

                return false;
            }

            // Bone
            if (!AnalyseBone(ref Analyser))
            {
                Debug.LogError("[Error] Pmx AnalyseBone Error\n");

                return false;
            }

            // Morph
            if (!AnalyseMorph(ref Analyser))
            {
                Debug.LogError("[Error] Pmx AnalyseMorph Error\n");

                return false;
            }

            // DisplayFrame
            if (!AnalyseDisplayFrame(ref Analyser))
            {
                Debug.LogError("[Error] Pmx AnalyseDisplayFrame Error\n");

                return false;
            }

            // Rigidbody
            if (!AnalyseRigidbody(ref Analyser))
            {
                Debug.LogError("[Error] Pmx AnalyseRigidbody Error\n");

                return false;
            }

            // Joint
            if (!AnalyseJoint(ref Analyser))
            {
                Debug.LogError("[Error] Pmx AnalyseJoint Error\n");

                return false;
            }

            return true;
        }

        public SPmxMetaData GetMetaData()
        {
            return m_MetaData;
        }

        public CPmxMesh GetPmxMesh()
        {
            return m_PmxMesh;
        }

        public List<CPmxTexture> GetPmxTextureList()
        {
            return m_PmxTextureList;
        }

        public List<CPmxMaterial> GetPmxMaterialList()
        {
            return m_PmxMaterialList;
        }

        public List<CPmxBone> GetPmxBoneList()
        {
            return m_PmxBoneList;
        }

        public Dictionary<string, CPmxMorphTarget> GetPmxVertexMorphList()
        {
            return m_PmxVertexMorphList;
        }

        public List<SPmxRigidbody> GetPmxRigidbodyList()
        {
            return m_PmxRigidbodyList;
        }

        public List<SPmxJoint> GetPmxJointList()
        {
            return m_PmxJointList;
        }

        bool AnalyseMetaData(ref CBinaryReader Analyser)
        {
            // 後続のメタデータの長さ(PMX 2.0では8に固定)
            byte MetaSize = 0;
            if (!Analyser.GetByte(ref MetaSize)) return false;

            if (!Analyser.IsValid((byte)MetaSize)) return false;

            // メタデータを読む
            m_MetaData.EncodeType = (EPmxEncodeType)((int)Analyser.GetByte()); // エンコード方式
            m_MetaData.AdditionalUVCount = (int)(Analyser.GetByte()); // 追加UV数
            m_MetaData.VertexIndexSize = (int)(Analyser.GetByte()); // 頂点インデックスサイズ
            m_MetaData.TextureIndexSize = (int)(Analyser.GetByte()); // テクスチャインデックスサイズ
            m_MetaData.MaterialIndexSize = (int)(Analyser.GetByte()); // マテリアルインデックスサイズ
            m_MetaData.BoneIndexSize = (int)(Analyser.GetByte()); // ボーンインデックスサイズ
            m_MetaData.MorphIndexSize = (int)(Analyser.GetByte()); // モーフインデックスサイズ
            m_MetaData.RigidIndexSize = (int)(Analyser.GetByte()); // 剛体インデックスサイズ

            // モデル名
            {
                int ByteLength = 0;
                if (!Analyser.GetInt(ref ByteLength)) return false;

                if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                {
                    if (!Analyser.GetUTF16String(ref m_MetaData.ModelName, ByteLength)) return false;
                }
                else if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                {
                    if (!Analyser.GetString(ref m_MetaData.ModelName, ByteLength)) return false;
                }
            }

            // モデル名英
            {
                int ByteLength = 0;
                if (!Analyser.GetInt(ref ByteLength)) return false;

                if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                {
                    if (!Analyser.GetUTF16String(ref m_MetaData.ModelName_EN, ByteLength)) return false;
                }
                else if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                {
                    if (!Analyser.GetString(ref m_MetaData.ModelName_EN, ByteLength)) return false;
                }
            }

            // コメント
            {
                int ByteLength = 0;
                if (!Analyser.GetInt(ref ByteLength)) return false;

                if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                {
                    if (!Analyser.GetUTF16String(ref m_MetaData.Comment, ByteLength)) return false;
                }
                else if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                {
                    if (!Analyser.GetString(ref m_MetaData.Comment, ByteLength)) return false;
                }
            }

            // コメント英
            {
                int ByteLength = 0;
                if (!Analyser.GetInt(ref ByteLength)) return false;

                if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                {
                    if (!Analyser.GetUTF16String(ref m_MetaData.Comment_EN, ByteLength)) return false;
                }
                else if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                {
                    if (!Analyser.GetString(ref m_MetaData.Comment_EN, ByteLength)) return false;
                }
            }

            return true;
        }

        bool AnalyseMesh(ref CBinaryReader Analyser)
        {

            // 頂点バッファの読み込み
            List<float> PositionAttribute = new List<float>();
            List<float> NormalAttribute = new List<float>();
            List<float> UVAttribute = new List<float>();
            List<float> TangentAttribute = new List<float>();
            
            // m_MetaData.BoneIndexSizeに応じてバイト数が変わる
            List<uint> UIntBoneAttribute = new List<uint> ();
            List<byte> ByteBoneAttribute = new List<byte>();
            List<ushort> UShortBoneAttribute = new List<ushort>();

            List<float> WeightAttribute = new List<float>();

            List<List<float>> AdditionalUVAttribute = new List<List<float>>();
            for(int i = 0; i < m_MetaData.AdditionalUVCount; i++)
            {
                AdditionalUVAttribute.Add(new List<float>());
            }

            {
                int NumOfVertex = 0;
                if (!Analyser.GetInt(ref NumOfVertex)) return false;

                for (int VertexIndex = 0; VertexIndex < NumOfVertex; VertexIndex++)
                {
                    // 位置(x, y, z)
                    {
                        if (!Analyser.IsValid(4 * 3)) return false;

                        float x = Analyser.GetFloat();
                        float y = Analyser.GetFloat();
                        float z = Analyser.GetFloat();

                        PositionAttribute.Add(x);
                        PositionAttribute.Add(y);
                        PositionAttribute.Add(z);
                    }

                    // 法線(x, y, z)
                    {
                        if (!Analyser.IsValid(4 * 3)) return false;

                        float x = Analyser.GetFloat();
                        float y = Analyser.GetFloat();
                        float z = Analyser.GetFloat();

                        NormalAttribute.Add(x);
                        NormalAttribute.Add(y);
                        NormalAttribute.Add(z);
                    }

                    // UV(u, v)
                    {
                        if (!Analyser.IsValid(4 * 2)) return false;

                        float u = Analyser.GetFloat();
                        float v = Analyser.GetFloat();

                        // UVが上下反転しているので補正する
                        v = 1.0f - v;

                        UVAttribute.Add(u);
                        UVAttribute.Add(v);
                    }

                    // Additional UV(x, y, z, w) * n
                    for (int AddUVIndex = 0; AddUVIndex < m_MetaData.AdditionalUVCount; AddUVIndex++)
                    {
                        if (!Analyser.IsValid(4 * 4)) return false;

                        float x = Analyser.GetFloat();
                        float y = Analyser.GetFloat();
                        float z = Analyser.GetFloat();
                        float w = Analyser.GetFloat();

                        AdditionalUVAttribute[AddUVIndex].Add(x);
                        AdditionalUVAttribute[AddUVIndex].Add(y);
                        AdditionalUVAttribute[AddUVIndex].Add(z);
                        AdditionalUVAttribute[AddUVIndex].Add(w);
                    }

                    // Bones, Weghts
                    {
                        // ウェイト変形方式 0:BDEF1 1:BDEF2 2:BDEF4 3:SDEF
                        byte WeightFormatIndex = 0;
                        if (!Analyser.GetByte(ref WeightFormatIndex)) return false;

                        // Bones, Weghtsの格納方法
                        EPmxWeightDeformFormat WeightDeformFormat = (EPmxWeightDeformFormat)((int)(WeightFormatIndex));

                        if (WeightDeformFormat == EPmxWeightDeformFormat.BDEF1)
                        {
                            // BDEF1 : int 		| 4   | ボーンのみ
                            /*
                            n : ボーンIndexサイズ  | ウェイト1.0の単一ボーン(参照Index)
                            */
                            if (!GetMultiTypeValue(ref Analyser, m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;

                            WeightAttribute.Add(1.0f);

                            // あまりは0埋めする
                            if (!AddEmptyMultiTypeValue(m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;
                            if (!AddEmptyMultiTypeValue(m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;
                            if (!AddEmptyMultiTypeValue(m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;

                            WeightAttribute.Add(0.0f);
                            WeightAttribute.Add(0.0f);
                            WeightAttribute.Add(0.0f);
                        }
                        else if (WeightDeformFormat == EPmxWeightDeformFormat.BDEF2)
                        {
                            // BDEF2 : int,int,float 	| 4*3 | ボーン2つと、ボーン1のウェイト値(PMD方式)
                            /*
                              n : ボーンIndexサイズ  | ボーン1の参照Index
                              n : ボーンIndexサイズ  | ボーン2の参照Index
                              4 : float              | ボーン1のウェイト値(0～1.0), ボーン2のウェイト値は 1.0-ボーン1ウェイト
                            */
                            if (!GetMultiTypeValue(ref Analyser, m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;
                            if (!GetMultiTypeValue(ref Analyser, m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;

                            if (!Analyser.IsValid(4 * 1)) return false;

                            float WeightX = Analyser.GetFloat();
                            float WeightY = 1.0f - WeightX;

                            WeightAttribute.Add(WeightX);
                            WeightAttribute.Add(WeightY);

                            // あまりは0埋めする
                            if (!AddEmptyMultiTypeValue(m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;
                            if (!AddEmptyMultiTypeValue(m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;

                            WeightAttribute.Add(0.0f);
                            WeightAttribute.Add(0.0f);
                        }
                        else if (WeightDeformFormat == EPmxWeightDeformFormat.BDEF4)
                        {
                            // BDEF4 : int*4, float*4	| 4*8 | ボーン4つと、それぞれのウェイト値。ウェイト合計が1.0である保障はしない
                            /*
                              n : ボーンIndexサイズ  | ボーン1の参照Index
                              n : ボーンIndexサイズ  | ボーン2の参照Index
                              n : ボーンIndexサイズ  | ボーン3の参照Index
                              n : ボーンIndexサイズ  | ボーン4の参照Index
                              4 : float              | ボーン1のウェイト値
                              4 : float              | ボーン2のウェイト値
                              4 : float              | ボーン3のウェイト値
                              4 : float              | ボーン4のウェイト値 (ウェイト計1.0の保障はない)
                            */
                            if (!GetMultiTypeValue(ref Analyser, m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;
                            if (!GetMultiTypeValue(ref Analyser, m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;
                            if (!GetMultiTypeValue(ref Analyser, m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;
                            if (!GetMultiTypeValue(ref Analyser, m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;

                            if (!Analyser.IsValid(4 * 4)) return false;

                            float WeightX = Analyser.GetFloat();
                            float WeightY = Analyser.GetFloat();
                            float WeightZ = Analyser.GetFloat();
                            float WeightW = Analyser.GetFloat();

                            WeightAttribute.Add(WeightX);
                            WeightAttribute.Add(WeightY);
                            WeightAttribute.Add(WeightZ);
                            WeightAttribute.Add(WeightW);
                        }
                        else if (WeightDeformFormat == EPmxWeightDeformFormat.SDEF)
                        {
                            // SDEF  : int,int,float, float3*3 
                            //			| 4*12 | BDEF2に加え、SDEF用のfloat3(Vector3)が3つ。実際の計算ではさらに補正値の算出が必要(一応そのままBDEF2としても使用可能)
                            /*
                              n : ボーンIndexサイズ  | ボーン1の参照Index
                              n : ボーンIndexサイズ  | ボーン2の参照Index
                              4 : float              | ボーン1のウェイト値(0～1.0), ボーン2のウェイト値は 1.0-ボーン1ウェイト
                             12 : float3             | SDEF-C値(x,y,z)
                             12 : float3             | SDEF-R0値(x,y,z)
                             12 : float3             | SDEF-R1値(x,y,z) ※修正値を要計算
                            */

                            if (!GetMultiTypeValue(ref Analyser, m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;
                            if (!GetMultiTypeValue(ref Analyser, m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;

                            if (!Analyser.IsValid(4 * 10)) return false;

                            float WeightX = Analyser.GetFloat();
                            float WeightY = 1.0f - WeightX;

                            WeightAttribute.Add(WeightX);
                            WeightAttribute.Add(WeightY);

                            // あまりは0埋めする
                            if (!AddEmptyMultiTypeValue(m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;
                            if (!AddEmptyMultiTypeValue(m_MetaData.BoneIndexSize, ref UIntBoneAttribute, ref ByteBoneAttribute, ref UShortBoneAttribute)) return false;

                            WeightAttribute.Add(0.0f);
                            WeightAttribute.Add(0.0f);

                            // SDEF(未対応)
                            float SDEF_C_X = Analyser.GetFloat();
                            float SDEF_C_Y = Analyser.GetFloat();
                            float SDEF_C_Z = Analyser.GetFloat();

                            float SDEF_R0_X = Analyser.GetFloat();
                            float SDEF_R0_Y = Analyser.GetFloat();
                            float SDEF_R0_Z = Analyser.GetFloat();

                            float SDEF_R1_X = Analyser.GetFloat();
                            float SDEF_R1_Y = Analyser.GetFloat();
                            float SDEF_R1_Z = Analyser.GetFloat();
                        }
                        else
                        {
                            return false;
                        }
                    }

                    // EdgeScale
                    float EdgeScale = 1.0f;
                    if (!Analyser.GetFloat(ref EdgeScale)) return false;
                }
            }

            // インデックスバッファの読み込み
            List<uint> UIntIndices = new List<uint>();
            List<byte> ByteIndices = new List<byte>();
            List<ushort> UShortIndices = new List<ushort>();

            {
                int NumOfIndices = 0;
                if (!Analyser.GetInt(ref NumOfIndices)) return false;

                int VertexIndexSize = m_MetaData.VertexIndexSize;

                for (int i = 0; i < NumOfIndices; i++)
                {
                    if (!GetMultiTypeValue(ref Analyser, m_MetaData.VertexIndexSize, ref UIntIndices, ref ByteIndices, ref UShortIndices)) return false;
                }
            }

            m_PmxMesh = new CPmxMesh(PositionAttribute, NormalAttribute, UVAttribute, TangentAttribute, UIntBoneAttribute, ByteBoneAttribute, UShortBoneAttribute, WeightAttribute, AdditionalUVAttribute, UIntIndices, ByteIndices, UShortIndices);

            return true;
        }

        bool AnalyseTexture(ref CBinaryReader Analyser)
        {
            int NumOfTexture = 0;
            if (!Analyser.GetInt(ref NumOfTexture)) return false;

            for (int TextureIndex = 0; TextureIndex < NumOfTexture; TextureIndex++)
            {
                CPmxTexture PmxTexture = new CPmxTexture();

                string TextureName = string.Empty;

                int ByteLength = 0;
                if (!Analyser.GetInt(ref ByteLength)) return false;

                if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                {
                    if (!Analyser.GetUTF16String(ref TextureName, ByteLength)) return false;
                }
                else if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                {
                    if (!Analyser.GetString(ref TextureName, ByteLength)) return false;
                }

                PmxTexture.SetFilePath(TextureName);

                m_PmxTextureList.Add(PmxTexture);
            }

            return true;
        }

        bool AnalyseMaterial(ref CBinaryReader Analyser)
        {
            int NumOfMaterial = 0;
            if (!Analyser.GetInt(ref NumOfMaterial)) return false;

            for (int MaterialIndex = 0; MaterialIndex < NumOfMaterial; MaterialIndex++)
            {
                // MaterialName
                string MaterialName = string.Empty;
                {
                    int ByteLength = 0;
                    if (!Analyser.GetInt(ref ByteLength)) return false;

                    if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                    {
                        if (!Analyser.GetString(ref MaterialName, ByteLength)) return false;
                    }
                    else if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                    {
                        if (!Analyser.GetUTF16String(ref MaterialName, ByteLength)) return false;
                    }
                }

                // MaterialName_EN
                string MaterialName_EN = string.Empty;
                {
                    int ByteLength = 0;
                    if (!Analyser.GetInt(ref ByteLength)) return false;

                    if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                    {
                        if (!Analyser.GetString(ref MaterialName_EN, ByteLength)) return false;
                    }
                    else if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                    {
                        if (!Analyser.GetUTF16String(ref MaterialName_EN, ByteLength)) return false;
                    }
                }

                // Diffuse
                Vector4 Diffuse = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                {
                    if (!Analyser.IsValid(4 * 4)) return false;

                    float R = Analyser.GetFloat();
                    float G = Analyser.GetFloat();
                    float B = Analyser.GetFloat();
                    float A = Analyser.GetFloat();

                    Diffuse = new Vector4(R, G, B, A);
                }

                // Specular
                Vector4 Specular = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                {
                    if (!Analyser.IsValid(4 * 3)) return false;

                    float R = Analyser.GetFloat();
                    float G = Analyser.GetFloat();
                    float B = Analyser.GetFloat();

                    Specular = new Vector4(R, G, B, 1.0f);
                }

                // Specular係数
                float SpecularCoef = 1.0f;
                if (!Analyser.GetFloat(ref SpecularCoef)) return false;

                // Ambient
                Vector4 Ambient = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                {
                    if (!Analyser.IsValid(4 * 3)) return false;

                    float R = Analyser.GetFloat();
                    float G = Analyser.GetFloat();
                    float B = Analyser.GetFloat();

                    Ambient = new Vector4(R, G, B, 1.0f);
                }

                /*
                bitFlag  	| 描画フラグ(8bit) - 各bit 0:OFF 1:ON
                       0x01:両面描画, 0x02:地面影, 0x04:セルフシャドウマップへの描画, 0x08:セルフシャドウの描画, 
                       0x10:エッジ描画
                // CPmaxMaterial内で解析する
                */
                byte DrawBitFlag = 0;
                if (!Analyser.GetByte(ref DrawBitFlag)) return false;

                // エッジカラー
                Vector4 EdgeColor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                {
                    if (!Analyser.IsValid(4 * 4)) return false;

                    float R = Analyser.GetFloat();
                    float G = Analyser.GetFloat();
                    float B = Analyser.GetFloat();
                    float A = Analyser.GetFloat();

                    EdgeColor = new Vector4(R, G, B, A);
                }

                // エッジサイズ
                float EdgeSize = 1.0f;
                if (!Analyser.GetFloat(ref EdgeSize)) return false;

                // メインテクスチャの参照インデックス
                int MainTexIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.TextureIndexSize);

                // スフィアテクスチャの参照インデックス
                int SphereTexIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.TextureIndexSize);

                // スフィアモード 0:無効 1:乗算(sph) 2:加算(spa) 3:サブテクスチャ(追加UV1のx,yをUV参照して通常テクスチャ描画を行う)
                byte SphereModeByte = 0;
                if (!Analyser.GetByte(ref SphereModeByte)) return false;

                EPmxSphereMode SphereMode = (EPmxSphereMode)((int)SphereModeByte);

                // 共有Toonフラグ 0:継続値は個別Toon 1:継続値は共有Toon
                byte SharedToonFlag = 0;
                if (!Analyser.GetByte(ref SharedToonFlag)) return false;

                // トゥーンテクスチャ
                int ToonTexIndex = -1;
                int SharedToonTexIndex = -1;

                if (SharedToonFlag == 0)
                {
                    // Toonテクスチャ, テクスチャテーブルの参照Index
                    ToonTexIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.TextureIndexSize);
                }
                else if (SharedToonFlag == 1)
                {
                    // 共有Toonテクスチャ[0～9] -> それぞれ toon01.bmp～toon10.bmp に対応
                    byte SharedToonTexByte = 0;
                    if (!Analyser.GetByte(ref SharedToonTexByte)) return false;

                    SharedToonTexIndex = (int)(SharedToonTexByte);
                }
                else
                {
                    return false;
                }

                // メモ : 自由欄／スクリプト記述／エフェクトへのパラメータ配置など
                string MaterialDescription = string.Empty;
                {
                    int ByteLength = 0;
                    if (!Analyser.GetInt(ref ByteLength)) return false;

                    if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                    {
                        if (!Analyser.GetString(ref MaterialDescription, ByteLength)) return false;
                    }
                    else if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                    {
                        if (!Analyser.GetUTF16String(ref MaterialDescription, ByteLength)) return false;
                    }
                }

                // 材質に対応する面(頂点)数 (必ず3の倍数になる)
                int MatRefIndiceCount = 0;
                if (!Analyser.GetInt(ref MatRefIndiceCount)) return false;

                // マテリアルを登録
                CPmxMaterial PmxMaterial = new CPmxMaterial(MaterialName, MaterialName_EN, Diffuse, Specular, SpecularCoef, Ambient, DrawBitFlag, EdgeColor, EdgeSize,
                    MainTexIndex, SphereTexIndex, SphereMode, ToonTexIndex, SharedToonTexIndex, MaterialDescription, MatRefIndiceCount);

                m_PmxMaterialList.Add(PmxMaterial);
            }

            return true;
        }

        bool AnalyseBone(ref CBinaryReader Analyser)
        {
            int NumOfBone = 0;
		    if (!Analyser.GetInt(ref NumOfBone)) return false;

		    for (int BoneIndex = 0; BoneIndex < NumOfBone; BoneIndex++)
		    {
                // BoneName
                string BoneName = string.Empty;
                {
				    int ByteLength = 0;
				    if (!Analyser.GetInt(ref ByteLength)) return false;

				    if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
				    {
					    if (!Analyser.GetString(ref BoneName, ByteLength)) return false;
				    }
				    else if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
				    {
					    if (!Analyser.GetUTF16String(ref BoneName, ByteLength)) return false;
				    }
			    }

                // BoneName_EN
                string BoneName_EN = string.Empty;
                {
				    int ByteLength = 0;
				    if (!Analyser.GetInt(ref ByteLength)) return false;

				    if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
				    {
					    if (!Analyser.GetString(ref BoneName_EN, ByteLength)) return false;
				    }
				    else if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
				    {
					    if (!Analyser.GetUTF16String(ref BoneName_EN, ByteLength)) return false;
				    }
			    }

			    // 位置
			    Vector3 Pos = new Vector3(0.0f, 0.0f, 0.0f);
			    {
				    if (!Analyser.IsValid(4 * 3)) return false;

				    Pos.x = Analyser.GetFloat();
				    Pos.y = Analyser.GetFloat();
				    Pos.z = Analyser.GetFloat();
			    }

			    // 親ボーンのインデックス
			    int ParentBoneIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.BoneIndexSize);

			    // 変形階層
			    int DeformLayer = -1;
			    if (!Analyser.GetInt(ref DeformLayer)) return false;

			    // ボーンフラグ(16bit)
			    ushort BoneFlag = 0;
			    if (!Analyser.GetUShort(ref BoneFlag)) return false;

                EHumanoidBones humanoidBone = CPmxHumanoidBoneMapper.CastStringToBoneName(BoneName);

                // PmxBoneを作成
                CPmxBone PmxBone = new CPmxBone(BoneName, BoneName_EN, humanoidBone, Pos, ParentBoneIndex, DeformLayer, BoneFlag);

			    // ボーンフラグを見て処理を分ける
			    {
				    // 接続先
				    if ((BoneFlag & 0x0001) != 0)
				    {
					    // 接続先: 1
					    // 接続先ボーンのボーンIndex(ネットで調べるときは『表示先』と出る)
					    int ConnectBoneIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.BoneIndexSize);
				    }
				    else
				    {
					    // 接続先: 0
					    // 座標オフセット, ボーン位置からの相対分
					    if (!Analyser.IsValid(4 * 3)) return false;

					    Vector3 Offset = new Vector3(0.0f, 0.0f, 0.0f);

					    Offset.x = Analyser.GetFloat();
					    Offset.y = Analyser.GetFloat();
					    Offset.z = Analyser.GetFloat();

					    // Posにオフセットを追加する(いらないかも)
					    //Pos += Offset;
				    }

				    // 回転付与 または 移動付与 が 1
				    if ((BoneFlag & 0x0100) != 0 || (BoneFlag & 0x0200) != 0)
				    {
					    // 付与親ボーンのボーンIndex
					    int GrantParentBoneIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.BoneIndexSize);

					    // 付与率
					    float GrantRate = 0.0f;
					    if (!Analyser.GetFloat(ref GrantRate)) return false;

					    if ((BoneFlag & 0x0100) != 0)
					    {
						    // 回転付与
						    PmxBone.SetRotateGrant(GrantParentBoneIndex, GrantRate);

					    }
					    else if ((BoneFlag & 0x0200) != 0)
					    {
						    // 移動付与
						    PmxBone.SetMoveGrant(GrantParentBoneIndex, GrantRate);
					    }
				    }

				    // 軸固定:1 の場合
				    if ((BoneFlag & 0x0400) != 0)
				    {
					    if (!Analyser.IsValid(4 * 3)) return false;

					    Vector3 FixedAxisVector = new Vector3(0.0f, 0.0f, 0.0f);

					    FixedAxisVector.x = Analyser.GetFloat();
					    FixedAxisVector.y = Analyser.GetFloat();
					    FixedAxisVector.z = Analyser.GetFloat();
				    }

				    // ローカル軸:1 の場合
				    if ((BoneFlag & 0x0800) != 0)
				    {
					    if (!Analyser.IsValid(4 * 3 * 2)) return false;

					    //
					    Vector3 XAxisVector = new Vector3(0.0f, 0.0f, 0.0f);

					    XAxisVector.x = Analyser.GetFloat();
					    XAxisVector.y = Analyser.GetFloat();
					    XAxisVector.z = Analyser.GetFloat();

                        XAxisVector = XAxisVector.normalized;

                        //
                        Vector3 ZAxisVector = new Vector3(0.0f, 0.0f, 0.0f);

					    ZAxisVector.x = Analyser.GetFloat();
					    ZAxisVector.y = Analyser.GetFloat();
					    ZAxisVector.z = Analyser.GetFloat();

					    ZAxisVector = ZAxisVector.normalized;

					    //
					    PmxBone.SetLocalAxis(XAxisVector, ZAxisVector);
				    }

				    // 外部親変形:1 の場合
				    if ((BoneFlag & 0x2000) != 0)
				    {
					    int KeyIndex = -1;
					    if (!Analyser.GetInt(ref KeyIndex)) return false;
				    }

				    // IK:1 の場合 IKデータを格納
				    if ((BoneFlag & 0x0020) != 0)
				    {
					    // IKターゲットボーンのボーンIndex
					    int IKTargetBoneIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.BoneIndexSize);

					    // IKループ回数 (PMD及びMMD環境では255回が最大になるようです)
					    int IKLoopCount = 0;
					    if (!Analyser.GetInt(ref IKLoopCount)) return false;

					    // IKループ計算時の1回あたりの制限角度 -> ラジアン角 | PMDのIK値とは4倍異なるので注意
					    float LimitedAngle = 0.0f;
					    if (!Analyser.GetFloat(ref LimitedAngle)) return false;

					    // IKリンク数 : 後続の要素数
					    int IKLinkCount = 0;
					    if (!Analyser.GetInt(ref IKLinkCount)) return false;

					    List<SIKLink> IKLinkList = new List<SIKLink>();

					    for (int IKLinkIndex = 0; IKLinkIndex < IKLinkCount; IKLinkIndex++)
					    {
						    // リンクボーンのボーンIndex
						    int IKLinkBoneIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.BoneIndexSize);

						    // 角度制限 0:OFF 1:ON
						    byte IsLimitAngle = 0;
						    if (!Analyser.GetByte(ref IsLimitAngle)) return false;

						    bool UseLimitAngle = false;
						    Vector3 LowerAngle = new Vector3(0.0f, 0.0f, 0.0f);
						    Vector3 UpperAngle = new Vector3(0.0f, 0.0f, 0.0f);

						    if ((IsLimitAngle & 0x01) != 0)
						    {
							    if (!Analyser.IsValid(4 * 3 * 2)) return false;

							    UseLimitAngle = true;

							    LowerAngle.x = Analyser.GetFloat();
							    LowerAngle.y = Analyser.GetFloat();
							    LowerAngle.z = Analyser.GetFloat();

							    UpperAngle.x = Analyser.GetFloat();
							    UpperAngle.y = Analyser.GetFloat();
							    UpperAngle.z = Analyser.GetFloat();
						    }

						    // IKLinkを登録
						    IKLinkList.Add(new SIKLink(IKLinkBoneIndex, UseLimitAngle, LowerAngle, UpperAngle));
					    }

                        // IKParamを登録
                        SIKParam IKParam = new SIKParam(IKTargetBoneIndex, IKLoopCount, LimitedAngle, IKLinkList);

					    PmxBone.SetIKParam(IKParam);
				    }
			    }

			    // PmxBoneを登録
			    m_PmxBoneList.Add(PmxBone);
		    }

		    return true;
        }


        bool AnalyseMorph(ref CBinaryReader Analyser)
        {
            int NumOfMorph = 0;
            if (!Analyser.GetInt(ref NumOfMorph)) return false;

            for (int i = 0; i < NumOfMorph; i++)
            {
                // モーフ名
                string MorphName = string.Empty;
                {
                    int ByteLength = 0;
                    if (!Analyser.GetInt(ref ByteLength)) return false;

                    if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                    {
                        if (!Analyser.GetString(ref MorphName, ByteLength)) return false;
                    }
                    else if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                    {
                        if (!Analyser.GetUTF16String(ref MorphName, ByteLength)) return false;
                    }
                }

                // モーフ名英
                string MorphName_EN = string.Empty;
                {
                    int ByteLength = 0;
                    if (!Analyser.GetInt(ref ByteLength)) return false;

                    if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                    {
                        if (!Analyser.GetString(ref MorphName_EN, ByteLength)) return false;
                    }
                    else if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                    {
                        if (!Analyser.GetUTF16String(ref MorphName_EN, ByteLength)) return false;
                    }
                }

                // モーフオブジェクト
                CPmxMorphTarget PmxMorph = new CPmxMorphTarget(MorphName, MorphName_EN);

                // 操作パネル (PMD:カテゴリ) 1:眉(左下) 2:目(左上) 3:口(右上) 4:その他(右下)  | 0:システム予約
                byte OperatePanel = 0;
                if (!Analyser.GetByte(ref OperatePanel)) return false;

                // モーフ種類 - 0:グループ, 1:頂点, 2:ボーン, 3:UV, 4:追加UV1, 5:追加UV2, 6:追加UV3, 7:追加UV4, 8:材質
                byte MorphType = 0;
                if (!Analyser.GetByte(ref MorphType)) return false;

                // モーフのオフセット数 : 後続の要素数
                int NumofMorphOffset = 0;
                if (!Analyser.GetInt(ref NumofMorphOffset)) return false;

                // モーフ種類に従ってオフセットデータを格納 ※異なる種類の混合は不可
                if (MorphType == 0)
                {
                    // グループ
                    for (int m = 0; m < NumofMorphOffset; m++)
                    {
                        // モーフIndex
                        int MorphIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.MorphIndexSize);

                        // モーフ率 : グループモーフのモーフ値 * モーフ率 = 対象モーフのモーフ値
                        float MorphRate = 0.0f;
                        if (!Analyser.GetFloat(ref MorphRate)) return false;
                    }
                }
                else if (MorphType == 1)
                {
                    // 頂点
                    for (int m = 0; m < NumofMorphOffset; m++)
                    {
                        // 頂点Index
                        int VertexIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.VertexIndexSize);

                        // 座標オフセット量(x,y,z)
                        if (!Analyser.IsValid(4 * 3)) return false;

                        Vector3 Offset = new Vector3(0.0f, 0.0f, 0.0f);

                        Offset.x = Analyser.GetFloat();
                        Offset.y = Analyser.GetFloat();
                        Offset.z = Analyser.GetFloat();

                        // 登録
                        PmxMorph.AddVertexMorph(VertexIndex, Offset);
                    }

                    m_PmxVertexMorphList.Add(MorphName, PmxMorph);
                }
                else if (MorphType == 2)
                {
                    // ボーン
                    for (int m = 0; m < NumofMorphOffset; m++)
                    {
                        // ボーンIndex
                        int BoneIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.BoneIndexSize);

                        // 移動量
                        if (!Analyser.IsValid(4 * 3)) return false;

                        Vector3 Translate = new Vector3(0.0f, 0.0f, 0.0f);

                        Translate.x = Analyser.GetFloat();
                        Translate.y = Analyser.GetFloat();
                        Translate.z = Analyser.GetFloat();

                        // 回転量(クォータニオン)
                        if (!Analyser.IsValid(4 * 4)) return false;

                        Quaternion Rotate = Quaternion.identity;

                        Rotate.x = Analyser.GetFloat();
                        Rotate.y = Analyser.GetFloat();
                        Rotate.z = Analyser.GetFloat();
                        Rotate.w = Analyser.GetFloat();
                    }
                }
                else if (MorphType == 3)
                {
                    // UV
                    for (int m = 0; m < NumofMorphOffset; m++)
                    {
                        // 頂点Index
                        int VertexIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.VertexIndexSize);

                        // UVオフセット量(x,y,z,w) ※通常UVはz,wが不要項目になるがモーフとしてのデータ値は記録しておく
                        if (!Analyser.IsValid(4 * 4)) return false;

                        Vector4 UV = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                        UV.x = Analyser.GetFloat();
                        UV.y = Analyser.GetFloat();
                        UV.z = Analyser.GetFloat();
                        UV.w = Analyser.GetFloat();
                    }
                }
                else if (MorphType == 4)
                {
                    // 追加UV1
                    for (int m = 0; m < NumofMorphOffset; m++)
                    {
                        // 頂点Index
                        int VertexIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.VertexIndexSize);

                        // UVオフセット量(x,y,z,w) ※通常UVはz,wが不要項目になるがモーフとしてのデータ値は記録しておく
                        if (!Analyser.IsValid(4 * 4)) return false;

                        Vector4 UV = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                        UV.x = Analyser.GetFloat();
                        UV.y = Analyser.GetFloat();
                        UV.z = Analyser.GetFloat();
                        UV.w = Analyser.GetFloat();
                    }
                }
                else if (MorphType == 5)
                {
                    // 追加UV2
                    for (int m = 0; m < NumofMorphOffset; m++)
                    {
                        // 頂点Index
                        int VertexIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.VertexIndexSize);

                        // UVオフセット量(x,y,z,w) ※通常UVはz,wが不要項目になるがモーフとしてのデータ値は記録しておく
                        if (!Analyser.IsValid(4 * 4)) return false;

                        Vector4 UV = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                        UV.x = Analyser.GetFloat();
                        UV.y = Analyser.GetFloat();
                        UV.z = Analyser.GetFloat();
                        UV.w = Analyser.GetFloat();
                    }
                }
                else if (MorphType == 6)
                {
                    // 追加UV3
                    for (int m = 0; m < NumofMorphOffset; m++)
                    {
                        // 頂点Index
                        int VertexIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.VertexIndexSize);

                        // UVオフセット量(x,y,z,w) ※通常UVはz,wが不要項目になるがモーフとしてのデータ値は記録しておく
                        if (!Analyser.IsValid(4 * 4)) return false;

                        Vector4 UV = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                        UV.x = Analyser.GetFloat();
                        UV.y = Analyser.GetFloat();
                        UV.z = Analyser.GetFloat();
                        UV.w = Analyser.GetFloat();
                    }
                }
                else if (MorphType == 7)
                {
                    // 追加UV4
                    for (int m = 0; m < NumofMorphOffset; m++)
                    {
                        // 頂点Index
                        int VertexIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.VertexIndexSize);

                        // UVオフセット量(x,y,z,w) ※通常UVはz,wが不要項目になるがモーフとしてのデータ値は記録しておく
                        if (!Analyser.IsValid(4 * 4)) return false;

                        Vector4 UV = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                        UV.x = Analyser.GetFloat();
                        UV.y = Analyser.GetFloat();
                        UV.z = Analyser.GetFloat();
                        UV.w = Analyser.GetFloat();
                    }
                }
                else if (MorphType == 8)
                {
                    // 材質
                    for (int m = 0; m < NumofMorphOffset; m++)
                    {
                        // 材質Index
                        int MaterialIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.MaterialIndexSize);

                        // オフセット演算形式 | 0:乗算, 1:加算
                        byte OffsetCalcFormat = 0;
                        if (!Analyser.GetByte(ref OffsetCalcFormat)) return false;

                        // Diffuse
                        Vector4 Diffuse = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                        {
                            if (!Analyser.IsValid(4 * 4)) return false;

                            float R = Analyser.GetFloat();
                            float G = Analyser.GetFloat();
                            float B = Analyser.GetFloat();
                            float A = Analyser.GetFloat();

                            Diffuse = new Vector4(R, G, B, A);
                        }

                        // Specular
                        Vector4 Specular = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                        {
                            if (!Analyser.IsValid(4 * 3)) return false;

                            float R = Analyser.GetFloat();
                            float G = Analyser.GetFloat();
                            float B = Analyser.GetFloat();

                            Specular = new Vector4(R, G, B, 1.0f);
                        }

                        // Specular係数
                        float SpecularCoef = 1.0f;
                        if (!Analyser.GetFloat(ref SpecularCoef)) return false;

                        // Ambient
                        Vector4 Ambient = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                        {
                            if (!Analyser.IsValid(4 * 3)) return false;

                            float R = Analyser.GetFloat();
                            float G = Analyser.GetFloat();
                            float B = Analyser.GetFloat();

                            Ambient = new Vector4(R, G, B, 1.0f);
                        }

                        // エッジカラー
                        Vector4 EdgeColor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                        {
                            if (!Analyser.IsValid(4 * 4)) return false;

                            float R = Analyser.GetFloat();
                            float G = Analyser.GetFloat();
                            float B = Analyser.GetFloat();
                            float A = Analyser.GetFloat();

                            EdgeColor = new Vector4(R, G, B, A);
                        }

                        // エッジサイズ
                        float EdgeSize = 1.0f;
                        if (!Analyser.GetFloat(ref EdgeSize)) return false;

                        // テクスチャ係数
                        Vector4 TextureFactor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                        if (!Analyser.IsValid(4 * 4)) return false;

                        TextureFactor.x = Analyser.GetFloat();
                        TextureFactor.x = Analyser.GetFloat();
                        TextureFactor.x = Analyser.GetFloat();
                        TextureFactor.x = Analyser.GetFloat();

                        // スフィアテクスチャ係数
                        Vector4 SphereTextureFactor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                        if (!Analyser.IsValid(4 * 4)) return false;

                        SphereTextureFactor.x = Analyser.GetFloat();
                        SphereTextureFactor.x = Analyser.GetFloat();
                        SphereTextureFactor.x = Analyser.GetFloat();
                        SphereTextureFactor.x = Analyser.GetFloat();

                        // Toonテクスチャ係数
                        Vector4 ToonTextureFactor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                        if (!Analyser.IsValid(4 * 4)) return false;

                        ToonTextureFactor.x = Analyser.GetFloat();
                        ToonTextureFactor.x = Analyser.GetFloat();
                        ToonTextureFactor.x = Analyser.GetFloat();
                        ToonTextureFactor.x = Analyser.GetFloat();
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        bool AnalyseDisplayFrame(ref CBinaryReader Analyser)
        {
            // 表示枠数
            int NumOfDisplayFrame = 0;
            if (!Analyser.GetInt(ref NumOfDisplayFrame)) return false;

            for (int i = 0; i < NumOfDisplayFrame; i++)
            {
                // 枠名
                string FrameName = string.Empty;
                {
                    int ByteLength = 0;
                    if (!Analyser.GetInt(ref ByteLength)) return false;

                    if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                    {
                        if (!Analyser.GetString(ref FrameName, ByteLength)) return false;
                    }
                    else if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                    {
                        if (!Analyser.GetUTF16String(ref FrameName, ByteLength)) return false;
                    }
                }

                // 枠名EN
                string FrameNameEN = string.Empty;
                {
                    int ByteLength = 0;
                    if (!Analyser.GetInt(ref ByteLength)) return false;

                    if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                    {
                        if (!Analyser.GetString(ref FrameName, ByteLength)) return false;
                    }
                    else if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                    {
                        if (!Analyser.GetUTF16String(ref FrameName, ByteLength)) return false;
                    }
                }

                // 特殊枠フラグ - 0:通常枠 1:特殊枠
                byte SpetialFrame = 0;
                if (!Analyser.GetByte(ref SpetialFrame)) return false;

                // 枠内要素数
                int NumOfInnerFrameElem = 0;
                if (!Analyser.GetInt(ref NumOfInnerFrameElem)) return false;

                for (int e = 0; e < NumOfInnerFrameElem; e++)
                {
                    // 要素対象 0:ボーン 1:モーフ
                    byte ElementTarget = 0;
                    if (!Analyser.GetByte(ref ElementTarget)) return false;

                    if (ElementTarget == 0)
                    {
                        int BoneIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.BoneIndexSize);
                    }
                    else if (ElementTarget == 1)
                    {
                        int MorphIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.MorphIndexSize);
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        bool AnalyseRigidbody(ref CBinaryReader Analyser)
        {
            // 剛体数
            int NumOfRigidbody = 0;
            if (!Analyser.GetInt(ref NumOfRigidbody)) return false;

            for (int i = 0; i < NumOfRigidbody; i++)
            {
                // 剛体名
                string RigidbodyName = string.Empty;
                {
                    int ByteLength = 0;
                    if (!Analyser.GetInt(ref ByteLength)) return false;

                    if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                    {
                        if (!Analyser.GetString(ref RigidbodyName, ByteLength)) return false;
                    }
                    else if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                    {
                        if (!Analyser.GetUTF16String(ref RigidbodyName, ByteLength)) return false;
                    }
                }

                // 剛体名EN
                string RigidbodyNameEN = string.Empty;
                {
                    int ByteLength = 0;
                    if (!Analyser.GetInt(ref ByteLength)) return false;

                    if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                    {
                        if (!Analyser.GetString(ref RigidbodyNameEN, ByteLength)) return false;
                    }
                    else if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                    {
                        if (!Analyser.GetUTF16String(ref RigidbodyNameEN, ByteLength)) return false;
                    }
                }

                // 関連ボーンIndex - 関連なしの場合は-1
                int RelationBoneIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.BoneIndexSize, true);

                // グループ
                // ここのバイナリに入っているのは左シフト数
                byte shiftCount = 0;
                if (!Analyser.GetByte(ref shiftCount)) return false;

                // シフト演算を実行してグループを取得する
                ushort group = (ushort)(0x01 << shiftCount);

                // 非衝突グループフラグ
                ushort NoneCollideGroupFlag = 0;
                if (!Analyser.GetUShort(ref NoneCollideGroupFlag)) return false;

                // フラグが立っていると当たらないということにする
                NoneCollideGroupFlag = (ushort)(~NoneCollideGroupFlag);

                // 形状 - 0:球 1:箱 2:カプセル
                byte Shape = 0;
                if (!Analyser.GetByte(ref Shape)) return false;

                EPmxPhysicsShape PhysicsShape = (EPmxPhysicsShape)((int)(Shape));

                // サイズ(x,y,z)
                if (!Analyser.IsValid(4 * 3)) return false;
                Vector3 Size = new Vector3(Analyser.GetFloat(), Analyser.GetFloat(), Analyser.GetFloat());

                // 位置(x,y,z)
                if (!Analyser.IsValid(4 * 3)) return false;
                Vector3 Pos = new Vector3(Analyser.GetFloat(), Analyser.GetFloat(), Analyser.GetFloat());

                // 回転(x,y,z) -> ラジアン角
                if (!Analyser.IsValid(4 * 3)) return false;
                Vector3 Rotate = new Vector3(Analyser.GetFloat(), Analyser.GetFloat(), Analyser.GetFloat());

                // 質量
                float Mass = 0.0f;
                if (!Analyser.GetFloat(ref Mass)) return false;

                // 移動減衰
                float TransDamping = 0.0f;
                if (!Analyser.GetFloat(ref TransDamping)) return false;

                // 回転減衰
                float RotateDamping = 0.0f;
                if (!Analyser.GetFloat(ref RotateDamping)) return false;

                // 反発力
                float Repulsion = 0.0f;
                if (!Analyser.GetFloat(ref Repulsion)) return false;

                // 摩擦力
                float Friction = 0.0f;
                if (!Analyser.GetFloat(ref Friction)) return false;

                // 剛体の物理演算 - 0:ボーン追従(static) 1:物理演算(dynamic) 2:物理演算 + Bone位置合わせ
                byte PhysicsType = 0;
                if (!Analyser.GetByte(ref PhysicsType)) return false;

                // 剛体を登録
                SPmxRigidbody PmxRigidbody = new SPmxRigidbody();
                PmxRigidbody.RigidbodyName = RigidbodyName;
                PmxRigidbody.RigidbodyNameEN = RigidbodyNameEN;
                PmxRigidbody.RelationBoneIndex = RelationBoneIndex;
                PmxRigidbody.group = group;
                PmxRigidbody.NoneCollideGroupFlag = NoneCollideGroupFlag;
                PmxRigidbody.PhysicsShape = PhysicsShape;
                PmxRigidbody.Size = Size;
                PmxRigidbody.Pos = Pos;
                PmxRigidbody.Rotate = Rotate;
                PmxRigidbody.Mass = Mass;
                PmxRigidbody.TransDamping = TransDamping;
                PmxRigidbody.RotateDamping = RotateDamping;
                PmxRigidbody.Repulsion = Repulsion;
                PmxRigidbody.Friction = Friction;
                PmxRigidbody.PhysicsType = (EPmxPhysicsType)((int)(PhysicsType));

                m_PmxRigidbodyList.Add(PmxRigidbody);
            }

            return true;
        }

        bool AnalyseJoint(ref CBinaryReader Analyser)
        {
            // Joint数
            int NumOfJoint = 0;
            if (!Analyser.GetInt(ref NumOfJoint)) return false;

            for (int i = 0; i < NumOfJoint; i++)
            {
                SPmxJoint PmxJoint = new SPmxJoint();

                // Joint名
                string JointName = string.Empty;
                {
                    int ByteLength = 0;
                    if (!Analyser.GetInt(ref ByteLength)) return false;

                    if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                    {
                        if (!Analyser.GetString(ref JointName, ByteLength)) return false;
                    }
                    else if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                    {
                        if (!Analyser.GetUTF16String(ref JointName, ByteLength)) return false;
                    }
                }

                PmxJoint.JointName = JointName;

                // Joint名EN
                string JointNameEN = string.Empty;
                {
                    int ByteLength = 0;
                    if (!Analyser.GetInt(ref ByteLength)) return false;

                    if (m_MetaData.EncodeType == EPmxEncodeType.UTF8)
                    {
                        if (!Analyser.GetString(ref JointNameEN, ByteLength)) return false;
                    }
                    else if (m_MetaData.EncodeType == EPmxEncodeType.UTF16)
                    {
                        if (!Analyser.GetUTF16String(ref JointNameEN, ByteLength)) return false;
                    }
                }

                PmxJoint.JointNameEN = JointNameEN;

                // Joint種類 - 0:スプリング6DOF   | PMX2.0では 0 のみ(拡張用)
                // 2.1以降で6Dof以外の時はいったんエラーを出して伝える → その後実装する？
                byte JointType = 0;
                if (!Analyser.GetByte(ref JointType)) return false;

                PmxJoint.PmxJointType = (EPmxJointType)((int)(JointType));

                // 0: ﾊﾞﾈ付6DOF: btGeneric6DofSpringConstraint
                // 1: 6DOF: btGeneric6DofConstraint
                // 2: P2P: btPoint2PointConstraint
                // 3: ConeTwist: btConeTwistConstraint
                // 5: Slider: btSliderConstraint

                // 関連剛体AのIndex - 関連なしの場合は-1
                PmxJoint.BodyAIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.RigidIndexSize);

                // 関連剛体BのIndex - 関連なしの場合は-1
                PmxJoint.BodyBIndex = GetMultiTypeValueAsInterger(ref Analyser, m_MetaData.RigidIndexSize);

                // 位置(x,y,z)
                if (!Analyser.IsValid(4 * 3)) return false;
                PmxJoint.Pos = new Vector3(Analyser.GetFloat(), Analyser.GetFloat(), Analyser.GetFloat());

                // 回転(x,y,z) -> ラジアン角
                if (!Analyser.IsValid(4 * 3)) return false;
                PmxJoint.Rotate = new Vector3(Analyser.GetFloat(), Analyser.GetFloat(), Analyser.GetFloat());

                if (JointType != 2)
                {
                    // 移動制限-下限(x,y,z)
                    if (!Analyser.IsValid(4 * 3)) return false;
                    PmxJoint.LowerTransLimit = new Vector3(Analyser.GetFloat(), Analyser.GetFloat(), Analyser.GetFloat());

                    // 移動制限-上限(x,y,z)
                    if (!Analyser.IsValid(4 * 3)) return false;
                    PmxJoint.UpperTransLimit = new Vector3(Analyser.GetFloat(), Analyser.GetFloat(), Analyser.GetFloat());

                    // 回転制限-下限(x,y,z) -> ラジアン角
                    if (!Analyser.IsValid(4 * 3)) return false;
                    PmxJoint.LowerRotateLimit = new Vector3(Analyser.GetFloat(), Analyser.GetFloat(), Analyser.GetFloat());

                    // 回転制限-上限(x,y,z) -> ラジアン角
                    if (!Analyser.IsValid(4 * 3)) return false;
                    PmxJoint.UpperRotateLimit = new Vector3(Analyser.GetFloat(), Analyser.GetFloat(), Analyser.GetFloat());

                    if (JointType != 1)
                    {
                        // バネ定数-移動(x,y,z)
                        if (!Analyser.IsValid(4 * 3)) return false;
                        PmxJoint.TransSpring = new Vector3(Analyser.GetFloat(), Analyser.GetFloat(), Analyser.GetFloat());

                        // バネ定数-回転(x,y,z)
                        if (!Analyser.IsValid(4 * 3)) return false;
                        PmxJoint.RotateSpring = new Vector3(Analyser.GetFloat(), Analyser.GetFloat(), Analyser.GetFloat());
                    }
                }

                // Jointを登録
                m_PmxJointList.Add(PmxJoint);
            }

            return true;
        }

        // Helper Functions ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        bool GetMultiTypeValue(ref CBinaryReader Analyser, int ByteSize, ref List<uint> UIntValueList, 
            ref List<byte> ByteValueList, ref List<ushort> UShortValueList)
        {
            if (ByteSize == 1)
            {
                byte BoneIndex = 0;
                if (!Analyser.GetByte(ref BoneIndex)) return false;

                ByteValueList.Add(BoneIndex);
            }
            else if (ByteSize == 2)
            {
                ushort BoneIndex = 0;
                if (!Analyser.GetUShort(ref BoneIndex)) return false;

                UShortValueList.Add(BoneIndex);
            }
            else if (ByteSize == 4)
            {
                int BoneIndex = 0;
                if (!Analyser.GetInt(ref BoneIndex)) return false;

                UIntValueList.Add((uint)(BoneIndex));
            }
            else
            {
                return false;
            }

            return true;
        }

        bool AddEmptyMultiTypeValue(int ByteSize, ref List<uint> UIntValueList, ref List<byte> ByteValueList, ref List<ushort> UShortValueList)
        {
            if (ByteSize == 1)
            {
                byte BoneIndex = 0;

                ByteValueList.Add(BoneIndex);
            }
            else if (ByteSize == 2)
            {
                ushort BoneIndex = 0;

                UShortValueList.Add(BoneIndex);
            }
            else if (ByteSize == 4)
            {
                uint BoneIndex = 0;

                UIntValueList.Add(BoneIndex);
            }
            else
            {
                return false;
            }

            return true;
        }

        int GetMultiTypeValueAsInterger(ref CBinaryReader Analyser, int ByteSize, bool UseSign = false)
        {
            int Result = -1;

            if (ByteSize == 1)
            {
                byte Index = 0;
                if (!Analyser.GetByte(ref Index)) return -1;

                Result = (int)(Index);
            }
            else if (ByteSize == 2)
            {
                if (UseSign)
                {
                    short Index = 0;
                    if (!Analyser.GetShort(ref Index)) return -1;

                    Result = (int)(Index);
                }
                else
                {
                    ushort Index = 0;
                    if (!Analyser.GetUShort(ref Index)) return -1;

                    Result = (int)(Index);
                }
            }
            else if (ByteSize == 4)
            {
                int Index = 0;
                if (!Analyser.GetInt(ref Index)) return -1;

                Result = Index;
            }
            else
            {
                Result = -1;
            }

            return Result;
        }
    }
}