using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

// https://docs.unity3d.com/6000.3/Documentation/Manual/editor-EditorWindows.html
// about unsafe : https://annulusgames.com/blog/unity-nativearray/
namespace livesystem
{
    public unsafe class VATGenWindow : EditorWindow
    {
        public Object m_ClipObj = null;
        public Object m_TargetObj = null;

        [MenuItem("Assets/Create/Animation/Generate VAT")]
        public static void ShowWindow()
        {

            EditorWindow.GetWindow(typeof(VATGenWindow));
        }

        // ここでGUIを描画
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Animation Clip");
            m_ClipObj = EditorGUILayout.ObjectField(m_ClipObj, typeof(AnimationClip), false);

            EditorGUILayout.LabelField("Animate GameObject");
            m_TargetObj = EditorGUILayout.ObjectField(m_TargetObj, typeof(GameObject), false);

            if(GUILayout.Button("Generate") && m_ClipObj != null && m_TargetObj != null)
            {
                if(!Generate(m_ClipObj, m_TargetObj))
                {
                    throw new System.Exception("Failed to generate VAR.");
                }

                Debug.Log("Succeded to generate VAT.");
            }
        }

        private bool Generate(Object clipObj, Object animateObj)
        {
            AnimationClip clip = (AnimationClip)clipObj;

            // animateObjはプレファブ前提なので生成
            GameObject rootObject = Instantiate((GameObject)animateObj);

            Animator animator = rootObject.GetComponent<Animator>();
            if(animator == null) rootObject.AddComponent<Animator>();

            float FrameRate = clip.frameRate;
            float StartTime = 0.0f;
            float EndTime = clip.length;
            float DeltaTime = 1.0f / FrameRate;
            int NumOfFrame = (int)((EndTime - StartTime) / DeltaTime) + 1;

            // レンダラー単位でVATを生成する
            var MeshRenderers = rootObject.GetComponentsInChildren<SkinnedMeshRenderer>();

            // 全フレームにおけるゲームオブジェクトの姿勢をクリップを実際にシミュレートして計算し、それをテクスチャに焼き付ける
            // https://docs.unity3d.com/ja/560/ScriptReference/AnimationMode.html
            AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();

            foreach (var renderer in MeshRenderers)
            {
                NativeList<Matrix4x4> BoneWorldMatrixList = new NativeList<Matrix4x4>(Allocator.Temp);

                float ParseTime = 0.0f;
                while (ParseTime >= StartTime && ParseTime <= EndTime)
                {
                    AnimationMode.SampleAnimationClip(rootObject, clip, ParseTime);

                    RegistBoneWorldMatrixWithChild(rootObject.transform.localToWorldMatrix, renderer.rootBone, ref BoneWorldMatrixList);

                    // 経過時間を更新
                    ParseTime += DeltaTime;
                }

                AnimationMode.EndSampling();
                AnimationMode.StopAnimationMode();

                // 行列データをピクセルのbyteデータに変換
                NativeArray<byte> PixelData = new NativeArray<byte>(BoneWorldMatrixList.Length * sizeof(float) * 16, Allocator.Temp);

                // データを高速コピー
                UnsafeUtility.MemCpy(PixelData.GetUnsafePtr(), BoneWorldMatrixList.GetUnsafePtr(), PixelData.Length);

                // テクスチャ生成
                Texture2D VAT = null;
                {
                    // vec4が1つで1ピクセルとするのでmat4型(つまり1つのSkinMatrix)は4ピクセルで構成される
                    // それがボーン数だけ存在するのでそれらを考慮したものがテクスチャの幅となる
                    int TextureWidth = (int)(renderer.bones.Length) * (16 / 4);

                    // 縦にボーンのフレームごとのデータが並ぶのでフレーム数がそのままテクスチャの高さになる
                    int TextureHeight = NumOfFrame;

                    VAT = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBAFloat, false);
                    VAT.LoadRawTextureData<byte>(PixelData);
                    VAT.Apply();
                }

                // アセット生成
                if(VAT != null)
                {
                    // アセットを保存
                    string ClipAssetPath = AssetDatabase.GetAssetPath(clipObj);
                    string AssetName = Path.ChangeExtension(ClipAssetPath, "png");

                    byte[] pngBytes = VAT.EncodeToPNG();

                    File.WriteAllBytes(AssetName, pngBytes);

                    // アセットインポート
                    AssetDatabase.ImportAsset(AssetName, ImportAssetOptions.Default);
                }

                // メモリ解放
                BoneWorldMatrixList.Dispose();
                PixelData.Dispose();
            }

            // シーンに新規生成されてしまうので削除
            DestroyImmediate(rootObject);

            return true;
        }

        void RegistBoneWorldMatrixWithChild(Matrix4x4 RootWorldMatrix, Transform node, ref NativeList<Matrix4x4> BoneWorldMatrixList)
        {
            // 動かす可能性があるのでルートのワールド行列は省く
            Matrix4x4 WorldMatrix = RootWorldMatrix.inverse * node.localToWorldMatrix;

            BoneWorldMatrixList.Add(WorldMatrix);

            for (int c = 0; c < node.childCount; c++)
            {
                Transform childNode = node.GetChild(c);

                RegistBoneWorldMatrixWithChild(RootWorldMatrix, childNode, ref BoneWorldMatrixList);
            }
        }
    }
}

