using binary;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Profiling.Memory.Experimental;
using static TreeEditor.TextureAtlas;

namespace mmdlib
{
    public class CVMDData
    {
        // ボーンアニメーション
        Dictionary<EHumanoidBones, List<SVMDFrame>> m_FrameMap = new Dictionary<EHumanoidBones, List<SVMDFrame>>();
        int m_MinFrameIndex = int.MaxValue;
        int m_MaxFrameIndex = int.MinValue;

        // 表情アニメーション
        Dictionary<string, List<SVMDSkinFrame>> m_SkinFrameMap = new Dictionary<string, List<SVMDSkinFrame>>();
        int m_MinSkinFrameIndex;
        int m_MaxSkinFrameIndex;

        public CVMDData()
        {
        }

        // ボーンアニメーション
        public Dictionary<EHumanoidBones, List<SVMDFrame>> GetFrameMap()
	    {
		    return m_FrameMap;
	    }

        public int GetMinFrameIndex()
	    {
		    return m_MinFrameIndex;
	    }

        public int GetMaxFrameIndex()
	    {
		    return m_MaxFrameIndex;
	    }

	    // 表情アニメーション
	    public Dictionary<string, List<SVMDSkinFrame>> GetSkinFrameMap()
	    {
		    return m_SkinFrameMap;
	    }

        public int GetMinSkinFrameIndex()
	    {
		    return m_MinSkinFrameIndex;
	    }

	    public int GetMaxSkinFrameIndex()
	    {
		    return m_MaxSkinFrameIndex;
	    }

        public bool Analyse(string fileName)
        {
            CBinaryReader Analyser = new CBinaryReader();
            if (!Analyser.Init(fileName)) return false;

            /*
		    // ヘッダ
		    struct VMD_HEADER {
		    char VmdHeader[30]; // "Vocaloid Motion Data 0002"
		    char VmdModelName[20]; // カメラの場合:"カメラ・照明" // カメラ・照明・アクセサリモードではモデル用のVMDは読めなくなりました(7.10-)
		    } vmd_header;
		    */

            // ヘッダ情報をチェック
            string header = "";
            if (!Analyser.GetString(ref header, 30)) return false;

            string modelName = string.Empty;
            if (!Analyser.GetSJISString(ref modelName, 20)) return false;

            // フレームデータ
            if (!AnalyseFrameData(ref Analyser)) return false;

            // 表情データ
            // スキンデータと書かれることが多いがこれはリターゲット用のスキンデータではなく表情という意味らしい
            // (なのでVMDモーションはリターゲット不要と捉えていいのかな？)
            if (!AnalyseFacialExpressionData(ref Analyser)) return false;

            // カメラデータ
            if (!AnalyseCameraData(ref Analyser)) return false;

            // 照明データ
            if (!AnalyseLightData(ref Analyser)) return false;

            // セルフシャドウデータ
            if (!AnalyseSelfShadowData(ref Analyser)) return false;

            return true;
        }

        bool AnalyseFrameData(ref CBinaryReader Analyser)
        {
            /*
		    // モーションデータ数
		    //
		    // キーフレーム数の上限(MMD側で制限):300,000(32bit版), 600,000(64bit版 7.39dot-)
		    // 上限判定時は、フレーム0にあるキー(デフォルトではモデルのボーン数と同数)もカウントされるので注意
		    //
		    struct VMD_MOTION_COUNT {
		    DWORD Count;// モーションデータ数
		    } vmd_motion_count;

		    // モーションデータ
		    struct VMD_MOTION { // 111 Bytes // モーション
		    char BoneName[15]; // ボーン名
		    DWORD FrameNo; // フレーム番号(読込時は現在のフレーム位置を0とした相対位置)
		    float Location[3]; // 位置
		    float Rotatation[4]; // Quaternion // 回転
		    BYTE Interpolation[64]; // [4][4][4] // 補完
		    } vmd_motion;
		    */

            // フレームデータ数
            int FrameDataCount = 0;
            if (!Analyser.GetInt(ref FrameDataCount)) return false;

            for (int i = 0; i < FrameDataCount; i++)
            {
                // ボーン名
                string Name = string.Empty;
                if (!Analyser.GetSJISString(ref Name, 15)) return false;

                // フレームインデックス
                int FrameIndex = -1;
                if (!Analyser.GetInt(ref FrameIndex)) return false;

                m_MinFrameIndex = Mathf.Min(FrameIndex, m_MinFrameIndex);
                m_MaxFrameIndex = Mathf.Max(FrameIndex, m_MaxFrameIndex);

                // ボーンの位置
                if (!Analyser.IsValid(4 * 3)) return false;

                Vector3 Pos = new Vector3(0.0f, 0.0f, 0.0f);

                Pos.x = Analyser.GetFloat();
                Pos.y = Analyser.GetFloat();
                Pos.z = Analyser.GetFloat();

                // ボーンの回転
                if (!Analyser.IsValid(4 * 4)) return false;

                Quaternion Rot = new Quaternion();

                Rot.x = Analyser.GetFloat();
                Rot.y = Analyser.GetFloat();
                Rot.z = Analyser.GetFloat();
                Rot.w = Analyser.GetFloat();

                // Unityではこの補正は不要
                // VMDは鏡反転になっているので補正する
                //Pos.x *= -1.0f;
                //Rot.x *= -1.0f;
                //Rot.w *= -1.0f;

                // 補完パラメーター(ベジュ曲線に使用する) - Interpolation Params
                if (!Analyser.IsValid(4 * 4 * 4)) return false;

                //
                Vector2 X_Interpolation_A = new Vector2(0.0f, 0.0f);
                Vector2 X_Interpolation_B = new Vector2(0.0f, 0.0f);
                Vector2 Y_Interpolation_A = new Vector2(0.0f, 0.0f);
                Vector2 Y_Interpolation_B = new Vector2(0.0f, 0.0f);
                Vector2 Z_Interpolation_A = new Vector2(0.0f, 0.0f);
                Vector2 Z_Interpolation_B = new Vector2(0.0f, 0.0f);
                Vector2 R_Interpolation_A = new Vector2(0.0f, 0.0f);
                Vector2 R_Interpolation_B = new Vector2(0.0f, 0.0f);

                // ax
                {
                    X_Interpolation_A.x = (float)(Analyser.GetByte());
                    Y_Interpolation_A.x = (float)(Analyser.GetByte());
                    Z_Interpolation_A.x = (float)(Analyser.GetByte());
                    R_Interpolation_A.x = (float)(Analyser.GetByte());
                }

                // ay
                {
                    X_Interpolation_A.y = (float)(Analyser.GetByte());
                    Y_Interpolation_A.y = (float)(Analyser.GetByte());
                    Z_Interpolation_A.y = (float)(Analyser.GetByte());
                    R_Interpolation_A.y = (float)(Analyser.GetByte());
                }

                // bx
                {
                    X_Interpolation_B.x = (float)(Analyser.GetByte());
                    Y_Interpolation_B.x = (float)(Analyser.GetByte());
                    Z_Interpolation_B.x = (float)(Analyser.GetByte());
                    R_Interpolation_B.x = (float)(Analyser.GetByte());
                }

                // by
                {
                    X_Interpolation_B.y = (float)(Analyser.GetByte());
                    Y_Interpolation_B.y = (float)(Analyser.GetByte());
                    Z_Interpolation_B.y = (float)(Analyser.GetByte());
                    R_Interpolation_B.y = (float)(Analyser.GetByte());
                }

                //
                List<Vector2> XPointList = new List<Vector2>();
                {
                    XPointList.Add(new Vector2(0.0f, 0.0f));
                    XPointList.Add(X_Interpolation_A / 127.0f);
                    XPointList.Add(X_Interpolation_B / 127.0f);
                    XPointList.Add(new Vector2(1.0f, 1.0f));
                }

                List<Vector2> YPointList = new List<Vector2>();
                {
                    YPointList.Add(new Vector2(0.0f, 0.0f));
                    YPointList.Add(Y_Interpolation_A / 127.0f);
                    YPointList.Add(Y_Interpolation_B / 127.0f);
                    YPointList.Add(new Vector2(1.0f, 1.0f));
                }

                List<Vector2> ZPointList = new List<Vector2>();
                {
                    ZPointList.Add(new Vector2(0.0f, 0.0f));
                    ZPointList.Add(Z_Interpolation_A / 127.0f);
                    ZPointList.Add(Z_Interpolation_B / 127.0f);
                    ZPointList.Add(new Vector2(1.0f, 1.0f));
                }

                List<Vector2> RPointList = new List<Vector2>();
                {
                    RPointList.Add(new Vector2(0.0f, 0.0f));
                    RPointList.Add(R_Interpolation_A / 127.0f);
                    RPointList.Add(R_Interpolation_B / 127.0f);
                    RPointList.Add(new Vector2(1.0f, 1.0f));
                }

                // 残りの48バイトはひとまずスキップ
                if (!Analyser.Skip(16 * 3)) return false;

                // ボーン名を取得
                EHumanoidBones BoneName = CPmxHumanoidBoneMapper.CastStringToBoneName(Name);

                // Noneはどのボーンに割り当てればいいかわからないのでスキップする
                if (BoneName == EHumanoidBones.None) continue;

                // MapにPairが無ければ新規作成
                if (!m_FrameMap.ContainsKey(BoneName))
                {
                    m_FrameMap.Add(BoneName, new List<SVMDFrame>());
                }

                // Mapにデータを登録する
                SVMDFrame Frame = new SVMDFrame(BoneName, FrameIndex, Pos, Rot, XPointList, YPointList, ZPointList, RPointList);

                m_FrameMap[BoneName].Add(Frame);
            }

            return true;
        }

        bool AnalyseFacialExpressionData(ref CBinaryReader Analyser)
        {
            /*
            // 表情データ数
            struct VMD_Skeleton_COUNT {
            DWORD Count; // 表情データ数
            } vmd_Skeleton_count;

            // 表情データ
            struct VMD_Skeleton { // 23 Bytes // 表情
            char SkeletonName[15]; // 表情名
            DWORD FlameNo; // フレーム番号
            float Weight; // 表情の設定値(表情スライダーの値)
            } vmd_Skeleton;
            */

            // 表情データ数
            int ExpressionCount = 0;
            if (!Analyser.GetInt(ref ExpressionCount)) return false;

            for (int i = 0; i < ExpressionCount; i++)
            {
                // 表情名
                string BlendShapeName = string.Empty;
                if (!Analyser.GetSJISString(ref BlendShapeName, 15)) return false;

                // フレームインデックス
                int FrameIndex = -1;
                if (!Analyser.GetInt(ref FrameIndex)) return false;
                
                m_MinSkinFrameIndex = Mathf.Min(FrameIndex, m_MinSkinFrameIndex);
                m_MaxSkinFrameIndex = Mathf.Max(FrameIndex, m_MaxSkinFrameIndex);

                // ウェイト
                float Weight = 0.0f;
                if (!Analyser.GetFloat(ref Weight)) return false;

                // 登録
                if (!m_SkinFrameMap.ContainsKey(BlendShapeName))
                {
                    m_SkinFrameMap.Add(BlendShapeName, new List<SVMDSkinFrame>());
                }

                SVMDSkinFrame SkinFrame = new SVMDSkinFrame(FrameIndex, Weight);
                m_SkinFrameMap[BlendShapeName].Add(SkinFrame);
            }

            return true;
        }

        bool AnalyseCameraData(ref CBinaryReader Analyser)
        {
            /*
		    // カメラデータ数
		    struct VMD_CAMERA_COUNT {
		    DWORD Count; // カメラデータ数
		    } vmd_camera_count;

		    // カメラデータ
		    struct VMD_CAMERA { // 61 Bytes // カメラ
		    DWORD FlameNo; // フレーム番号
		    float Length; // -(距離)
		    float Location[3]; // 位置
		    float Rotation[3]; // オイラー角 // X軸は符号が反転しているので注意 // 回転
		    BYTE Interpolation[24]; // おそらく[6][4](未検証) // 補完
		    DWORD ViewingAngle; // 視界角
		    BYTE Perspective; // 0:on 1:off // パースペクティブ
		    } vmd_camera;
		    */

            return true;
        }

        bool AnalyseLightData(ref CBinaryReader Analyser)
        {
            /*
		    // 照明データ数
		    struct VMD_LIGHT_COUNT {
		    DWORD Count; // 照明データ数
		    } vmd_light_count;

		    // 照明データ
		    struct VMD_LIGHT { // 28 Bytes // 照明
		    DWORD FlameNo; // フレーム番号
		    float RGB[3]; // RGB各値/256 // 赤、緑、青
		    float Location[3]; // X, Y, Z
		    } vmd_light;
		    */

            return true;
        }

        bool AnalyseSelfShadowData(ref CBinaryReader Analyser)
        {
            /*
		    // セルフシャドウデータ数
		    struct VMD_SELF_SHADOW_COUNT {
		    DWORD Count; // セルフシャドウデータ数
		    } vmd_self_shadow_count;

		    // セルフシャドウデータ
		    struct VMD_SELF_SHADOW { // 9 Bytes // セルフシャドー
		    DWORD FlameNo; // フレーム番号
		    BYTE Mode; // 00-02 // モード
		    float Distance; // 0.1 - (dist * 0.00001) // 距離
		    } vmd_self_shadow;
		    */

            return true;
        }
    }
}