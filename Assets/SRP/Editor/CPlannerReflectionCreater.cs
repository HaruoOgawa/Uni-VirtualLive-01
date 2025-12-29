using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using srp.effect;

namespace srp.editor
{
    public static class CPlannerReflectionCreater
    {
        const string PLANNER_REFLECT_CAMERA = "PLANNER_REFLECT_CAMERA";
        const string PLANNER_REFLECT_PLANE = "PLANNER_REFLECT_PLANE";

        // https://docs.unity3d.com/ja/2023.1/ScriptReference/MenuItem.html
        [MenuItem("GameObject/Rendering/Planner Reflection")]
        public static void CreateObject()
        {
            // ゲームオブジェクトを新規作成
            GameObject PRObject = new GameObject("Planner Reflection");
            PRObject.AddComponent<PlannerReflection>();

            // 平面反射用カメラの子要素とコンポーネントを追加
            GameObject ReflectCameraObject = new GameObject("ReflectCamera");
            ReflectCameraObject.transform.parent = PRObject.transform;

            Camera ReflectCamera = ReflectCameraObject.AddComponent<Camera>();

            // 平面反射用のカメラだということをパイプラインに伝えるためのタグを付与する
            AddPlannerReflectionTag();
            ReflectCamera.tag = PLANNER_REFLECT_CAMERA;

            // レンダーテクスチャ
            {
                // Sceneと同じディレクトリにシーン名のフォルダを作ってそこにレンダーテクスチャを生成
                string ScenePath = SceneManager.GetActiveScene().path;
                string SceneFolder = Path.Combine(Path.GetDirectoryName(ScenePath), Path.GetFileNameWithoutExtension(ScenePath));

                if (!AssetDatabase.IsValidFolder(SceneFolder))
                {
                    // 存在しなければ生成
                    string GUID = AssetDatabase.CreateFolder(Path.GetDirectoryName(ScenePath), Path.GetFileNameWithoutExtension(ScenePath));
                    SceneFolder = AssetDatabase.GUIDToAssetPath(GUID);
                }

                RenderTexture renderTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGBFloat);
                renderTexture.useMipMap = true;

                string FileName = "ReflectRenderTexture_" + System.Guid.NewGuid().ToString() + ".renderTexture";
                string AssetName = Path.Combine(SceneFolder, FileName);
                AssetDatabase.CreateAsset(renderTexture, AssetName);

                // レンダーテクスチャをカメラと平面にアサイン
                ReflectCamera.targetTexture = renderTexture;
                //renderer.sharedMaterial.SetTexture("_BaseMap", renderTexture);
            }

            // UndoできるようにUndoシステムに登録する
            Undo.RegisterCreatedObjectUndo(PRObject, "Create" + PRObject.name);

            // 作成されたオブジェクトにフォーカスが当たるようにする
            Selection.activeObject = PRObject;
        }

        static void AddPlannerReflectionTag()
        {
            // SerializedObject : https://docs.unity3d.com/ja/560/ScriptReference/SerializedObject.html
            // UnityのObjectをシリアライズ(データ化)された状態で読むためのAPI
            // これを介してUnity Objectのテキスト情報を読み取ったり編集したりすることができる
            // Unity Object(.assetだったり.animだったりUnityでファイルとして扱えるもの全般)はUnity独自のYAML形式で表される
            // SerializedObjectはこれを読み書きする

            // TagManagerにlayer情報が書き込まれているのでこれを取ってくる
            SerializedObject TagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var tags = TagManager.FindProperty("tags");

            // 既にタグが存在しないかチェック
            for (int i = 0; i < tags.arraySize; i++)
            {
                SerializedProperty prop = tags.GetArrayElementAtIndex(i);

                if (prop.stringValue.IndexOf(PLANNER_REFLECT_CAMERA) != -1)
                {
                    // PLANNER_REFLECT_CAMERAというタグが1つでもあればタグ追加を終了する
                    return;
                }
            }

            // 新規でtagsのプロパティを追加
            int NewIndex = tags.arraySize;
            tags.InsertArrayElementAtIndex(NewIndex);

            SerializedProperty NewProp = tags.GetArrayElementAtIndex(NewIndex);

            if (NewProp != null)
            {
                NewProp.stringValue = PLANNER_REFLECT_CAMERA;
            }

            // 反映
            TagManager.ApplyModifiedProperties();
        }
    }
}