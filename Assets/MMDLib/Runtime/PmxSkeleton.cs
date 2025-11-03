using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace mmdlib
{
    public class PmxSkeleton : MonoBehaviour
    {
        List<PmxBone> m_PmxBoneList = new List<PmxBone>();
        List<PmxBone> m_GrantBoneList = new List<PmxBone>();
        List<PmxBone> m_IKBoneList = new List<PmxBone>();
        List<CIKSolver> m_IKSolverList = new List<CIKSolver>();

        // 全ボーン一覧
        public void SetPmxBoneList(List<PmxBone> BoneList)
        {
            m_PmxBoneList = BoneList;
        }

        // IKボーン
        public void MakeIKBoneList()
        {
            foreach(var Bone in m_PmxBoneList)
		{
                // IKは重いのでひとまず標準ボーン以外は除外する
                if (Bone.GetBoneName() == EHumanoidBones.None) continue;

                // IKParamを持っていればリストに追加する
                if (Bone.GetIKParam() != null)
                {
                    CIKSolver IKSolver = new CIKSolver();
                    //if (!IKSolver.Create(std::get < 1 > (Bone), m_BoneList)) continue;

                    m_IKSolverList.Add(IKSolver);
                    m_IKBoneList.Add(Bone);
                }
            }
        }

        // 付与ボーン
        public void MakeGrantBoneList()
        {
            foreach(var Bone in m_PmxBoneList)
		    {
                if (Bone.IsRotateGrant() || Bone.IsMoveGrant())
                {
                    m_GrantBoneList.Add(Bone);
                }
            }
        }

        void Start()
        {

        }

        void LateUpdate()
        {
            // IKの計算を行う
            CalculateIK();

            // 付与ボーンの位置を計算
            CalculateGrantBone();
        }

        // IK計算
        void CalculateIK()
        {
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
    }
}