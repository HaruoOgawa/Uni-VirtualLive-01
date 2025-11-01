using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
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
            List<Mesh> MeshList = new List<Mesh>();

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
    }

}