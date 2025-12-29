using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.ProjectWindowCallback;

namespace srp.editor
{
    public static class CHLSLCreator
    {
        [MenuItem("Assets/Create/Shader/HLSL")]
        public static void CreateFile()
        {
            // 現在開いているフォルダを取得
            var obj = Selection.activeObject;

            // パスを取得
            string CurrentFolder = AssetDatabase.GetAssetPath(obj);
            string FilePath = CurrentFolder + "/" + "NewShader" + ".hlsl";

            // ファイル名指定後のアクション
            var endNameEditAction = ScriptableObject.CreateInstance<CustomEndNameEditAction>();

            // ファイルを名前入力待機状態で一時生成
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(obj.GetInstanceID(), endNameEditAction, FilePath, null, "");
        }
    }

    public class CustomEndNameEditAction : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            // AssetDatabase.CreateAssetだと「.hlsl」が対応してなくてエラーがでるのでファイルをピュアC#で生成てアセットをインポートする
            string emptyData = "";
            File.WriteAllText(pathName, emptyData);

            AssetDatabase.ImportAsset(pathName);
        }
    }
}