using System.Collections.Generic;
using UnityEngine;

namespace mmdlib
{
    public class PmxSkeletonComponent : MonoBehaviour
    {
        public List<PmxBoneComponent> m_PmxBoneList = new List<PmxBoneComponent>();
        public List<PmxBoneComponent> m_GrantBoneList = new List<PmxBoneComponent>();
        public List<PmxBoneComponent> m_IKBoneList = new List<PmxBoneComponent>();
        public List<CIKSolver> m_IKSolverList = new List<CIKSolver>();
        public List<PmxLoneryBone> m_PmxLoneryBoneList = new List<PmxLoneryBone>();

        void Start()
        {
        }

        // 全ボーン一覧
        public void SetBoneList(List<PmxBoneComponent> BoneList)
        {
            m_PmxBoneList = BoneList;
        }

        public List<PmxBoneComponent> GetPmxBoneList()
        {
            return m_PmxBoneList;
        }

        // IKボーン
        public void MakeIKBoneList()
        {
            foreach (var Bone in m_PmxBoneList)
            {
                // IKは重いのでひとまず標準ボーン以外は除外する
                if (Bone.GetBoneName() == EHumanoidBones.None) continue;

                // IKParamを持っていればリストに追加する
                if (Bone.IsIKEnabled())
                {
                    CIKSolver IKSolver = new CIKSolver();
                    if (!IKSolver.Create(Bone, m_PmxBoneList)) continue;

                    m_IKSolverList.Add(IKSolver);
                    m_IKBoneList.Add(Bone);
                }
            }
        }

        // 付与ボーン
        public void MakeGrantBoneList()
        {
            foreach (var Bone in m_PmxBoneList)
            {
                if (Bone.IsRotateGrant() || Bone.IsMoveGrant())
                {
                    m_GrantBoneList.Add(Bone);
                }
            }
        }

        // イベントの実行順
        // https://docs.unity3d.com/ja/2018.4/Manual/ExecutionOrder.html
        // FixedUpdate → 物理演算 → アニメーション更新 → Update → LateUpdate 

        void FixedUpdate()
        {
            // 物理演算実行前に物理オブジェクトの位置をボーンと合わせる
            foreach(var Bone in m_PmxBoneList)
            {
                Bone.ApplyBoneToPhysics();
            }
        }

        void Update()
        {
            // IKの計算を行う
            CalculateIK();

            // 付与ボーンの位置を計算
            CalculateGrantBone();

            // ロンリーボーンの計算
            CalculateLoneryBone();
        }

        void LateUpdate()
        {
            // 物理エンジンの計算結果をボーンに反映
            foreach (var Bone in m_PmxBoneList)
            {
                Bone.ApplyPhysicsToBone();
            }
        }

        // IK計算
        bool CalculateIK()
        {
            foreach (var IKSolver in m_IKSolverList)
		    {
                if (!IKSolver.Solve()) return false;
            }

            return true;
        }

        // 付与ボーンの計算
        void CalculateGrantBone()
        {
            // 付与はローカルトランスフォームに対して実行する
            foreach(var GrantBone in m_GrantBoneList)
			{
                // ParentGrantBoneを取得
                int GrantParentBoneIndex = GrantBone.GetGrantParentBoneIndex();
                if (GrantParentBoneIndex < 0 || GrantParentBoneIndex >= m_PmxBoneList.Count) continue;

                var ParentGrantBone = m_PmxBoneList[GrantParentBoneIndex];
                if (ParentGrantBone == null) continue;

                // 付与率
                float GrantRate = GrantBone.GetGrantRate();

                // ひとまず負の時はスキップする
                if (GrantRate < 0.0f) continue;

                if (GrantBone.IsRotateGrant())
                {
                    // 回転付与
                    Quaternion LocalParentRot = ParentGrantBone.transform.localRotation;

                    if (GrantRate >= 0.0f)
                    {
                        //glm::quat GrantRot = (GrantRate * LocalParentRot) * GrantBone.GetBoneNode().GetRot();
                        Quaternion GrantRot = Quaternion.Slerp(GrantBone.transform.localRotation, LocalParentRot, GrantRate);

                        GrantBone.transform.localRotation = GrantRot;
                    }
                    else
                    {
                        // 付与率が負の時は逆行列をかける
                        Quaternion invQuat = Quaternion.Inverse(LocalParentRot);

                        invQuat.x *= Mathf.Abs(GrantRate);
                        invQuat.y *= Mathf.Abs(GrantRate);
                        invQuat.z *= Mathf.Abs(GrantRate);
                        invQuat.w *= Mathf.Abs(GrantRate);

                        Quaternion GrantRot = invQuat * GrantBone.transform.localRotation;

                        GrantBone.transform.localRotation = GrantRot;
                    }
                }
                else if (GrantBone.IsMoveGrant())
                {
                    Vector3 LocalParentPos = ParentGrantBone.transform.localPosition;

                    // 移動付与
                    if (GrantRate >= 0.0f)
                    {
                        //glm::vec3 GrantPos = GrantRate * LocalParentPos + GrantBone.GetBoneNode().GetPos();
                        Vector3 GrantPos = (1.0f - GrantRate) * GrantBone.transform.localPosition + GrantRate * LocalParentPos;

                        GrantBone.transform.localPosition = GrantPos;
                    }
                    else
                    {
                        // 付与率が負の時は逆行列をかける
                        Vector3 GrantPos = (-1.0f) * GrantRate * LocalParentPos + GrantBone.transform.localPosition;

                        GrantBone.transform.localPosition = GrantPos;
                    }
                }
            }
        }

        // ロンリーボーンの計算
        // 標準ボーン・付与ボーン・IKボーン・物理ボーンのどれでもないボーン
        // 服だったりの一部メッシュがこれを参照していて動かないことがあるのでその対策
        // アニメーションシステム的には動かなくて正しいが、なぜかMMDだと動いているので
        // 一番近い標準ボーンに常に回転が追従していると予想
        void CalculateLoneryBone()
        {
            foreach(var LoneryBone in m_PmxLoneryBoneList)
            {
                if(LoneryBone ==null) continue;

                if(LoneryBone.FollowBone == null) continue;

                LoneryBone.transform.localRotation = LoneryBone.FollowBone.localRotation;
            }
        }
    }
}