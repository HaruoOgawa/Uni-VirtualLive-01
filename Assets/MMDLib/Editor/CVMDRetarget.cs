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

                Dictionary<string, Transform> BoneMap = new Dictionary<string, Transform>();
                FindChild(pmxGameObject.transform, ref BoneMap);

                foreach (EditorCurveBinding bind in CurveBindings) 
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(SrcClip, bind);

                    Transform BoneTrans = null;
                    BoneMap.TryGetValue(bind.path, out BoneTrans);

                    if (BoneTrans == null) continue;

                    string newBonePath = BoneTrans.name;

                    Transform parentBoneTrans = BoneTrans.parent;

                    while (parentBoneTrans != null)
                    {
                        newBonePath = parentBoneTrans.name + "/" + newBonePath;

                        parentBoneTrans = parentBoneTrans.parent;

                        if (parentBoneTrans == pmxGameObject.transform) break;
                    }

                    // カーブエディタのPathにはおそらくアニメーションしないノードは省略されて表示されない
                    // なので途中のノードがうまく書き出されていないように見えるが問題ないはず
                    //Debug.LogFormat("newBonePath: {0}", newBonePath);

                    DstClip.SetCurve(newBonePath, bind.type, bind.propertyName, curve);
                }

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
