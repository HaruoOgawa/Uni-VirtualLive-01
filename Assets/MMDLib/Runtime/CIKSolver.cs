using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using Unity.Mathematics;
using UnityEngine;

namespace mmdlib
{
    [System.Serializable]
    public class CIKSolver
    {
        public SIKParam m_IKParam = null;
        public PmxBoneComponent m_IKTarget = null;
        public List<PmxBoneComponent> m_IKChainList = new List<PmxBoneComponent>();

		public CIKSolver()
		{
		}

        public bool Create(PmxBoneComponent IKTargetBone, List<PmxBoneComponent> BoneList)
		{
			m_IKTarget = IKTargetBone;
			m_IKParam = IKTargetBone.GetIKParam();

			// ChainListを作成
			for (int i = (m_IKParam.IKLinkList.Count - 1); i >= 0; i--)
			{
				var IKLink = m_IKParam.IKLinkList[i];

				int BoneIndex = IKLink.IKLinkBoneIndex;
				if (BoneIndex < 0 || BoneIndex >= BoneList.Count) return false;

				m_IKChainList.Add(BoneList[BoneIndex]);
			}

			// EndEffectorをChainの末尾に追加
			int EndEffectorIndex = m_IKParam.IKTargetBoneIndex;
			if (EndEffectorIndex < 0 || EndEffectorIndex >= BoneList.Count) return false;

			// 根本から先端の方向でLinkNodeが入っている
			m_IKChainList.Add(BoneList[EndEffectorIndex]);

			return true;
		}

		public bool Solve()
        {
            CPmxTransform[] PmxTransformList = new CPmxTransform[m_IKChainList.Count];

            // まずLinkNodeを初期姿勢に戻す
            // T-Pose(元の姿勢)にリセットして演算を行うことで演算結果が安定するようになる
            // このようにしないと途中で変な方向を向いたりぶるぶるしたりして不安定になる
            for (int n = 0; n < m_IKChainList.Count; n++)
            {
                PmxBoneComponent LinkNode = m_IKChainList[n];

                PmxTransformList[n] = new CPmxTransform();

                PmxTransformList[n].SetLocalPos(LinkNode.m_DefaultLocalPos);
                PmxTransformList[n].SetLocalRot(LinkNode.m_DefaultLocalRot);
                PmxTransformList[n].SetLocalScale(LinkNode.m_DefaultLocalScale);

                Matrix4x4 LocalMatrix = PmxTransformList[n].GetLocalMatrix();

                // Linkノードのワールド行列を再計算する
                PmxBoneComponent ParentNode = LinkNode.GetParentNode();
                // 始点(先頭リンク)の親ワールド行列を無視する
                // これを考慮すると例えば体を捻った時にIKが暴れてしまう
                if (ParentNode == null || n == 0)
                {
                    // 親ノードがない時はローカル行列をワールド行列として渡す
                    PmxTransformList[n].SetWorldMatrix(LocalMatrix);

                    continue;
                }

                //Matrix4x4 NewWorldMatrix = ParentNode.GetWorldMatrix() * LocalMatrix;
                // IKの親子関係はLinkBoneで繋がっていると仮定して1つ前のノードを親とする
                Matrix4x4 NewWorldMatrix = PmxTransformList[n - 1].GetWorldMatrix() * LocalMatrix;
                PmxTransformList[n].SetWorldMatrix(NewWorldMatrix);
            }

            // CCD-IKを採用
            int NumOfLink = m_IKChainList.Count;
            if (NumOfLink < 2) return true;

            int EndIndex = NumOfLink - 1;

            float Threshold = 0.01f;

            // 始点(先頭リンク)の親ワールド行列を無視する(つまり始点を原点としてIK計算を行う)
            // これを考慮すると例えば体を捻った時にIKが暴れてしまう
            Matrix4x4 TargetMat = Matrix4x4.Inverse(m_IKChainList[0].GetParentNode().transform.localToWorldMatrix) * m_IKTarget.transform.localToWorldMatrix;
            Vector3 TargetPos = TargetMat.GetPosition();

            // ターゲットに届くかサイクルの最大値に達するまで計算を繰り返す
            int CurrentLoopNum = 0;
            bool DoLoop = true;

            int MaxLoopNum = m_IKParam.IKLoopCount;

            while (DoLoop && CurrentLoopNum < MaxLoopNum)
            {
                Vector3 EndPos = PmxTransformList[EndIndex].GetWorldPos();
                
                // 既に接触しているなら終了
                if (Vector3.Distance(TargetPos, EndPos) < Threshold)
                {
                    DoLoop = false;
                    break;
                }

                for (int i = NumOfLink - 2; i >= 0; i--)
                {
                    CPmxTransform LinkTrans = PmxTransformList[i];

                    Vector3 LinkPos = LinkTrans.GetWorldPos();
                    
                    Vector3 e_i = Vector3.Normalize(EndPos - LinkPos);
                    Vector3 t_i = Vector3.Normalize(TargetPos - LinkPos);
                    
                    // 内積
                    // なぜか1を微妙に越してNaNになってしまうことがあるのでちゃんとクランプしておく
                    float dot = Mathf.Clamp(Vector3.Dot(e_i, t_i), -1.0f, 1.0f);
                    
                    // 外積
                    Vector3 axis = Vector3.Cross(e_i, t_i);

                    Quaternion rot;

                    // EffectVecとTargetVecがほぼ平行なので回転軸が存在しない(ほぼ0)になっていることがあるのでそれを考慮する
                    // ほぼ平行の時は任意な垂直時軸に対して0度か180度回転させる
                    if (axis.magnitude < 1e-6f)
                    {
                        if (Mathf.Sign(dot) == 1.0f)
                        {
                            // Linkが２つしかないなら1回だけ演算したら終了とする
                            // 回転不要なのでここで終了
                            if (NumOfLink <= 2)
                            {
                                // 終了
                                DoLoop = false;
                                break;
                            }

                            // 同じ方向に平行な時は回転の必要がない
                            continue;
                        }
                        else
                        {
                            // 反対方向に平行なので任意の垂直軸で180度回転する
                            Vector3 XAxis = new Vector3(1.0f, 0.0f, 0.0f);
                            Vector3 YAxis = new Vector3(0.0f, 1.0f, 0.0f);
                            Vector3 ZAxis = new Vector3(0.0f, 0.0f, 1.0f);

                            Vector3 SubAxis = Vector3.Cross(XAxis, e_i);

                            if (SubAxis.magnitude < 1e-6f)
                            {
                                // X軸とも平行なのでY軸の方を使う
                                SubAxis = Vector3.Cross(YAxis, e_i);

                                if (SubAxis.magnitude < 1e-6f)
                                {
                                    // Y軸とも平行なのでZ軸の方を使う
                                    SubAxis = Vector3.Cross(ZAxis, e_i);
                                }
                            }

                            // 回転角度がおかしくなってしまうので回転取得前にちゃんと軸を正規化しておく
                            rot = Quaternion.AngleAxis(Mathf.Rad2Deg * 3.1415f, Vector3.Normalize(SubAxis)); 
                        }
                    }
                    else
                    {
                        // 通常通り内積結果から回転
                        float angle = Mathf.Rad2Deg * Mathf.Acos(dot);

                        // LimitedAngleは-180 ～ 180 で表現されるので 0 ～ 360のeulerもそのように直す
                        angle = ConvertDegreeAngle360To180(angle);

                        // 単位角で回転量を制限。LimitedAngleはラジアン
                        angle = Mathf.Min(angle, Mathf.Rad2Deg * m_IKParam.LimitedAngle);

                        // 回転角度がおかしくなってしまうので回転取得前にちゃんと軸を正規化しておく
                        rot = Quaternion.AngleAxis(angle, Vector3.Normalize(axis)); 
                    }
                    
                    if (math.isnan(rot.x) || math.isnan(rot.y) || math.isnan(rot.z) || math.isnan(rot.w))
                    {
                        Debug.LogError("[Error] CCDIK - found NaN value in ik rot. when clamp rotation.\n");
                        return false;
                    }

                    // 角度制限前にいったん反映する
                    LinkTrans.SetLocalRot(rot * LinkTrans.GetLocalRot());

                    Quaternion ResultRot = LinkTrans.GetLocalRot();

                    // 演算終了後の回転に対して角度を制限行う
                    // 制限を行うことで例えば膝が変な方向に曲がらないようにする
                    int LinkIndex = (m_IKParam.IKLinkList.Count) - 1 - i;
                    SIKLink IKLink = m_IKParam.IKLinkList[LinkIndex];

                    if (IKLink.IsLimitAngle)
                    {
                        Vector3 LowerAngle = Mathf.Rad2Deg * IKLink.LowerAngle;
                        Vector3 UpperAngle = Mathf.Rad2Deg * IKLink.UpperAngle;

                        Vector3 euler = ResultRot.eulerAngles;

                        // LowerAngleとUpperAngleは-180 ～ 180 で表現されるので 0 ～ 360のeulerもそのように直す
                        euler.x = ConvertDegreeAngle360To180(euler.x);
                        euler.y = ConvertDegreeAngle360To180(euler.y);
                        euler.z = ConvertDegreeAngle360To180(euler.z);

                        // オイラー角に対して角度制限を行う
                        // LowerAngleとUpperAngleはラジアン
                        euler.x = Mathf.Clamp(euler.x, LowerAngle.x, UpperAngle.x);
                        euler.y = Mathf.Clamp(euler.y, LowerAngle.y, UpperAngle.y);
                        euler.z = Mathf.Clamp(euler.z, LowerAngle.z, UpperAngle.z);
                        
                        ResultRot = Quaternion.Euler(euler);

                        LinkTrans.SetLocalRot(ResultRot);

                        if (math.isnan(ResultRot.x) || math.isnan(ResultRot.y) || math.isnan(ResultRot.z) || math.isnan(ResultRot.w))
                        {
                            Debug.LogError("[Error] CCDIK - found NaN value in ik ResultRot. when clamp rotation.\n");
                            return false;
                        }
                    }

                    // もう一度反映
                    PmxTransformList[i] = LinkTrans;

                    // Linkノードのワールド行列を再計算する
                    for (int n = i; n < NumOfLink; n++)
                    {
                        PmxBoneComponent ReCalcNode = m_IKChainList[n];

                        PmxBoneComponent ParentNode = ReCalcNode.GetParentNode();

                        Matrix4x4 LocalMatrix = PmxTransformList[n].GetLocalMatrix();

                        // 始点(先頭リンク)の親ワールド行列を無視する
                        // これを考慮すると例えば体を捻った時にIKが暴れてしまう
                        if (ParentNode == null || n == 0)
                        {
                            // 親ノードがない時はローカル行列をワールド行列として渡す
                            PmxTransformList[n].SetWorldMatrix(LocalMatrix);

                            continue;
                        }

                        //Matrix4x4 NewWorldMatrix = ParentNode.GetWorldMatrix() * ReCalcNode.GetLocalMatrix();
                        // IKの親子関係はLinkBoneで繋がっていると仮定して1つ前のノードを親とする
                        Matrix4x4 NewWorldMatrix = PmxTransformList[n - 1].GetWorldMatrix() * LocalMatrix;
                        PmxTransformList[n].SetWorldMatrix(NewWorldMatrix);
                    }

                    // EndNodeの座標を更新
                    EndPos = PmxTransformList[EndIndex].GetWorldPos();

                    // 接触しているなら終了
                    if (Vector3.Distance(TargetPos, EndPos) < 0.01f)
                    {
                        // 終了
                        DoLoop = false;
                        break;
                    }

                    // Linkが２つしかないなら1回だけ演算したら終了とする
                    if (NumOfLink <= 2)
                    {
                        // 終了
                        DoLoop = false;
                        break;
                    }
                }

                // ループ回数を更新
                CurrentLoopNum++;
            }

            // 始点(先頭Link)の親ワールド行列を考慮したうえで再計算する
            for (int n = 0; n < m_IKChainList.Count; n++)
            {
                PmxBoneComponent LinkNode = m_IKChainList[n];

                Matrix4x4 LocalMatrix = PmxTransformList[n].GetLocalMatrix();

                // Linkノードのワールド行列を再計算する
                PmxBoneComponent ParentNode = LinkNode.GetParentNode();
                if (ParentNode == null)
                {
                    // 親ノードがない時はローカル行列をワールド行列として渡す
                    LinkNode.transform.position = LocalMatrix.GetPosition();
                    LinkNode.transform.rotation = LocalMatrix.rotation;
                    //LinkNode.SetWorldMatrix(LinkNode.GetLocalMatrix());

                    continue;
                }

                Matrix4x4 NewWorldMatrix = ParentNode.transform.localToWorldMatrix * LocalMatrix;
                LinkNode.transform.position = NewWorldMatrix.GetPosition();
                LinkNode.transform.rotation = NewWorldMatrix.rotation;
                //LinkNode.SetWorldMatrix(NewWorldMatrix);
            }

            return true;
        }

        // 0度～360度の範囲の角度を-180度から180度の範囲の角度に直す
        static float ConvertDegreeAngle360To180(float SrcAngle)
        {
            float DstAngle = SrcAngle;

            // まず0 から 360の範囲にする
            DstAngle = math.fmod(DstAngle, 360.0f);

            // 180度を越していたら0から180の間に直して符号を反転する
            if(DstAngle > 180.0f)
            {
                DstAngle = 360.0f - DstAngle;
                DstAngle *= -1.0f;
            }

            return DstAngle;
        }
    }
}