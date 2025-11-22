using srp;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        // コンポーネントを追加
        PlannerReflection plannerReflection = PRObject.AddComponent<PlannerReflection>();

        // 平面反射用のカメラを子要素とコンポーネントに登録
        GameObject ReflectCameraObject = new GameObject("ReflectCamera");
        ReflectCameraObject.transform.parent = PRObject.transform;

        Camera ReflectCamera = ReflectCameraObject.AddComponent<Camera>();
        plannerReflection.ReflectCamera = ReflectCamera;

        // 平面反射用のカメラだということをパイプラインに伝えるためのタグを付与する
        AddPlannerReflectionTag();
        ReflectCamera.tag = PLANNER_REFLECT_CAMERA;

        // 平面反射描画用の平面を追加
        GameObject Plane = new GameObject("Plane");
        Plane.transform.parent = PRObject.transform;
        Plane.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);

        plannerReflection.Plane = Plane;

        MeshRenderer renderer = Plane.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Shader.Find("CustomSRP/PlannerReflection"));

        MeshFilter meshFilter = Plane.AddComponent<MeshFilter>(); // SkinedMeshRendererだったら不要だが、MeshRendererにはMeshFilterがないとメッシュデータが登録できない
        meshFilter.mesh = CSceneController.CreateFullscreenMesh();

        // 平面反射用のカメラが描画中はこの平面を描画しないようにするためのレイヤーを追加
        AddPlannerReflectionLayer();

        Plane.layer = LayerMask.NameToLayer(PLANNER_REFLECT_PLANE);

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

            RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGBFloat);

            string FileName = "ReflectRenderTexture_" + System.Guid.NewGuid().ToString() + ".renderTexture";
            string AssetName = Path.Combine(SceneFolder, FileName);
            AssetDatabase.CreateAsset(renderTexture, AssetName);

            // レンダーテクスチャをカメラと平面にアサイン
            ReflectCamera.targetTexture = renderTexture;
            renderer.sharedMaterial.SetTexture("_BaseMap", renderTexture);
        }

        // UndoできるようにUndoシステムに登録する
        Undo.RegisterCreatedObjectUndo(PRObject, "Create" +  PRObject.name);

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
    
    static void AddPlannerReflectionLayer()
    {
        // SerializedObject : https://docs.unity3d.com/ja/560/ScriptReference/SerializedObject.html
        // UnityのObjectをシリアライズ(データ化)された状態で読むためのAPI
        // これを介してUnity Objectのテキスト情報を読み取ったり編集したりすることができる
        // Unity Object(.assetだったり.animだったりUnityでファイルとして扱えるもの全般)はUnity独自のYAML形式で表される
        // SerializedObjectはこれを読み書きする

        // TagManagerにlayer情報が書き込まれているのでこれを取ってくる
        SerializedObject TagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        var layers = TagManager.FindProperty("layers");

        SerializedProperty EmptyProp = null;

        for (int i = 0; i < layers.arraySize; i++)
        {
            SerializedProperty prop = layers.GetArrayElementAtIndex(i);

            if (prop.stringValue == "" || prop.stringValue == string.Empty)
            {
                // 空レイヤーが残ていれば後ほどの新規登録用に保持しておく
                if (EmptyProp == null) EmptyProp = prop;
            }
            else if (prop.stringValue.IndexOf(PLANNER_REFLECT_PLANE) != -1)
            {
                // PLANNER_REFLECT_PLANEというレイヤーが1つでもあればレイヤー追加を終了する
                return;
            }
        }

        // 空のレイヤーが足りない時はエラーにする
        if (EmptyProp != null)
        {
            EmptyProp.stringValue = PLANNER_REFLECT_PLANE;
        }

        // 反映
        TagManager.ApplyModifiedProperties();
    }
}
