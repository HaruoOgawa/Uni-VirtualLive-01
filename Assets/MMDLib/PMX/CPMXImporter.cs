using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            if (!CreateAnimationSkeleton(model, ref rootNode, ref NodeList)) return false;

            // テクスチャリスト
            List<Texture> TextureList = new List<Texture>();
            //if (!CreateTextureList(model, srcFolder, ref TextureList, FolderMap)) return false;

            // マテリアルリスト
            Dictionary<string, Material> MaterialMap = new Dictionary<string, Material>();
            if (!CreateMaterialList(model, ref NodeList, ref MaterialMap, TextureList, FolderMap)) return false;

            // メッシュ
            if (!CreateMeshList(model, ref rootNode, ref NodeList, MaterialMap, FolderMap)) return false;

            // 物理演算

            // プレファブを生成
            string PrefabFolder = string.Empty;
            if (!FolderMap.TryGetValue("Prefab", out PrefabFolder)) return false;

            string PrefabName = Path.Combine(PrefabFolder, rootName);
            PrefabName += ".prefab";

            PrefabUtility.SaveAsPrefabAsset(rootNode, PrefabName);

            return true;
        }

        static bool CreateAnimationSkeleton(CPmxModel model, ref GameObject rootNode, ref List<GameObject> NodeList)
        {
            var PmxBoneList = model.GetPmxBoneList();

            List<(GameObject Node, CPmxBone PmxBone)> BoneList = new List<(GameObject, CPmxBone)>();

            for(int BoneIndex = 0; BoneIndex < PmxBoneList.Count; BoneIndex++)
            {
                var PmxBone = PmxBoneList[BoneIndex];
                if(PmxBone == null) continue;

                // BoneNodeの作成
                GameObject BoneNode = new GameObject(PmxBone.GetBoneName());
                
                Vector3 Pos = PmxBone.GetPos();
                //Quaternion Rot = PmxBone->GetLocalAxis();

                // PMXのPos・Rotateはワールド座標系での値なので親ノードのワールドマトリックスを乗算してローカル座標系に戻す必要がある
                int ParentBoneIndex = PmxBone.GetParentBoneIndex();
                if (ParentBoneIndex >= 0 && ParentBoneIndex < PmxBoneList.Count)
                {
                    var ParentPmxBone = PmxBoneList[ParentBoneIndex];

                    // Posはワールド座標系なのでローカル座標系に戻す必要がある
                    // ただしRotは(存在すれば)ローカル軸から取得するので既にローカル座標系である
                    Pos -= ParentPmxBone.GetPos();
                }

                BoneNode.transform.position = Pos;

                BoneList.Add((BoneNode, PmxBone));
            }

            // ボーンの親子関係を構築
            foreach(var bonePair in BoneList)
            {
                GameObject BoneNode = bonePair.Node;
                if(BoneNode == null) continue;

                CPmxBone PmxBone = bonePair.PmxBone;
                if(PmxBone == null) continue;

                // 親ボーンを取得
                int ParentBoneIndex = PmxBone.GetParentBoneIndex();
                if (ParentBoneIndex >= 0 && ParentBoneIndex < PmxBoneList.Count)
                {
                    var parentBonePair = BoneList[ParentBoneIndex];

                    GameObject ParentBoneNode = parentBonePair.Node;
                    if (ParentBoneNode == null) continue;

                    BoneNode.transform.parent = ParentBoneNode.transform;
                }
                else
                {
                    // ルートノードを親とする
                    BoneNode.transform.parent = rootNode.transform;
                }
            }

            return true;
        }

        static bool CreateTextureList(CPmxModel model, string srcFolder, ref List<Texture> TextureList,
            Dictionary<string, string> FolderMap)
        {
            string TextureFolder = string.Empty;
            if (!FolderMap.TryGetValue("Textures", out TextureFolder)) return false;

            foreach(var PmxTexture in model.GetPmxTextureList())
            {
                string TexturePath = Path.Combine(srcFolder, PmxTexture.GetFilePath());

                string fileName = Path.GetFileName(TexturePath);
                string TexAssetName = Path.Combine(TextureFolder, fileName);

                bool result = AssetDatabase.CopyAsset(TexturePath, TexAssetName);

                Debug.LogFormat("TexturePath: {0}, TexAssetName: {1}, result: {2}", TexturePath, TexAssetName, result);

                /*Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                Debug.LogFormat("texture.name: {0}", texture.name);
                if (texture == null)
                {
                    Debug.LogErrorFormat("Failed to load Texture. TexturePath: {0}", TexturePath);

                    return false;
                }

                // アセットを保存
                string fileName = Path.GetFileName(TexturePath);

                string TexAssetName = Path.Combine(TextureFolder, fileName);
                AssetDatabase.CreateAsset(texture, TexAssetName);
                
                TextureList.Add(texture); 
                */

            }

            return true;
        }

        static bool CreateMaterialList(CPmxModel model, ref List<GameObject> NodeList, ref Dictionary<string, Material> MaterialMap,
            List<Texture> TextureList, Dictionary<string, string> FolderMap)
        {
            string MaterialFolder = string.Empty;
            if (!FolderMap.TryGetValue("Materials", out MaterialFolder)) return false;

            foreach(var PmxMaterial in model.GetPmxMaterialList())
            {
                if (PmxMaterial == null) continue;

                string MaterialName = PmxMaterial.GetMaterialName();

                Material material = new Material(Shader.Find("MMDLib/BasicToon"));

                // カリング
                // https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Rendering.CullMode.html
                CullMode cullMode = CullMode.Back;
                if (PmxMaterial.IsDrawDoubleSlided()) cullMode = CullMode.Off;

                material.SetInteger("_Cull", (int)cullMode);

                // ブレンドモード
                BlendMode blendSrc = BlendMode.SrcAlpha;
                material.SetInteger("_BlendSrc", (int)blendSrc);

                BlendMode blendDst = BlendMode.OneMinusSrcAlpha;
                material.SetInteger("_BlendDst", (int)blendDst);

                // ShaderUniformをセット
                material.SetFloat("_EdgeSize", PmxMaterial.GetEdgeSize());
                material.SetFloat("_SpecularIntensity", PmxMaterial.GetSpecularCoef());

                material.SetColor("_DiffuseFactor", PmxMaterial.GetDiffuse());
                material.SetColor("_AmbientFactor", PmxMaterial.GetAmbient());
                material.SetColor("_SpecularFactor", PmxMaterial.GetSpecular());
                material.SetColor("_EdgeColor", PmxMaterial.GetEdgeColor());

                // MainTexture
                int MainTexIndex = PmxMaterial.GetMainTexIndex();
                if(MainTexIndex >= 0 && MainTexIndex < TextureList.Count)
                {
                    Texture texture = TextureList[MainTexIndex];
                    material.SetTexture("_MainTexture", texture);
                }

                // ToonTexture
                int ToonTexIndex = PmxMaterial.GetToonTexIndex();
                int SharedToonTexIndex = PmxMaterial.GetSharedToonTexIndex();

                if (ToonTexIndex >= 0 && ToonTexIndex < TextureList.Count)
                {
                    Texture texture = TextureList[ToonTexIndex];
                    material.SetTexture("_ToonTexture", texture);
                }
                else if(SharedToonTexIndex >= 0)
                {
                    // 未対応
                }

                // SphereTexture
                int SphereTexIndex = PmxMaterial.GetSphereTexIndex();
                if (SphereTexIndex >= 0 && SphereTexIndex < TextureList.Count)
                {
                    Texture texture = TextureList[SphereTexIndex];
                    material.SetTexture("_SphereTexture", texture);
                }

                //
                MaterialMap.Add(MaterialName, material);

                // マテリアルアセット生成
                string MaterialAssetName = Path.Combine(MaterialFolder, MaterialName);
                MaterialAssetName += ".mat";

                AssetDatabase.CreateAsset(material, MaterialAssetName);
            }

            return true;
        }

        static bool CreateMeshList(CPmxModel model, ref GameObject rootNode, ref List<GameObject> NodeList, 
            Dictionary<string, Material> MaterialMap, Dictionary<string, string> FolderMap)
        {
            string MeshFolder = string.Empty;
            if (!FolderMap.TryGetValue("Meshs", out MeshFolder)) return false;

            CPmxMesh PmxMesh = model.GetPmxMesh();
            if (PmxMesh == null) return true;

            var PmxMaterialList = model.GetPmxMaterialList();

            // 明示的にMeshNodeを作成
            GameObject MeshNode = new GameObject("BaseMeshNode");
            MeshNode.transform.parent = rootNode.transform;

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

                // バウンディングボックスを再計算
                mesh.RecalculateBounds();

                // メッシュアセットを作成
                string MeshAssetName = Path.Combine(MeshFolder, "BaseMesh");
                MeshAssetName += ".mesh";

                AssetDatabase.CreateAsset(mesh, MeshAssetName);

                // スキンメッシュレンダラーを作成

            }

            return true;
        }

        // Helper
        //bool memcpy)
    }

}