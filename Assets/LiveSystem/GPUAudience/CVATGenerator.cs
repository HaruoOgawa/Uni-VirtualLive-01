using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace livesystem
{
    public static class CVATGenerator
    {
        [MenuItem("Assets/Create/Animation/VAT")]
        public static void CreateVAT()
        {
            var obj = Selection.activeObject;
            if (obj == null) return;

            if (obj.GetType() != typeof(AnimationClip)) return;

            AnimationClip clip = obj as AnimationClip;

            var CurveBindings = AnimationUtility.GetCurveBindings(clip);

            // CurveをBonePathで分類
            Dictionary<string, SBonePathData> pathCurveMap = new Dictionary<string, SBonePathData>();

            foreach (var binding in CurveBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                string path = binding.path;
                string propertyName = binding.propertyName;

                // 登録
                if (!pathCurveMap.ContainsKey(path))
                {
                    SBonePathData data = new SBonePathData();
                    data.BonePath = path;
                    data.CurveBindMap = new Dictionary<string, (AnimationCurve curve, EditorCurveBinding binding)>();

                    pathCurveMap.Add(path, data);
                }

                pathCurveMap[path].CurveBindMap.Add(binding.propertyName, (curve, binding));
            }

            float FrameRate = clip.frameRate;
            float StartTime = 0.0f;
            float EndTime = clip.length;
            float DeltaTime = 1.0f / FrameRate;
            int NumOfFrame = (int)((EndTime - StartTime) / DeltaTime) + 1;

            int SumOfMatrix = 0;

            //
            NativeArray<Matrix4x4> MatrixList = new NativeArray<Matrix4x4>();
            MatrixList.ResizeArray(NumOfFrame * pathCurveMap.Count);

            foreach (var pathCurve in pathCurveMap)
            {
                float ParseTime = 0.0f;

                SBonePathData pathData = pathCurve.Value;

                (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalPos_X = (new AnimationCurve(), new EditorCurveBinding());
                pathData.CurveBindMap.TryGetValue("m_LocalPosition.x", out CurveBinding_LocalPos_X);

                (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalPos_Y = (new AnimationCurve(), new EditorCurveBinding());
                pathData.CurveBindMap.TryGetValue("m_LocalPosition.y", out CurveBinding_LocalPos_Y);

                (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalPos_Z = (new AnimationCurve(), new EditorCurveBinding());
                pathData.CurveBindMap.TryGetValue("m_LocalPosition.z", out CurveBinding_LocalPos_Z);

                (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalRot_X = (new AnimationCurve(), new EditorCurveBinding());
                pathData.CurveBindMap.TryGetValue("m_LocalRotation.x", out CurveBinding_LocalRot_X);

                (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalRot_Y = (new AnimationCurve(), new EditorCurveBinding());
                pathData.CurveBindMap.TryGetValue("m_LocalRotation.y", out CurveBinding_LocalRot_Y);

                (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalRot_Z = (new AnimationCurve(), new EditorCurveBinding());
                pathData.CurveBindMap.TryGetValue("m_LocalRotation.z", out CurveBinding_LocalRot_Z);

                (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalRot_W = (new AnimationCurve(), new EditorCurveBinding());
                pathData.CurveBindMap.TryGetValue("m_LocalRotation.w", out CurveBinding_LocalRot_W);

                // 実際のアニメーションクリップの長さだけシミュレーションを回して各時間における姿勢を計算
                while (ParseTime >= StartTime && ParseTime <= EndTime)
                {
                    float PosOffset_X = CurveBinding_LocalPos_X.curve.Evaluate(ParseTime);
                    float PosOffset_Y = CurveBinding_LocalPos_Y.curve.Evaluate(ParseTime);
                    float PosOffset_Z = CurveBinding_LocalPos_Z.curve.Evaluate(ParseTime);

                    Vector3 Pos = new Vector3(PosOffset_X, PosOffset_Y, PosOffset_Z);

                    float QuatOffset_X = CurveBinding_LocalRot_X.curve.Evaluate(ParseTime);
                    float QuatOffset_Y = CurveBinding_LocalRot_Y.curve.Evaluate(ParseTime);
                    float QuatOffset_Z = CurveBinding_LocalRot_Z.curve.Evaluate(ParseTime);
                    float QuatOffset_W = CurveBinding_LocalRot_W.curve.Evaluate(ParseTime);

                    Quaternion Quat = new Quaternion(QuatOffset_X, QuatOffset_Y, QuatOffset_Z, QuatOffset_W);

                    // ワールド行列を作成
                    Matrix4x4 BoneWorldMatrix = Matrix4x4.Translate(Pos) * Matrix4x4.Rotate(Quat);

                    MatrixList[SumOfMatrix] = BoneWorldMatrix;
                    SumOfMatrix++;

                    // 経過時間を更新
                    ParseTime += DeltaTime;
                }
            }

            // 行列データをピクセルのbyteデータに変換
            NativeArray<byte> PixelData = MatrixList.Reinterpret<byte>(UnsafeUtility.SizeOf<Matrix4x4>());

            // VATを作成
            Texture2D VAT = new Texture2D(2, 2, TextureFormat.RGBAFloat, false);
            VAT.LoadRawTextureData<byte>(PixelData);

            // アセットを保存
            string ClipAssetPath = AssetDatabase.GetAssetPath(obj);
            string AssetName = Path.ChangeExtension(ClipAssetPath, "png");

            Debug.LogFormat("AssetName: {0}", AssetName);

            // PNGテクスチャとして保存
            {
                byte[] pngByte = VAT.EncodeToPNG();
                File.WriteAllBytes(AssetName, pngByte);
            }
        }
    }

    struct SBonePathData
    {
        public string BonePath;
        public Dictionary<string, (AnimationCurve curve, EditorCurveBinding binding)> CurveBindMap;
    }
}