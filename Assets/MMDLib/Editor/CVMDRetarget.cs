using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(AnimationClip))]
public class CVMDRetarget : Editor
{
    public Object pmxObj = null;

    public override void OnInspectorGUI()
    {
        // 既存のインスペクタを描画
        DrawDefaultInspector();

        // 区切り線
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("VMD ReTarget Tools", EditorStyles.boldLabel);

        pmxObj = EditorGUILayout.ObjectField(pmxObj, typeof(GameObject), false);
        
        // ボタンを追加
        if(GUILayout.Button("ReTarget"))
        {
            // CurveのRalativePathをリターゲットした新しいアニメーションクリップを作成
            if (target != null && pmxObj != null)
            {
                GameObject pmxGameObject = (GameObject)pmxObj;

                AnimationClip SrcClip = (AnimationClip)target;
                AnimationClip DstClip = new AnimationClip();

                var CurveBindings = AnimationUtility.GetCurveBindings(SrcClip);

                // PMXが持っているTransform一覧を取得
                Dictionary<string, Transform> BoneMap = new Dictionary<string, Transform>();
                FindChild(pmxGameObject.transform, ref BoneMap);

                // CurveをBonePathで分類
                Dictionary<string, SBonePathData> pathCurveMap = new Dictionary<string, SBonePathData>();

                foreach (EditorCurveBinding binding in CurveBindings)
                {
                    AnimationCurve SrcCurve = AnimationUtility.GetEditorCurve(SrcClip, binding);

                    Transform BoneTrans = null;
                    BoneMap.TryGetValue(binding.path, out BoneTrans);

                    if (BoneTrans == null) continue;

                    // カーブエディタのPathにはおそらくアニメーションしないノードは省略されて表示されない
                    // なので途中のノードがうまく書き出されていないように見えるが問題ないはず
                    //Debug.LogFormat("newBonePath: {0}", newBonePath);

                    // ボーンパスを再計算
                    // VMDに記載されている標準ボーン名をもとにあるPMXのそこからルートボーンまでのパスにする
                    // モデルによっては途中に標準ボーン以外が挟まっていてUnityアニメーションが動かないことがあるのでその対策
                    string newBonePath = BoneTrans.name;

                    Transform parentBoneTrans = BoneTrans.parent;

                    while (parentBoneTrans != null)
                    {
                        newBonePath = parentBoneTrans.name + "/" + newBonePath;

                        parentBoneTrans = parentBoneTrans.parent;

                        if (parentBoneTrans == pmxGameObject.transform) break;
                    }

                    // 登録
                    if(!pathCurveMap.ContainsKey(newBonePath))
                    {
                        SBonePathData data = new SBonePathData();
                        data.BonePath = newBonePath;
                        data.BoneTrans = BoneTrans;
                        data.CurveBindMap = new Dictionary<string, (AnimationCurve curve, EditorCurveBinding binding)>();

                        pathCurveMap.Add(newBonePath, data);
                    }

                    pathCurveMap[newBonePath].CurveBindMap.Add(binding.propertyName, (SrcCurve, binding));
                }

                foreach (var pathCurve in pathCurveMap) 
                {
                    SBonePathData pathData = pathCurve.Value;

                    (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalPos_X = (new AnimationCurve(), new EditorCurveBinding());
                    pathData.CurveBindMap.TryGetValue("m_LocalPosition.x", out CurveBinding_LocalPos_X);

                    (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalPos_Y = (new AnimationCurve(), new EditorCurveBinding());
                    pathData.CurveBindMap.TryGetValue("m_LocalPosition.y", out CurveBinding_LocalPos_Y);

                    (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalPos_Z = (new AnimationCurve(), new EditorCurveBinding());
                    pathData.CurveBindMap.TryGetValue("m_LocalPosition.z", out CurveBinding_LocalPos_Z);

                    (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalRot_X = (new AnimationCurve(), new EditorCurveBinding());
                    pathData.CurveBindMap.TryGetValue("m_LocalRotation.x", out CurveBinding_LocalRot_X);

                    (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalRot_Y = (new AnimationCurve(), new EditorCurveBinding());
                    pathData.CurveBindMap.TryGetValue("m_LocalRotation.y", out CurveBinding_LocalRot_Y);

                    (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalRot_Z = (new AnimationCurve(), new EditorCurveBinding());
                    pathData.CurveBindMap.TryGetValue("m_LocalRotation.z", out CurveBinding_LocalRot_Z);

                    (AnimationCurve curve, EditorCurveBinding binding) CurveBinding_LocalRot_W = (new AnimationCurve(), new EditorCurveBinding());
                    pathData.CurveBindMap.TryGetValue("m_LocalRotation.w", out CurveBinding_LocalRot_W);

                    // VMDアニメーションはあるフレームでの絶対座標ではなく初期座標に対するオフセットである
                    // つまり毎フレーム、1つ前のフレームよりどれぐらい移動しているかなのでPMXごとにVMDアニメーションクリップの
                    // キーフレームを再計算してあげる必要がある
                    AnimationCurve localPos_X_Curve = new AnimationCurve();
                    AnimationCurve localPos_Y_Curve = new AnimationCurve();
                    AnimationCurve localPos_Z_Curve = new AnimationCurve();
                    AnimationCurve localRot_X_Curve = new AnimationCurve();
                    AnimationCurve localRot_Y_Curve = new AnimationCurve();
                    AnimationCurve localRot_Z_Curve = new AnimationCurve();
                    AnimationCurve localRot_W_Curve = new AnimationCurve();

                    List<Keyframe> localPos_X_KeyFrameList = new List<Keyframe>();
                    List<Keyframe> localPos_Y_KeyFrameList = new List<Keyframe>();
                    List<Keyframe> localPos_Z_KeyFrameList = new List<Keyframe>();
                    List<Keyframe> localRot_X_KeyFrameList = new List<Keyframe>();
                    List<Keyframe> localRot_Y_KeyFrameList = new List<Keyframe>();
                    List<Keyframe> localRot_Z_KeyFrameList = new List<Keyframe>();
                    List<Keyframe> localRot_W_KeyFrameList = new List<Keyframe>();

                    float FrameRate = SrcClip.frameRate;
                    float StartTime = 0.0f;
                    float EndTime = SrcClip.length;
                    float DeltaTime = 1.0f / FrameRate;
                    float ParseTime = 0.0f;

                    // 初期トランスフォームを保存しておく
                    Vector3 DefaultLocalPos = pathData.BoneTrans.localPosition;
                    Quaternion DefaultLocalQuat = pathData.BoneTrans.localRotation;

                    // 実際のアニメーションクリップの長さだけシミュレーションを回して各時間における姿勢を再計算
                    while (ParseTime >= StartTime && ParseTime <= EndTime)
                    {
                        float PosOffset_X = CurveBinding_LocalPos_X.curve.Evaluate(ParseTime);
                        float PosOffset_Y = CurveBinding_LocalPos_Y.curve.Evaluate(ParseTime);
                        float PosOffset_Z = CurveBinding_LocalPos_Z.curve.Evaluate(ParseTime);

                        Vector3 PosOffset = new Vector3(PosOffset_X, PosOffset_Y, PosOffset_Z);

                        float QuatOffset_X = CurveBinding_LocalRot_X.curve.Evaluate(ParseTime);
                        float QuatOffset_Y = CurveBinding_LocalRot_Y.curve.Evaluate(ParseTime);
                        float QuatOffset_Z = CurveBinding_LocalRot_Z.curve.Evaluate(ParseTime);
                        float QuatOffset_W = CurveBinding_LocalRot_W.curve.Evaluate(ParseTime);

                        Quaternion QuatOffset = new Quaternion(QuatOffset_X, QuatOffset_Y, QuatOffset_Z, QuatOffset_W);

                        // 新しいローカル座標を計算
                        Vector3 NewPos = DefaultLocalPos + PosOffset;
                        Quaternion NewQuat = DefaultLocalQuat * QuatOffset;

                        // キーフレームを追加
                        localPos_X_KeyFrameList.Add(new Keyframe(ParseTime, NewPos.x));
                        localPos_Y_KeyFrameList.Add(new Keyframe(ParseTime, NewPos.y));
                        localPos_Z_KeyFrameList.Add(new Keyframe(ParseTime, NewPos.z));

                        localRot_X_KeyFrameList.Add(new Keyframe(ParseTime, NewQuat.x));
                        localRot_Y_KeyFrameList.Add(new Keyframe(ParseTime, NewQuat.y));
                        localRot_Z_KeyFrameList.Add(new Keyframe(ParseTime, NewQuat.z));
                        localRot_W_KeyFrameList.Add(new Keyframe(ParseTime, NewQuat.w));

                        // 経過時間を更新
                        ParseTime += DeltaTime;
                    }

                    // SamplerをClipに登録する
                    localPos_X_Curve.keys = localPos_X_KeyFrameList.ToArray();
                    localPos_Y_Curve.keys = localPos_Y_KeyFrameList.ToArray();
                    localPos_Z_Curve.keys = localPos_Z_KeyFrameList.ToArray();
                    localRot_X_Curve.keys = localRot_X_KeyFrameList.ToArray();
                    localRot_Y_Curve.keys = localRot_Y_KeyFrameList.ToArray();
                    localRot_Z_Curve.keys = localRot_Z_KeyFrameList.ToArray();
                    localRot_W_Curve.keys = localRot_W_KeyFrameList.ToArray();

                    DstClip.SetCurve(pathData.BonePath, typeof(Transform), "localPosition.x", localPos_X_Curve);
                    DstClip.SetCurve(pathData.BonePath, typeof(Transform), "localPosition.y", localPos_Y_Curve);
                    DstClip.SetCurve(pathData.BonePath, typeof(Transform), "localPosition.z", localPos_Z_Curve);

                    DstClip.SetCurve(pathData.BonePath, typeof(Transform), "localRotation.x", localRot_X_Curve);
                    DstClip.SetCurve(pathData.BonePath, typeof(Transform), "localRotation.y", localRot_Y_Curve);
                    DstClip.SetCurve(pathData.BonePath, typeof(Transform), "localRotation.z", localRot_Z_Curve);
                    DstClip.SetCurve(pathData.BonePath, typeof(Transform), "localRotation.w", localRot_W_Curve);
                }

                //
                DstClip.frameRate = SrcClip.frameRate;

                // アセットを再生成
                string SrcAssetPath = AssetDatabase.GetAssetPath(target);

                string DstFolder = Path.GetDirectoryName(SrcAssetPath);
                string DstFileName = Path.GetFileNameWithoutExtension(SrcAssetPath) + "_ReTarget_" + pmxGameObject.name + Path.GetExtension(SrcAssetPath);
                string DstAssetPath = Path.Combine(DstFolder, DstFileName);

                AssetDatabase.CreateAsset(DstClip, DstAssetPath);

                Debug.LogFormat("ReTarget {0} to {1}.", SrcClip.name, pmxGameObject.name);
            }
        }
    }

    static void FindChild(Transform currentBone, ref Dictionary<string, Transform> BoneMap)
    {
        BoneMap.Add(currentBone.name, currentBone);
        
        for(int i = 0; i < currentBone.childCount; i++)
        {
            Transform childBone = currentBone.GetChild(i);

            FindChild(childBone, ref BoneMap);
        }
    }
}

struct SBonePathData
{
    public string BonePath;
    public Transform BoneTrans;
    public Dictionary<string, (AnimationCurve curve, EditorCurveBinding binding)> CurveBindMap;
}
