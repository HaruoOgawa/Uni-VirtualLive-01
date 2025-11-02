using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI.Table;

// ScriptedImporter
// https://docs.unity3d.com/6000.2/Documentation/ScriptReference/AssetImporters.ScriptedImporter.html

namespace mmdlib
{
    [ScriptedImporter(1, "vmd")]
    public class CVMDImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (ctx == null) return;

            string assetOutDir = Path.GetDirectoryName(ctx.assetPath);
            string assetFileName = Path.GetFileNameWithoutExtension(ctx.assetPath);

            List<string> srcPathList = new List<string>();

            // インポートするファイルの元々配置してあったパスを取得
            foreach (var path in DragAndDrop.paths)
            {
                // 拡張子が一致しているか
                string DDExtension = Path.GetExtension(path);
                if (DDExtension != ".vmd") continue;

                // ファイル名がアセットパス末尾の数字を除いて一致しているか
                string DDFileName = Path.GetFileNameWithoutExtension(path);
                int index = assetFileName.IndexOf(DDFileName);

                if (index == -1) continue;

                // パスが一致しているので読み込むファイルとして追加
                srcPathList.Add(path);
                break;
            }

            // アセットインポートを実行
            foreach (var importPath in srcPathList)
            {
                // インポート
                if (!Import(importPath, assetOutDir))
                {
                    string message = "[Error] Failed to import. importPath: " + importPath;

                    throw new System.Exception(message);
                }
            }
        }

        static bool Import(string fileName, string outDir)
        {
            string srcFolder = Path.GetDirectoryName(fileName);

            CVMDData vmd = new CVMDData();
            if (!vmd.Analyse(fileName)) return false;

            string rootName = Path.GetFileNameWithoutExtension(fileName);

            string AssetFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(outDir, rootName));

            // アニメーションクリップの作成
            if (!CreateAnimationClip(vmd, AssetFolder, rootName)) return false;

            // 表情アニメーションクリップの作成
            if (!CreateBlendShapeClip(vmd, AssetFolder, rootName)) return false;

            return true;
        }

        static bool CreateAnimationClip(CVMDData VMDData, string AssetFolder, string AssetName)
        {
            // MMDのアニメーションは30FPSで固定
            const float FrameRate = 30.0f;

            int MinFrameIndex = VMDData.GetMinFrameIndex();
            int MaxFrameIndex = VMDData.GetMaxFrameIndex();

            float StartTime = (float)(MinFrameIndex) * (1.0f / FrameRate);
            float EndTime = (float)(MaxFrameIndex) * (1.0f / FrameRate);

            AnimationClip clip = new AnimationClip();
            clip.name = AssetName;

            int Index = 0;

            foreach (var Frame in VMDData.GetFrameMap())
		    {
                AnimationCurve localPos_X_Curve = new AnimationCurve();
                AnimationCurve localPos_Y_Curve = new AnimationCurve();
                AnimationCurve localPos_Z_Curve = new AnimationCurve();
                AnimationCurve localRot_X_Curve = new AnimationCurve();
                AnimationCurve localRot_Y_Curve = new AnimationCurve();
                AnimationCurve localRot_Z_Curve = new AnimationCurve();
                AnimationCurve localRot_W_Curve = new AnimationCurve();

                // Samplerを作成
                {
                    var FrameDataList = Frame.Value;

                    // FrameIndex順に並び替える
                    FrameDataList.Sort((a, b) => (a.FrameIndex.CompareTo(b.FrameIndex)));

                    List<Keyframe> localPos_X_KeyFrameList = new List<Keyframe>();
                    List<Keyframe> localPos_Y_KeyFrameList = new List<Keyframe>();
                    List<Keyframe> localPos_Z_KeyFrameList = new List<Keyframe>();
                    List<Keyframe> localRot_X_KeyFrameList = new List<Keyframe>();
                    List<Keyframe> localRot_Y_KeyFrameList = new List<Keyframe>();
                    List<Keyframe> localRot_Z_KeyFrameList = new List<Keyframe>();
                    List<Keyframe> localRot_W_KeyFrameList = new List<Keyframe>();

                    // KetFrame
                    foreach (var FrameData in FrameDataList)
				    {
                        int FrameIndex = FrameData.FrameIndex;
                        float CurrentTime = (float)(FrameIndex) * (1.0f / FrameRate);

                        var Pos = FrameData.Pos;
                        var Rot = FrameData.Rot;

                        localPos_X_KeyFrameList.Add(new Keyframe(CurrentTime, Pos.x));
                        localPos_Y_KeyFrameList.Add(new Keyframe(CurrentTime, Pos.y));
                        localPos_Z_KeyFrameList.Add(new Keyframe(CurrentTime, Pos.z));

                        localRot_X_KeyFrameList.Add(new Keyframe(CurrentTime, Rot.x));
                        localRot_Y_KeyFrameList.Add(new Keyframe(CurrentTime, Rot.y));
                        localRot_Z_KeyFrameList.Add(new Keyframe(CurrentTime, Rot.z));
                        localRot_W_KeyFrameList.Add(new Keyframe(CurrentTime, Rot.w));
                    }

                    // SamplerをClipに登録する
                    localPos_X_Curve.keys = localPos_X_KeyFrameList.ToArray();
                    localPos_Y_Curve.keys = localPos_Y_KeyFrameList.ToArray();
                    localPos_Z_Curve.keys = localPos_Z_KeyFrameList.ToArray();
                    localRot_X_Curve.keys = localRot_X_KeyFrameList.ToArray();
                    localRot_Y_Curve.keys = localRot_Y_KeyFrameList.ToArray();
                    localRot_Z_Curve.keys = localRot_Z_KeyFrameList.ToArray();
                    localRot_W_Curve.keys = localRot_W_KeyFrameList.ToArray();
                }

                string BonePath = "TmpBonePath_" + Index.ToString();

                clip.SetCurve(BonePath, typeof(Transform), "localPosition.x", localPos_X_Curve);
                clip.SetCurve(BonePath, typeof(Transform), "localPosition.y", localPos_Y_Curve);
                clip.SetCurve(BonePath, typeof(Transform), "localPosition.z", localPos_Z_Curve);
                clip.SetCurve(BonePath, typeof(Transform), "localRotation.x", localRot_X_Curve);
                clip.SetCurve(BonePath, typeof(Transform), "localRotation.y", localRot_Y_Curve);
                clip.SetCurve(BonePath, typeof(Transform), "localRotation.w", localRot_W_Curve);

                Index++;
            }

            // アセットを生成
            string ClipAssetName = Path.Combine(AssetFolder, AssetName);
            ClipAssetName += ".anim";

            AssetDatabase.CreateAsset(clip, ClipAssetName);

            return true;
        }
        
        static bool CreateBlendShapeClip(CVMDData vmd, string AssetFolder, string AssetName)
        {
            
            return true;
        }

        //static string CalcLinkBoneName(EHumanoidBones)
    }
}

