using NUnit.Framework;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace mmdlib
{
    public class CIKSolver
    {
        SIKParam m_IKParam = null;
        PmxBone m_IKTarget = null;
        List<PmxBone> m_IKChainList = new List<PmxBone>();

		public CIKSolver()
		{
		}

        public bool Create(PmxBone IKTargetBone, List<PmxBone> BoneList)
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
            // まずLinkNodeを初期姿勢に戻す
            // T-Pose(元の姿勢)にリセットして演算を行うことで演算結果が安定するようになる
            // このようにしないと途中で変な方向を向いたりぶるぶるしたりして不安定になる
            for (int n = 0; n < m_IKChainList.Count; n++)
            {
                PmxBone LinkNode = m_IKChainList[n];

                LinkNode.ResetToDefaultLocalTransform();

                // Linkノードのワールド行列を再計算する
                PmxBone ParentNode = LinkNode.GetParentNode();
                // 始点(先頭リンク)の親ワールド行列を無視する
                // これを考慮すると例えば体を捻った時にIKが暴れてしまう
                if (!ParentNode || n == 0)
                {
                    // 親ノードがない時はローカル行列をワールド行列として渡す
                    LinkNode.SetWorldMatrix(LinkNode.GetLocalMatrix());

                    continue;
                }

                Matrix4x4 NewWorldMatrix = ParentNode.GetWorldMatrix() * LinkNode.GetLocalMatrix();
                LinkNode.SetWorldMatrix(NewWorldMatrix);
            }

            // CCD-IKを採用
            int NumOfLink = m_IKChainList.Count;
            if (NumOfLink < 2) return true;

            int EndIndex = NumOfLink - 1;

            float Threshold = 0.01f;
            
            // 始点(先頭リンク)の親ワールド行列を無視する(つまり始点を原点としてIK計算を行う)
            // これを考慮すると例えば体を捻った時にIKが暴れてしまう
            Matrix4x4 TargetMat = Matrix4x4.Inverse(m_IKChainList[0].GetParentNode().GetWorldMatrix()) * m_IKTarget.GetWorldMatrix();
            Vector3 TargetPos = new Vector3(TargetMat.m30, TargetMat.m31, TargetMat.m32);

            // ターゲットに届くかサイクルの最大値に達するまで計算を繰り返す
            int CurrentLoopNum = 0;
            bool DoLoop = true;

            int MaxLoopNum = m_IKParam.IKLoopCount;

            PmxBone EndNode = m_IKChainList[EndIndex];

            while (DoLoop && CurrentLoopNum < MaxLoopNum)
            {
                Vector3 EndPos = EndNode.GetWorldPos();
                
                // 既に接触しているなら終了
                if (Vector3.Distance(TargetPos, EndPos) < Threshold)
                {
                    DoLoop = false;
                    break;
                }

                for (int i = NumOfLink - 2; i >= 0; i--)
                {
                    PmxBone LinkNode = m_IKChainList[i];

                    Vector3 LinkPos = LinkNode.GetWorldPos();
                    
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
                            rot = Quaternion.AngleAxis(3.1415f, Vector3.Normalize(SubAxis)); 
                        }
                    }
                    else
                    {
                        // 通常通り内積結果から回転
                        float angle = Mathf.Acos(dot);

                        // 単位角で回転量を制限。LimitedAngleはラジアン
                        angle = Mathf.Min(angle, m_IKParam.LimitedAngle);

                        // 回転角度がおかしくなってしまうので回転取得前にちゃんと軸を正規化しておく
                        rot = Quaternion.AngleAxis(angle, Vector3.Normalize(axis)); 
                    }
                    
                    if (math.isnan(rot.x) || math.isnan(rot.y) || math.isnan(rot.z) || math.isnan(rot.w))
                    {
                        Debug.LogError("[Error] CCDIK - found NaN value in ik rot. when clamp rotation.\n");
                        return false;
                    }

                    // 角度制限前にいったん反映する
                    LinkNode.SetLocalRot(rot * LinkNode.GetLocalRot());

                    Quaternion ResultRot = LinkNode.GetLocalRot();

                    // 演算終了後の回転に対して角度を制限行う
                    // 制限を行うことで例えば膝が変な方向に曲がらないようにする
                    int LinkIndex = (m_IKParam.IKLinkList.Count) - 1 - i;
                    SIKLink IKLink = m_IKParam.IKLinkList[LinkIndex];

                    if (IKLink.IsLimitAngle)
                    {
                        Vector3 LowerAngle = IKLink.LowerAngle;
                        Vector3 UpperAngle = IKLink.UpperAngle;

                        Vector3 euler = ResultRot.eulerAngles;

                        // オイラー角に対して角度制限を行う
                        // LowerAngleとUpperAngleはラジアン
                        euler.x = Mathf.Clamp(euler.x, LowerAngle.x, UpperAngle.x);
                        euler.y = Mathf.Clamp(euler.y, LowerAngle.y, UpperAngle.y);
                        euler.z = Mathf.Clamp(euler.z, LowerAngle.z, UpperAngle.z);

                        ResultRot = Quaternion.Euler(euler);

                        LinkNode.SetLocalRot(ResultRot);

                        if (math.isnan(ResultRot.x) || math.isnan(ResultRot.y) || math.isnan(ResultRot.z) || math.isnan(ResultRot.w))
                        {
                            Debug.LogError("[Error] CCDIK - found NaN value in ik ResultRot. when clamp rotation.\n");
                            return false;
                        }
                    }

                    // Linkノードのワールド行列を再計算する
                    for (int n = i; n < NumOfLink; n++)
                    {
                        PmxBone ReCalcNode = m_IKChainList[n];

                        PmxBone ParentNode = ReCalcNode.GetParentNode();
                        // 始点(先頭リンク)の親ワールド行列を無視する
                        // これを考慮すると例えば体を捻った時にIKが暴れてしまう
                        if (ParentNode == null || n == 0)
                        {
                            // 親ノードがない時はローカル行列をワールド行列として渡す
                            ReCalcNode.SetWorldMatrix(ReCalcNode.GetLocalMatrix());

                            continue;
                        }

                        Matrix4x4 NewWorldMatrix = ParentNode.GetWorldMatrix() * ReCalcNode.GetLocalMatrix();
                        ReCalcNode.SetWorldMatrix(NewWorldMatrix);
                    }

                    // EndNodeの座標を更新
                    EndPos = EndNode.GetWorldPos();

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
            foreach(PmxBone LinkNode in m_IKChainList)
            {
                // Linkノードのワールド行列を再計算する
                PmxBone ParentNode = LinkNode.GetParentNode();
                if (!ParentNode)
                {
                    // 親ノードがない時はローカル行列をワールド行列として渡す
                    LinkNode.SetWorldMatrix(LinkNode.GetLocalMatrix());

                    continue;
                }

                Matrix4x4 NewWorldMatrix = ParentNode.GetWorldMatrix() * LinkNode.GetLocalMatrix();
                LinkNode.SetWorldMatrix(NewWorldMatrix);
            }

            return true;
        }
    }
}