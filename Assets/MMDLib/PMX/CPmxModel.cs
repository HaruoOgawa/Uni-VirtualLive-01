using binary;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling.Memory.Experimental;

namespace mmdlib
{
    public class CPmxModel
    {
        SPmxMetaData m_MetaData = new SPmxMetaData();
        CPmxMesh m_PmxMesh = null;

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
            
            // MetaData.BoneIndexSizeに応じてバイト数が変わる
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
            return true;
        }

        bool AnalyseMaterial(ref CBinaryReader Analyser)
        {
            return true;
        }

        bool AnalyseBone(ref CBinaryReader Analyser)
        {
            return true;
        }

        bool AnalyseMorph(ref CBinaryReader Analyser)
        {
            return true;
        }

        bool AnalyseDisplayFrame(ref CBinaryReader Analyser)
        {
            return true;
        }

        bool AnalyseRigidbody(ref CBinaryReader Analyser)
        {
            return true;
        }

        bool AnalyseJoint(ref CBinaryReader Analyser)
        {
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

        int GetMultiTypeValueAsInterger(ref CBinaryReader Analyser, int ByteSize, bool UseSign)
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