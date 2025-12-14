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

            for(int RendererIndex = 0; RendererIndex < MeshRenderers.Length; RendererIndex++)
            {
                var renderer = MeshRenderers[RendererIndex];

                var rootBone = renderer.rootBone;

                NativeList<Matrix4x4> BoneWorldMatrixList = new NativeList<Matrix4x4>(Allocator.Temp);

                float ParseTime = 0.0f;

                bool IsFirstFrameLoop = false;
                int NumOfBone = 0;

                while (ParseTime >= StartTime && ParseTime <= EndTime)
                {
                    AnimationMode.SampleAnimationClip(rootObject, clip, ParseTime);

                    RegistBoneWorldMatrixWithChild(rootBone.transform.parent.localToWorldMatrix, rootBone, ref BoneWorldMatrixList);

                    // 経過時間を更新
                    ParseTime += DeltaTime;

                    // 最初のフレームにおけるボーンワールド行列の数をVATで処理するボーンの数とする
                    if(!IsFirstFrameLoop)
                    {
                        NumOfBone = BoneWorldMatrixList.Length;

                        IsFirstFrameLoop = true;
                    }
                }

                // 行列データをピクセルのbyteデータに変換
                NativeArray<byte> PixelData = new NativeArray<byte>(BoneWorldMatrixList.Length * sizeof(float) * 16, Allocator.Temp);

                // データを高速コピー
                UnsafeUtility.MemCpy(PixelData.GetUnsafePtr(), BoneWorldMatrixList.GetUnsafePtr(), PixelData.Length);

                // テクスチャ生成
                Texture2D VAT = null;
                {
                    // vec4が1つで1ピクセルとするのでmat4型(つまり1つのSkinMatrix)は4ピクセルで構成される
                    // それがボーン数だけ存在するのでそれらを考慮したものがテクスチャの幅となる
                    // 一方でUnityだとrendererに登録しているボーン数が処理したいボーン数と一致していないので
                    // 
                    int TextureWidth = NumOfBone * (16 / 4);

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

                    // PNGはRGBA32しか取り扱えなくてfloatの残りが失われてしまうのでEXRフォーマットとして保存する
                    string AssetName = Path.Combine(
                        Path.GetDirectoryName(ClipAssetPath),
                        Path.GetFileNameWithoutExtension(ClipAssetPath) + "_" + RendererIndex.ToString() + ".exr");
                        //Path.GetFileNameWithoutExtension(ClipAssetPath) + "_" + RendererIndex.ToString() + ".png");
                    
                    byte[] EXRBytes = VAT.EncodeToEXR();
                    //byte[] pngBytes = VAT.EncodeToPNG();

                    File.WriteAllBytes(AssetName, EXRBytes);
                    //File.WriteAllBytes(AssetName, pngBytes);

                    // アセットインポート
                    AssetDatabase.ImportAsset(AssetName, ImportAssetOptions.Default);

                    // テクスチャアセット設定を変更
                    TextureImporter textureImporter = AssetImporter.GetAtPath(AssetName) as TextureImporter;
                    if(textureImporter != null)
                    {
                        textureImporter.wrapMode = TextureWrapMode.Clamp;
                        textureImporter.filterMode = FilterMode.Point; // No Filter
                        textureImporter.npotScale = TextureImporterNPOTScale.None; // Non-Power of Two(強制的に2の累乗に変換するやつ)
                        //textureImporter.sRGBTexture = false; // これはいらないかも

                        // 再インポート
                        textureImporter.SaveAndReimport();
                    }
                }

                // メモリ解放
                BoneWorldMatrixList.Dispose();
                PixelData.Dispose();
            }

            AnimationMode.EndSampling();
            AnimationMode.StopAnimationMode();

            // シーンに新規生成されてしまうので削除
            DestroyImmediate(rootObject);

            return true;
        }

        void RegistBoneWorldMatrixWithChild(Matrix4x4 RootParentWorldMatrix, Transform node, ref NativeList<Matrix4x4> BoneWorldMatrixList)
        {
            // 動かす可能性があるのでルートボーンの親ワールド行列は省く
            Matrix4x4 WorldMatrix = RootParentWorldMatrix.inverse * node.localToWorldMatrix;

            BoneWorldMatrixList.Add(WorldMatrix);

            for (int c = 0; c < node.childCount; c++)
            {
                Transform childNode = node.GetChild(c);

                RegistBoneWorldMatrixWithChild(RootParentWorldMatrix, childNode, ref BoneWorldMatrixList);
            }
        }
    }
}

