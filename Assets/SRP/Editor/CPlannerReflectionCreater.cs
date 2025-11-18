using UnityEngine;
using UnityEditor;
using srp;

public static class CPlannerReflectionCreater
{
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

        // 平面反射描画用の平面を追加
        GameObject Plane = new GameObject("Plane");
        Plane.transform.parent = PRObject.transform;
        Plane.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);

        plannerReflection.Plane = Plane;

        MeshRenderer renderer = Plane.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Shader.Find("CustomSRP/PlannerReflection"));

        MeshFilter meshFilter = Plane.AddComponent<MeshFilter>(); // SkinedMeshRendererだったら不要だが、MeshRendererにはMeshFilterがないとメッシュデータが登録できない
        meshFilter.mesh = CSceneController.CreateFullscreenMesh();

        // UndoできるようにUndoシステムに登録する
        Undo.RegisterCreatedObjectUndo(PRObject, "Create" +  PRObject.name);

        // 作成されたオブジェクトにフォーカスが当たるようにする
        Selection.activeObject = PRObject; 
    }
}
