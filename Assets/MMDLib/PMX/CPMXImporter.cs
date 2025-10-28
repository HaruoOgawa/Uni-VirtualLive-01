using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

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
                Debug.LogFormat("importPath: {0}", importPath);

                if (!Import(importPath, assetOutDir))
                {
                    string message = "[Error] Failed to import. importPath: " + importPath;

                    throw new System.Exception(message);
                }
            }
        }

        static bool Import(string fileName, string outDir)
        {
            CPmxModel model = new CPmxModel();
            if (!model.Analyse(fileName)) return false;

            return true;
        }
    }

}