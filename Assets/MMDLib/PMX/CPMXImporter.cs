using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

// ScriptedImporter
// https://docs.unity3d.com/6000.2/Documentation/ScriptReference/AssetImporters.ScriptedImporter.html

namespace mmdlib
{
    [ScriptedImporter(1, "pmx")]
    public class CPMXImporter : ScriptedImporter
    {
        const string PMX_PHYSICS_LAYER = "PMX_PHYSICS_LAYER";

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
                if (DDExtension != ".pmx") continue;

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
                // フォルダリスト生成
                Dictionary<string, string> FolderMap = new Dictionary<string, string>();
                CreateFolderList(importPath, assetOutDir, ref FolderMap);

                // インポート
                if (!Import(importPath, assetOutDir, FolderMap))
                {
                    string message = "[Error] Failed to import. importPath: " + importPath;

                    throw new System.Exception(message);
                }
            }
        }

        static bool CreateFolderList(string fileName, string outDir, ref Dictionary<string, string> FolderMap)
        {
            // ルート
            string RootFolderName = string.Empty;
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                string guid = AssetDatabase.CreateFolder(outDir, name);
                RootFolderName = AssetDatabase.GUIDToAssetPath(guid);

                FolderMap.Add("Root", RootFolderName);
            }

            if (RootFolderName == string.Empty) return false;

            // マテリアル
            {
                string name = "Materials";
                string guid = AssetDatabase.CreateFolder(RootFolderName, name);
                
                string Folder = AssetDatabase.GUIDToAssetPath(guid);

                FolderMap.Add("Materials", Folder);
            }

            // メッシュ
            {
                string name = "Meshs";
                string guid = AssetDatabase.CreateFolder(RootFolderName, name);

                string Folder = AssetDatabase.GUIDToAssetPath(guid);

                FolderMap.Add("Meshs", Folder);
            }
            
            // テクスチャ
            {
                string name = "Textures";
                string guid = AssetDatabase.CreateFolder(RootFolderName, name);

                string Folder = AssetDatabase.GUIDToAssetPath(guid);

                FolderMap.Add("Textures", Folder);
            }

            // アバター
            {
                string name = "Avatar";
                string guid = AssetDatabase.CreateFolder(RootFolderName, name);

                string Folder = AssetDatabase.GUIDToAssetPath(guid);

                FolderMap.Add("Avatar", Folder);
            }

            // プレファブ
            {
                string name = "Prefab";
                string guid = AssetDatabase.CreateFolder(RootFolderName, name);

                string Folder = AssetDatabase.GUIDToAssetPath(guid);

                FolderMap.Add("Prefab", Folder);
            }

            return true;
        }

        static bool Import(string fileName, string outDir, Dictionary<string, string> FolderMap)
        {
            string srcFolder = Path.GetDirectoryName(fileName);

            CPmxModel model = new CPmxModel();
            if (!model.Analyse(fileName)) return false;

            List<GameObject> NodeList = new List<GameObject>();

            // ルートノードを生成
            string rootName = Path.GetFileNameWithoutExtension(fileName);
            GameObject rootNode = new GameObject(rootName);
            NodeList.Add(rootNode);

            // Skeleton
            GameObject rootBone = null;
            List<Transform> BoneTransformList = new List<Transform>();
            if (!CreateAnimationSkeleton(model, ref rootNode, ref rootBone, ref NodeList, ref BoneTransformList, FolderMap)) return false;

            // テクスチャリスト
            List<string> TexturePathList = new List<string>();
            if (!CreateTextureList(model, srcFolder, ref TexturePathList, FolderMap)) return false;

            // マテリアルリスト
            List<Material> MaterialList = new List<Material>();
            if (!CreateMaterialList(model, ref NodeList, ref MaterialList, TexturePathList, FolderMap)) return false;

            // メッシュ
            if (!CreateMeshList(model, ref rootNode, rootBone, ref NodeList, BoneTransformList, MaterialList, FolderMap)) return false;

            // 物理演算
            List<PmxPhysicsObject> PhysicsObjectList = new List<PmxPhysicsObject>();
            if (!CreateRigidbody(model, ref rootNode, rootBone.GetComponent<PmxSkeleton>(), ref PhysicsObjectList)) return false;

            if (!CreateJoint(model, rootBone.GetComponent<PmxSkeleton>(), PhysicsObjectList)) return false;

            // プレファブを生成
            string PrefabFolder = string.Empty;
            if (!FolderMap.TryGetValue("Prefab", out PrefabFolder)) return false;

            string PrefabName = Path.Combine(PrefabFolder, rootName);
            PrefabName += ".prefab";

            PrefabUtility.SaveAsPrefabAsset(rootNode, PrefabName);

            return true;
        }

        static bool CreateAnimationSkeleton(CPmxModel model, ref GameObject rootNode, ref GameObject rootBone, 
            ref List<GameObject> NodeList, ref List<Transform> BoneTransformList, Dictionary<string, string> FolderMap)
        {
            var PmxBoneList = model.GetPmxBoneList();
            
            List<(GameObject Node, CPmxBone PmxBone)> NodeBoneList = new List<(GameObject, CPmxBone)>();

            List<SkeletonBone> UnitySkeletonBoneList = new List<SkeletonBone>();
            List<HumanBone> UnityHumanBoneList = new List<HumanBone>();

            List<PmxBone> RuntimePmxBoneList = new List<PmxBone>();

            for (int BoneIndex = 0; BoneIndex < PmxBoneList.Count; BoneIndex++)
            {
                var PmxBone = PmxBoneList[BoneIndex];
                if(PmxBone == null) continue;

                // BoneNodeの作成
                string BoneName = CPmxHumanoidBoneMapper.GetStrBoneName(PmxBone.GetHumanoidBone());
                if (BoneName == string.Empty) BoneName = PmxBone.GetBoneName();

                GameObject BoneNode = new GameObject(BoneName);
                
                Vector3 Pos = PmxBone.GetPos();
                //Quaternion Rot = PmxBone.GetLocalAxis();
               
                // PMXのPos・Rotateはワールド座標系なので直接Transformのワールドポジションに渡す
                BoneNode.transform.position = Pos;

                NodeBoneList.Add((BoneNode, PmxBone));

                //
                NodeList.Add(BoneNode);
                BoneTransformList.Add(BoneNode.transform);

                // 全ての親ならルートボーンとする
                if(PmxBone.GetHumanoidBone() == EHumanoidBones.AllParent)
                {
                    rootBone = BoneNode;

                    // RuntimeのPMXスケルトンコンポーネントを追加
                    rootBone.AddComponent<PmxSkeleton>();
                }

                // RuntimeのPmxBoneを作成
                {
                    BoneNode.AddComponent<PmxBone>();

                    PmxBone Bone = BoneNode.GetComponent<PmxBone>();

                    // BoneにBoneNameを割り当てる
                    Bone.SetBoneName(PmxBone.GetHumanoidBone());

                    // ボーンの付与
                    if (PmxBone.IsRotateGrant())
                    {
                        // 回転付与
                        Bone.SetRotateGrant(PmxBone.GetGrantParentBoneIndex(), PmxBone.GetGrantRate());
                    }
                    else if (PmxBone.IsMoveGrant())
                    {
                        // 移動付与
                        Bone.SetMoveGrant(PmxBone.GetGrantParentBoneIndex(), PmxBone.GetGrantRate());
                    }

                    // IK
                    Bone.SetIKParam(PmxBone.GetIKParam());

                    //
                    RuntimePmxBoneList.Add(Bone);
                }
            }

            // ボーンの親子関係を構築
            foreach(var boneNonePair in NodeBoneList)
            {
                GameObject BoneNode = boneNonePair.Node;
                if(BoneNode == null) continue;

                CPmxBone PmxBone = boneNonePair.PmxBone;
                if(PmxBone == null) continue;

                PmxBone ParentBpne = null;

                // 親ボーンを取得
                int ParentBoneIndex = PmxBone.GetParentBoneIndex();
                if (ParentBoneIndex >= 0 && ParentBoneIndex < PmxBoneList.Count)
                {
                    var parentBonePair = NodeBoneList[ParentBoneIndex];

                    GameObject ParentBoneNode = parentBonePair.Node;
                    if (ParentBoneNode == null) continue;

                    BoneNode.transform.parent = ParentBoneNode.transform;

                    ParentBpne = ParentBoneNode.GetComponent<PmxBone>();
                }
                else
                {
                    // ルートノードを親とする
                    BoneNode.transform.parent = rootNode.transform;
                }

                // Unityボーン
                SkeletonBone uniSkeletonBone = new SkeletonBone();
                uniSkeletonBone.name = BoneNode.name;
                uniSkeletonBone.position = BoneNode.transform.localPosition;
                uniSkeletonBone.rotation = BoneNode.transform.localRotation;
                uniSkeletonBone.scale = BoneNode.transform.localScale;

                UnitySkeletonBoneList.Add(uniSkeletonBone);

                // Unityヒューマンボーン
                if (PmxBone.GetHumanoidBone() != EHumanoidBones.None && rootBone != null)
                {
                    HumanBone UniHumanBone = new HumanBone();
                    UniHumanBone.boneName = BoneNode.name;
                    UniHumanBone.humanName = BoneNode.name;

                    UnityHumanBoneList.Add(UniHumanBone);
                }

                // デフォルトトランスフォームを保存 
                PmxBone Bone = BoneNode.GetComponent<PmxBone>();
                if(Bone != null)
                {
                    Bone.SaveAsDefaultLocalTransform();

                    if (ParentBpne != null) Bone.SetParentBoneName(ParentBpne.GetBoneName());
                }
            }

            // Avatar作成
            string AvatarFolder = string.Empty;
            if (rootBone != null && FolderMap.TryGetValue("Avatar", out AvatarFolder))
            {
                HumanDescription humanDesc = new HumanDescription();
                humanDesc.skeleton = UnitySkeletonBoneList.ToArray();
                humanDesc.human = UnityHumanBoneList.ToArray();

                Avatar avatar = AvatarBuilder.BuildHumanAvatar(rootBone, humanDesc);

                // アバターアセット作成
                string AvatarAssetName = Path.Combine(AvatarFolder, rootNode.name);
                AvatarAssetName += ".asset";

                AssetDatabase.CreateAsset(avatar, AvatarAssetName);
            }

            // Skeletonにボーンリストを追加
            if (rootBone != null)
            {
                PmxSkeleton skeleton = rootBone.GetComponent<PmxSkeleton>();
                if (skeleton != null)
                {
                    skeleton.SetBoneList(RuntimePmxBoneList);
                    skeleton.MakeIKBoneList();
                    skeleton.MakeGrantBoneList();
                }
            }

            return true;
        }

        static bool CreateTextureList(CPmxModel model, string srcFolder, ref List<string> TexturePathList,
            Dictionary<string, string> FolderMap)
        {
            string TextureFolder = string.Empty;
            if (!FolderMap.TryGetValue("Textures", out TextureFolder)) return false;

            foreach(var PmxTexture in model.GetPmxTextureList())
            {
                string TexturePath = Path.Combine(srcFolder, PmxTexture.GetFilePath());

                string fileName = Path.GetFileName(TexturePath);
                string TexAssetName = Path.Combine(TextureFolder, fileName);

                // ファイルをコピー
                File.Copy(TexturePath, TexAssetName, true);

                // Unityでインポートを実行
                AssetDatabase.ImportAsset(TexAssetName, ImportAssetOptions.Default);

                // インポート後にアセットを取得
                Texture asset = AssetDatabase.LoadAssetAtPath<Texture>(TexAssetName);

                TexturePathList.Add(TexAssetName);
            }

            return true;
        }

        static bool CreateMaterialList(CPmxModel model, ref List<GameObject> NodeList, ref List<Material> MaterialList,
            List<string> TexturePathList, Dictionary<string, string> FolderMap)
        {
            string MaterialFolder = string.Empty;
            if (!FolderMap.TryGetValue("Materials", out MaterialFolder)) return false;

            foreach(var PmxMaterial in model.GetPmxMaterialList())
            {
                if (PmxMaterial == null) continue;

                string MaterialName = PmxMaterial.GetMaterialName();

                Material material = new Material(Shader.Find("MMDLib/BasicToon"));

                MaterialList.Add(material);

                // マテリアルアセット生成
                string MaterialAssetName = Path.Combine(MaterialFolder, MaterialName);
                MaterialAssetName += ".mat";

                AssetDatabase.CreateAsset(material, MaterialAssetName);
                
                // 次のエディタフレームで実行するコールバック
                EditorApplication.delayCall += () =>
                {
                    // カリング
                    // https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Rendering.CullMode.html
                    CullMode cullMode = CullMode.Back;
                    if (PmxMaterial.IsDrawDoubleSlided()) cullMode = CullMode.Off;

                    material.SetInteger("_Cull", (int)cullMode);

                    // アウトライン
                    material.SetInt("_DrawEdge", (PmxMaterial.IsDrawEdge() ? 1 : 0));

                    // ShaderUniformをセット
                    material.SetFloat("_EdgeSize", PmxMaterial.GetEdgeSize());
                    material.SetFloat("_SpecularIntensity", PmxMaterial.GetSpecularCoef());

                    material.SetColor("_DiffuseFactor", PmxMaterial.GetDiffuse());
                    material.SetColor("_AmbientFactor", PmxMaterial.GetAmbient());
                    material.SetColor("_SpecularFactor", PmxMaterial.GetSpecular());
                    material.SetColor("_EdgeColor", PmxMaterial.GetEdgeColor());

                    // アルファレンダリングを行うか
                    bool UseAlpha = (PmxMaterial.GetDiffuse().w > 0.0f);

                    // MainTexture
                    int MainTexIndex = PmxMaterial.GetMainTexIndex();

                    if (MainTexIndex >= 0 && MainTexIndex < TexturePathList.Count)
                    {
                        // CreateTextureListで作ったTextureオブジェクトはまだインポート中でそれをmaterial.SetTextureに
                        // 使うと消えてしまうので遅延コールバック内でAssetDatabase.LoadAssetAtPathで新規ロードする
                        string path = TexturePathList[MainTexIndex];

                        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                        material.SetTexture("_MainTexture", texture);

                        // メインテクスチャがアルファチャンネルを持っているかチェックする
                        if(!UseAlpha)
                        {
                            for (int y = 0; y < texture.height; y++)
                            {
                                for (int x = 0; x < texture.width; x++)
                                {
                                    Color col = texture.GetPixel(x, y);

                                    if(col.a < 1.0f)
                                    {
                                        UseAlpha = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // ToonTexture
                    int ToonTexIndex = PmxMaterial.GetToonTexIndex();
                    int SharedToonTexIndex = PmxMaterial.GetSharedToonTexIndex();

                    // 10枚しか存在しない想定
                    const int NumOfSharedToon = 10;

                    if (ToonTexIndex >= 0 && ToonTexIndex < TexturePathList.Count)
                    {
                        // CreateTextureListで作ったTextureオブジェクトはまだインポート中でそれをmaterial.SetTextureに
                        // 使うと消えてしまうので遅延コールバック内でAssetDatabase.LoadAssetAtPathで新規ロードする
                        string path = TexturePathList[ToonTexIndex];

                        material.SetTexture("_ToonTexture", AssetDatabase.LoadAssetAtPath<Texture>(path));
                    }
                    else if (SharedToonTexIndex >= 0 && SharedToonTexIndex <= NumOfSharedToon)
                    {
                        // CreateTextureListで作ったTextureオブジェクトはまだインポート中でそれをmaterial.SetTextureに
                        // 使うと消えてしまうので遅延コールバック内でAssetDatabase.LoadAssetAtPathで新規ロードする
                        int number = SharedToonTexIndex + 1;
                        string path = "Assets/MMDLib/Texture/SharedToon/toon" + number.ToString("00") + ".bmp";

                        Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(path);

                        material.SetTexture("_ToonTexture", texture);
                    }

                    // SphereTexture
                    int SphereTexIndex = PmxMaterial.GetSphereTexIndex();
                    if (SphereTexIndex >= 0 && SphereTexIndex < TexturePathList.Count)
                    {
                        // CreateTextureListで作ったTextureオブジェクトはまだインポート中でそれをmaterial.SetTextureに
                        // 使うと消えてしまうので遅延コールバック内でAssetDatabase.LoadAssetAtPathで新規ロードする
                        string path = TexturePathList[SphereTexIndex];

                        material.SetTexture("_SphereTexture", AssetDatabase.LoadAssetAtPath<Texture>(path));
                    }

                    // ブレンドモード
                    BlendMode blendSrc = (UseAlpha)? BlendMode.SrcAlpha : BlendMode.One;
                    material.SetInteger("_BlendSrc", (int)blendSrc);

                    BlendMode blendDst = (UseAlpha) ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero;
                    material.SetInteger("_BlendDst", (int)blendDst);

                    // RenderType
                    string RenderType = (UseAlpha) ? "Transparent" : "Opaque";
                    material.SetOverrideTag("RenderType", RenderType);

                    // Queue
                    RenderQueue renderQueue = (UseAlpha) ? RenderQueue.Transparent : RenderQueue.Geometry;
                    material.renderQueue = (int)renderQueue;

                    // マテリアルの変更を保存する(テクスチャのバインドを保持しておくために必要。これがないとテクスチャが消える)
                    EditorUtility.SetDirty(material);
                    AssetDatabase.SaveAssetIfDirty(material);
                };
            }

            return true;
        }

        static bool CreateMeshList(CPmxModel model, ref GameObject rootNode, GameObject rootBone, ref List<GameObject> NodeList,
            List<Transform> BoneTransformList, List<Material> MaterialList, Dictionary<string, string> FolderMap)
        {
            string MeshFolder = string.Empty;
            if (!FolderMap.TryGetValue("Meshs", out MeshFolder)) return false;

            CPmxMesh PmxMesh = model.GetPmxMesh();
            if (PmxMesh == null) return true;

            var PmxMaterialList = model.GetPmxMaterialList();

            // 明示的にMeshNodeを作成
            GameObject MeshNode = new GameObject("BaseMeshNode");
            MeshNode.transform.parent = rootNode.transform;
            NodeList.Add(MeshNode);

            {
                // メッシュを作成する
                Mesh mesh = new Mesh();

                // メタデータ
                SPmxMetaData MetaData = model.GetMetaData();

                // VertexBuffer
                {
                    var PmxPosition = PmxMesh.GetPositionAttribute();
                    var PmxNormal = PmxMesh.GetNormalAttribute();
                    var PmxUV = PmxMesh.GetUVAttribute();
                    var PmxTangent = PmxMesh.GetTangentAttribute();

                    // Position
                    if(PmxPosition != null && PmxPosition.Count > 0)
                    {
                        int NumOfElm = PmxPosition.Count / 3;

                        Vector3[] vertices = new Vector3[NumOfElm];

                        for(int elmIndex = 0; elmIndex < NumOfElm; elmIndex ++)
                        {
                            vertices[elmIndex] = new Vector3(
                                PmxPosition[elmIndex * 3 + 0],
                                PmxPosition[elmIndex * 3 + 1],
                                PmxPosition[elmIndex * 3 + 2]
                            );
                        }

                        //Array.Copy(PmxPosition.ToArray(), 0, vertices, 0, PmxPosition.Count);

                        mesh.vertices = vertices;
                    }

                    // Normal
                    if(PmxNormal != null && PmxNormal.Count > 0)
                    {
                        int NumOfElm = PmxNormal.Count / 3;

                        Vector3[] normals = new Vector3[NumOfElm];

                        for (int elmIndex = 0; elmIndex < NumOfElm; elmIndex++)
                        {
                            normals[elmIndex] = new Vector3(
                                PmxNormal[elmIndex * 3 + 0],
                                PmxNormal[elmIndex * 3 + 1],
                                PmxNormal[elmIndex * 3 + 2]
                            );
                        }

                        //Array.Copy(PmxNormal.ToArray(), 0, normals, 0, PmxNormal.Count);

                        mesh.normals = normals;
                    }
                    
                    // UV
                    if(PmxUV != null && PmxUV.Count > 0)
                    {
                        int NumOfElm = PmxUV.Count / 2;

                        Vector2[] uv = new Vector2[NumOfElm];

                        for (int elmIndex = 0; elmIndex < NumOfElm; elmIndex++)
                        {
                            uv[elmIndex] = new Vector2(
                                PmxUV[elmIndex * 2 + 0],
                                PmxUV[elmIndex * 2 + 1]
                            );
                        }

                        //Array.Copy(PmxUV.ToArray(), 0, uv, 0, PmxUV.Count);

                        mesh.uv = uv;
                    }

                    // Tangent
                    if (PmxTangent != null && PmxTangent.Count > 0)
                    {
                        int NumOfElm = PmxTangent.Count / 4;

                        Vector4[] tangents = new Vector4[NumOfElm];

                        for (int elmIndex = 0; elmIndex < NumOfElm; elmIndex++)
                        {
                            tangents[elmIndex] = new Vector4(
                                PmxTangent[elmIndex * 4 + 0],
                                PmxTangent[elmIndex * 4 + 1],
                                PmxTangent[elmIndex * 4 + 2],
                                PmxTangent[elmIndex * 4 + 3]
                            );
                        }

                        //Array.Copy(PmxTangent.ToArray(), 0, tangents, 0, PmxTangent.Count);

                        mesh.tangents = tangents;
                    }
                    else
                    {
                        mesh.RecalculateTangents();
                    }

                    // BoneIndex(JointIndex)・BoneWieght
                    List<BoneWeight> boneWeights = new List<BoneWeight>();

                    var PmxByteBoneAttribute = PmxMesh.GetByteBoneAttribute();
                    var PmxUShortBoneAttribute = PmxMesh.GetUShortBoneAttribute();
                    var PmxUIntBoneAttribute = PmxMesh.GetUIntBoneAttribute();
                    
                    var PmxWeights = PmxMesh.GetWeightAttribute();

                    for(int bwIndex = 0; bwIndex < (PmxWeights.Count / 4); bwIndex ++)
                    {
                        BoneWeight boneWeight = new BoneWeight();

                        if (MetaData.BoneIndexSize == 1)
                        {
                            boneWeight.boneIndex0 = (int)PmxByteBoneAttribute[bwIndex * 4 + 0];
                            boneWeight.boneIndex1 = (int)PmxByteBoneAttribute[bwIndex * 4 + 1];
                            boneWeight.boneIndex2 = (int)PmxByteBoneAttribute[bwIndex * 4 + 2];
                            boneWeight.boneIndex3 = (int)PmxByteBoneAttribute[bwIndex * 4 + 3];
                        }
                        else if (MetaData.BoneIndexSize == 2)
                        {
                            boneWeight.boneIndex0 = (int)PmxUShortBoneAttribute[bwIndex * 4 + 0];
                            boneWeight.boneIndex1 = (int)PmxUShortBoneAttribute[bwIndex * 4 + 1];
                            boneWeight.boneIndex2 = (int)PmxUShortBoneAttribute[bwIndex * 4 + 2];
                            boneWeight.boneIndex3 = (int)PmxUShortBoneAttribute[bwIndex * 4 + 3];
                        }
                        else if (MetaData.BoneIndexSize == 4)
                        {
                            boneWeight.boneIndex0 = (int)PmxUIntBoneAttribute[bwIndex * 4 + 0];
                            boneWeight.boneIndex1 = (int)PmxUIntBoneAttribute[bwIndex * 4 + 1];
                            boneWeight.boneIndex2 = (int)PmxUIntBoneAttribute[bwIndex * 4 + 2];
                            boneWeight.boneIndex3 = (int)PmxUIntBoneAttribute[bwIndex * 4 + 3];
                        }

                        boneWeight.weight0 = PmxWeights[bwIndex * 4 + 0];
                        boneWeight.weight1 = PmxWeights[bwIndex * 4 + 1];
                        boneWeight.weight2 = PmxWeights[bwIndex * 4 + 2];
                        boneWeight.weight3 = PmxWeights[bwIndex * 4 + 3];

                        boneWeights.Add(boneWeight);
                    }

                    mesh.boneWeights = boneWeights.ToArray();
                }

                // IndexBuffer
                {
                    int IndexBufferOffset = 0;

                    var ByteIndices = PmxMesh.GetByteIndices();
                    var UShortIndices = PmxMesh.GetUShortIndices();
                    var UIntIndices = PmxMesh.GetUIntIndices();

                    if (MetaData.VertexIndexSize == 1)
                    {
                        // byte型のインデックスの時はushortにキャストする
                        mesh.indexFormat = IndexFormat.UInt16;
                    }
                    else if (MetaData.VertexIndexSize == 2)
                    {
                        mesh.indexFormat = IndexFormat.UInt16;
                    }
                    else if (MetaData.VertexIndexSize == 4)
                    {
                        mesh.indexFormat = IndexFormat.UInt32;
                    }

                    // サブメッシュ数を指定
                    mesh.subMeshCount = PmxMaterialList.Count;

                    // 頂点バッファをインデックスバッファで分けてサブメッシュを構築
                    for (int SubMeshIndex = 0; SubMeshIndex < PmxMaterialList.Count; SubMeshIndex++)
                    {
                        var PmxMaterial = PmxMaterialList[SubMeshIndex];

                        int IndiceCount = PmxMaterial.GetMatRefIndiceCount();

                        if (MetaData.VertexIndexSize == 1)
                        {
                            // byte型のインデックスの時はushortにキャストする
                            ushort[] indices = new ushort[IndiceCount];

                            for(int n = 0; n < ByteIndices.Count; n++)
                            {
                                byte index = ByteIndices[n];

                                indices[n] = (ushort)index;
                            }

                            mesh.SetIndices(indices, MeshTopology.Triangles, SubMeshIndex);
                        }
                        else if (MetaData.VertexIndexSize == 2)
                        {
                            ushort[] indices = new ushort[IndiceCount];

                            Array.Copy(UShortIndices.ToArray(), IndexBufferOffset, indices, 0, IndiceCount);

                            mesh.SetIndices(indices, MeshTopology.Triangles, SubMeshIndex);

                        }
                        else if (MetaData.VertexIndexSize == 4)
                        {
                            int[] indices = new int[IndiceCount];

                            Array.Copy(UIntIndices.ToArray(), IndexBufferOffset, indices, 0, IndiceCount);

                            mesh.SetIndices(indices, MeshTopology.Triangles, SubMeshIndex);
                        }

                        IndexBufferOffset += IndiceCount;
                    }
                }

                // BlendShape
                {
                    foreach(var MorphPair in model.GetPmxVertexMorphList())
                    {
                        string BlensShapeName = MorphPair.Key;
                        CPmxMorphTarget morphTarget = MorphPair.Value;

                        Vector3[] VertexMorphList = new Vector3[mesh.vertexCount];
                        Array.Fill<Vector3>(VertexMorphList, Vector3.zero);

                        foreach(var PmxVertexMorphPair in morphTarget.GetVertexMorphList())
                        {
                            VertexMorphList[PmxVertexMorphPair.Key] = PmxVertexMorphPair.Value;
                        }

                        mesh.AddBlendShapeFrame(BlensShapeName, 100.0f, VertexMorphList, null, null);
                    }

                }

                // バウンディングボックスを再計算
                mesh.RecalculateBounds();

                // 逆バインドポーズリストを作成
                List<Matrix4x4> bindPoses = new List<Matrix4x4>();

                foreach(var BoneTransform in BoneTransformList)
                {
                    bindPoses.Add(BoneTransform.localToWorldMatrix.inverse);
                }

                mesh.bindposes = bindPoses.ToArray();

                // メッシュアセットを作成
                string MeshAssetName = Path.Combine(MeshFolder, "BaseMesh");
                MeshAssetName += ".mesh";

                AssetDatabase.CreateAsset(mesh, MeshAssetName);

                // スキンメッシュレンダラーを作成
                MeshNode.AddComponent<SkinnedMeshRenderer>();

                SkinnedMeshRenderer skinnedMeshRenderer = MeshNode.GetComponent<SkinnedMeshRenderer>();

                skinnedMeshRenderer.sharedMesh = mesh;
                skinnedMeshRenderer.materials = MaterialList.ToArray();

                skinnedMeshRenderer.bones = BoneTransformList.ToArray();
                skinnedMeshRenderer.rootBone = rootBone.transform;

                skinnedMeshRenderer.localBounds = mesh.bounds;
            }

            return true;
        }

        static bool CreateRigidbody(CPmxModel model, ref GameObject rootNode, PmxSkeleton Skeleton, ref List<PmxPhysicsObject> PhysicsObjectList)
	    {
            // 物理演算グループ分け用のレイヤーを作成
            AddPhysicsGroupLayer();

            //
            GameObject PhysicsRoot = new GameObject("PhysicsObjectList");
            PhysicsRoot.transform.parent = rootNode.transform;

            var BoneList = Skeleton.GetPmxBoneList();

            foreach(var PmxRigidbody in model.GetPmxRigidbodyList())
		    {
                if(PmxRigidbody == null) continue;

                // 関連ンボーンを取得
                if (PmxRigidbody.RelationBoneIndex < 0 || PmxRigidbody.RelationBoneIndex >= BoneList.Count) continue;
                PmxBone Bone = BoneList[PmxRigidbody.RelationBoneIndex];
                Vector3 BonePos = Bone.transform.position;

                //
                GameObject PhysicsNode = new GameObject(PmxRigidbody.RigidbodyName);
                PhysicsNode.transform.parent = PhysicsRoot.transform;

                // 物理オブジェクトを作成
                Vector3 RBPos = PmxRigidbody.Pos;
                Vector3 RBRotate = PmxRigidbody.Rotate;
                Vector3 RBSize = PmxRigidbody.Size;

                // 位置にはボーンの位置を反映し、BonePosとRBPosの差分をColliderのオフセット(Center)として使用する
                PhysicsNode.transform.localPosition = BonePos;
                Vector3 ColliderOffset = RBPos - BonePos;

                PhysicsNode.transform.localRotation = Quaternion.Euler(Mathf.Rad2Deg * RBRotate);
                //PhysicsNode.transform.localScale = RBSize;

                // Offsetに回転を考慮する
                ColliderOffset = Matrix4x4.Rotate(PhysicsNode.transform.localRotation).inverse * ColliderOffset;

                // 物理オブジェクトを割り当てる
                if (PmxRigidbody.PhysicsShape == EPmxPhysicsShape.SPHERE)
			    {
                    PhysicsNode.AddComponent<SphereCollider>();

                    SphereCollider collider = PhysicsNode.GetComponent<SphereCollider>();
                    collider.center = ColliderOffset;
                    collider.radius = RBSize.x;

                }
                else if (PmxRigidbody.PhysicsShape == EPmxPhysicsShape.BOX)
                {
                    PhysicsNode.AddComponent<BoxCollider>();

                    BoxCollider collider = PhysicsNode.GetComponent<BoxCollider>();
                    collider.center = ColliderOffset;
                    collider.size = RBSize;
                }
                else if (PmxRigidbody.PhysicsShape == EPmxPhysicsShape.CAPSULE)
                {
                    PhysicsNode.AddComponent<CapsuleCollider>();

                    CapsuleCollider collider = PhysicsNode.GetComponent<CapsuleCollider>();
                    collider.center = ColliderOffset;
                    collider.radius = RBSize.x;
                    collider.height = RBSize.y;
                }
                else
                {
                    continue;
                }

                // RigidBodyを追加
                PhysicsNode.AddComponent<Rigidbody>();

                Rigidbody rigidbody = PhysicsNode.GetComponent<Rigidbody>();
                rigidbody.isKinematic = (PmxRigidbody.PhysicsType == EPmxPhysicsType.STATIC || PmxRigidbody.Mass == 0.0);
                rigidbody.mass = PmxRigidbody.Mass;
                rigidbody.linearDamping = PmxRigidbody.TransDamping;
                rigidbody.angularDamping = PmxRigidbody.RotateDamping;

                // 自身の衝突グループ(レイヤー)を設定
                List<string> SelfGroup = GetPmxPhysicsLayerList(PmxRigidbody.group);
                if(SelfGroup.Count == 1) PhysicsNode.layer = LayerMask.NameToLayer(SelfGroup[0]);

                // 非衝突グループ(レイヤー)リストのビットマスクを設定
                List<string> NoCollideGroupList = GetPmxPhysicsLayerList(PmxRigidbody.NoneCollideGroupFlag);
                rigidbody.excludeLayers = LayerMask.GetMask(NoCollideGroupList.ToArray());

                // 非衝突グループ以外のレイヤーを衝突グループとしてビットマスクを設定
                ushort CollideGroupFlag = BitConverter.ToUInt16(BitConverter.GetBytes(~PmxRigidbody.NoneCollideGroupFlag), 0);
                List<string> CollideGroupList = GetPmxPhysicsLayerList(CollideGroupFlag);
                rigidbody.includeLayers = LayerMask.GetMask(CollideGroupList.ToArray());

                // PmxPhysicsObjectを作成
                PhysicsNode.AddComponent<PmxPhysicsObject>();

                PmxPhysicsObject PhysicsObject = PhysicsNode.GetComponent<PmxPhysicsObject>();
                PhysicsObject.Init(PmxRigidbody.PhysicsType, PhysicsNode);

                PhysicsObjectList.Add(PhysicsObject);

                // 関連ボーンに物理オブジェクトを追加
                Bone.AddPhysicsObject(PhysicsObject);
            }

		    return true;
	    }

        static void AddPhysicsGroupLayer()
        {
            // SerializedObject : https://docs.unity3d.com/ja/560/ScriptReference/SerializedObject.html
            // UnityのObjectをシリアライズ(データ化)された状態で読むためのAPI
            // これを介してUnity Objectのテキスト情報を読み取ったり編集したりすることができる
            // Unity Object(.assetだったり.animだったりUnityでファイルとして扱えるもの全般)はUnity独自のYAML形式で表される
            // SerializedObjectはこれを読み書きする

            // TagManagerにlayer情報が書き込まれているのでこれを取ってくる
            SerializedObject TagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var layers = TagManager.FindProperty("layers");
            
            List<SerializedProperty> EmptyPropList = new List<SerializedProperty>();

            for (int i = 0; i < layers.arraySize; i++)
            {
                SerializedProperty prop = layers.GetArrayElementAtIndex(i);
                
                if(prop.stringValue == "" || prop.stringValue == string.Empty)
                {
                    // 空レイヤーが残ていれば後ほどの新規登録用に保持しておく
                    EmptyPropList.Add(prop);
                }
                else if(prop.stringValue.IndexOf(PMX_PHYSICS_LAYER) != -1)
                {
                    // PMX_PHYSICS_LAYERというレイヤーが1つでもあればレイヤー追加を終了する
                    return;
                }
            }

            // 空のレイヤーが足りない時はエラーにする
            if(EmptyPropList.Count < 16)
            {
                throw new Exception("There are not enough empty layers to add PMX physics layers.");
            }

            // PMX_PHYSICS_LAYERを16個分作成
            for(int i = 0; i < 16; i++)
            {
                int LayerIndex = i + 1;

                EmptyPropList[i].stringValue = PMX_PHYSICS_LAYER + "_" + LayerIndex.ToString();
            }

            // 反映
            TagManager.ApplyModifiedProperties();
        }

        static List<string> GetPmxPhysicsLayerList(ushort byteOrder)
        {
            List<string> LayerNameList = new List<string>();

            for(int i = 0; i < 16; i++)
            {
                // 0000 0000 0000 0001 (0x0001) の16ビットをレイヤーの数だけシフト演算してそれが存在するかチェックする
                bool Exist = ((0x0001 << i) & byteOrder) != 0;
                if (Exist)
                {
                    int LayerIndex = i + 1;

                    string LayerName = PMX_PHYSICS_LAYER + "_" + LayerIndex.ToString();
                    LayerNameList.Add(LayerName);
                }
            }

            return LayerNameList;
        }

        static bool CreateJoint(CPmxModel model, PmxSkeleton Skeleton, List<PmxPhysicsObject> PhysicsObjectList)
	    {
		    var PmxRigidbodyList = model.GetPmxRigidbodyList();

		    foreach(var PmxJoint in model.GetPmxJointList())
		    {
			    // PhysicsObjectを取得
			    // BodyA(Fixed)
			    int BodyAIndex = PmxJoint.BodyAIndex;
			    if (BodyAIndex < 0 || BodyAIndex >= PmxRigidbodyList.Count) continue;

			    var FixedPhysicsObj = PhysicsObjectList[BodyAIndex];

			    // BodyB(Dynamic)
			    int BodyBIndex = PmxJoint.BodyBIndex;
			    if (BodyBIndex < 0 || BodyBIndex >= PmxRigidbodyList.Count) continue;

                var DynamicPhysicsObj = PhysicsObjectList[BodyBIndex];
                SPmxRigidbody pmxRigidbodyB = PmxRigidbodyList[BodyBIndex];

                if (FixedPhysicsObj == null || DynamicPhysicsObj == null) continue;


                // SpringBoneを追加
                DynamicPhysicsObj.AddComponent<SpringJoint>();

                SpringJoint[] springJointList = DynamicPhysicsObj.GetComponents<SpringJoint>();

                foreach(var springJoint in springJointList)
                {
                    if (springJoint == null) continue;

                    // SpringJointを複数個持たせる可能性があるのでconnectedBodyが常に設定されていたら他も設定済みということにしてスキップする
                    if (springJoint.connectedBody != null) continue;

                    springJoint.connectedBody = FixedPhysicsObj.GetComponent<Rigidbody>();
                    springJoint.enableCollision = true;
                    springJoint.damper = pmxRigidbodyB.TransDamping;
                    springJoint.spring = PmxJoint.TransSpring.x;

                    break;
                }

                

                //springJoint.anchor
                //springJoint.maxDistance
                //springJoint.tolerance

                // 移動できる範囲・回転できる範囲を設定
                // bulletではLimitのLowerとUpperが同じ値ならその軸は固定される
                // http://bulletjpn.web.fc2.com/08_Constraints.html
                /*
                 Lowerlimit == Upperlimit -> この軸はロックされます。
                 Lowerlimit > Upperlimit -> この軸は自由に動けます。
                 Lowerlimit < Upperlimit -> この軸の角度は制限されます。
                 */
                Rigidbody rigidbody = DynamicPhysicsObj.GetComponent<Rigidbody>();

                if (rigidbody != null) 
                {
                    RigidbodyConstraints constraints = RigidbodyConstraints.None;

                    if (PmxJoint.LowerTransLimit.x == PmxJoint.UpperTransLimit.x) constraints |= RigidbodyConstraints.FreezePositionX;
                    if (PmxJoint.LowerTransLimit.y == PmxJoint.UpperTransLimit.y) constraints |= RigidbodyConstraints.FreezePositionY;
                    if (PmxJoint.LowerTransLimit.z == PmxJoint.UpperTransLimit.z) constraints |= RigidbodyConstraints.FreezePositionZ;

                    if (PmxJoint.LowerRotateLimit.x == PmxJoint.UpperRotateLimit.x) constraints |= RigidbodyConstraints.FreezeRotationX;
                    if (PmxJoint.LowerRotateLimit.y == PmxJoint.UpperRotateLimit.y) constraints |= RigidbodyConstraints.FreezeRotationY;
                    if (PmxJoint.LowerRotateLimit.z == PmxJoint.UpperRotateLimit.z) constraints |= RigidbodyConstraints.FreezeRotationZ;

                    rigidbody.constraints = constraints;
                }
            }

            return true;
	    }
    }

}