using binary;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling.Memory.Experimental;

namespace mmdlib
{
    public class CPmxModel
    {
        SPmxMetaData m_MetaData = new SPmxMetaData();

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
            {
                int NumOfVertex = 0;
                if (!Analyser.GetInt(ref NumOfVertex)) return false;

                for (int VertexIndex = 0; VertexIndex < NumOfVertex; VertexIndex++)
                {

                }
            }

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
    }
}