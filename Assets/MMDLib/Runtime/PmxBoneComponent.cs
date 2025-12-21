using System.Collections.Generic;
using UnityEngine;

namespace mmdlib
{
    public class PmxBoneComponent : MonoBehaviour
    {
        public Vector3 m_DefaultLocalPos = Vector3.zero;
        public Quaternion m_DefaultLocalRot = Quaternion.identity;
        public Vector3 m_DefaultLocalScale = Vector3.one;

        public EHumanoidBones m_BoneName = EHumanoidBones.None;

        public EHumanoidBones m_ParentBoneName = EHumanoidBones.None;

        // 物理オブジェクト
        public List<PmxRigidBodyComponent> m_PhysicsObjectList = new List<PmxRigidBodyComponent> ();

        // 回転付与・移動付与
        // 付与とは他のボーンに付いて行くということ
        // 付与親ボーンのボーンIndex
        public int m_GrantParentBoneIndex = -1;

        // 付与率
        public float m_GrantRate = 0.0f;

        // 回転付与
        public bool m_RotateGrant = false;

        // 移動付与
        public bool m_MoveGrant = false;

        // IK
        public SIKParam m_IKParam = null;

        //
        CPmxTransform m_LocalTransform = new CPmxTransform();
        Matrix4x4 m_WorldMatrix = Matrix4x4.identity;

        public void SaveAsDefaultLocalTransform()
		{
			this.m_DefaultLocalPos = this.gameObject.transform.localPosition;
			this.m_DefaultLocalRot = this.gameObject.transform.localRotation;
            this.m_DefaultLocalScale = this.gameObject.transform.localScale;
        }

        public PmxBoneComponent GetParentNode()
        {
            var parent = this.gameObject.transform.parent;
            if (parent == null) return null;

            return parent.GetComponent<PmxBoneComponent>();
        }

        public EHumanoidBones GetBoneName()
		{
			return m_BoneName;
		}

        // 物理オブジェクト
        public void AddPhysicsObject(PmxRigidBodyComponent pmxRigidBodyComponent)
	    {
		    m_PhysicsObjectList.Add(pmxRigidBodyComponent);
	    }

        public void SetBoneName(EHumanoidBones BoneName)
		{
			m_BoneName = BoneName;
		}

        public EHumanoidBones GetParentBoneName()
		{
			return m_ParentBoneName;
		}

        public void SetParentBoneName(EHumanoidBones BoneName)
		{
			m_ParentBoneName = BoneName;
		}

        // 付与親ボーンのボーンIndex
        public int GetGrantParentBoneIndex()
		{
			return m_GrantParentBoneIndex;
		}

        // 付与率
        public float GetGrantRate()
		{
			return m_GrantRate;
		}

        // 回転付与
        public void SetRotateGrant(int GrantParentBoneIndex, float GrantRate)
		{
			m_RotateGrant = true;

			m_GrantParentBoneIndex = GrantParentBoneIndex;
			m_GrantRate = GrantRate;
		}

        public bool IsRotateGrant()
		{
			return m_RotateGrant;
		}

        // 移動付与
        public void SetMoveGrant(int GrantParentBoneIndex, float GrantRate)
		{
			m_MoveGrant = true;

			m_GrantParentBoneIndex = GrantParentBoneIndex;
			m_GrantRate = GrantRate;
		}

        public bool IsMoveGrant()
		{
			return m_MoveGrant;
		}

        // IK
        public bool IsIKEnabled()
        {
            if (m_IKParam == null) return false;

            return (m_IKParam.IKLinkList.Count > 0);
        }

        public SIKParam GetIKParam()
		{
			return m_IKParam;
		}

        public void SetIKParam(SIKParam Param)
		{
			m_IKParam = Param;
		}

        // 物理演算実行前に物理オブジェクトの位置をボーンと合わせる
        public void ApplyBoneToPhysics()
        {
            foreach(var PhysicsObj in m_PhysicsObjectList)
            {
                // Staticもしくはボーン付与の物理オブジェクトのみ反映する
                if (PhysicsObj.PhysicsType != EPmxPhysicsType.STATIC && PhysicsObj.PhysicsType != EPmxPhysicsType.DYNAMIC_BONE_ALIGNMENT) continue;

                PhysicsObj.transform.position = this.transform.position;

                // コライダーのオフセットをPhysicsObjのローカル回転を基準に作っているのでその姿勢から如何に回転させるかを考える
                PhysicsObj.transform.rotation = this.transform.rotation * PhysicsObj.DefaultTransform.m_LocalRotate;
            }
        }

        // 物理エンジンの計算結果をボーンに反映
        public void ApplyPhysicsToBone()
        {
            foreach (var PhysicsObj in m_PhysicsObjectList)
            {
                // ダイナミックもしくはボーン付与の物理オブジェクトのみ反映する
                if (PhysicsObj.PhysicsType != EPmxPhysicsType.DYNAMIC && PhysicsObj.PhysicsType != EPmxPhysicsType.DYNAMIC_BONE_ALIGNMENT) continue;
                
                this.transform.position = PhysicsObj.transform.position;

                // 元のボーンにはPhysicsObjの回転は不要なので除去した回転を反映
                this.transform.rotation = PhysicsObj.transform.rotation * Quaternion.Inverse(PhysicsObj.DefaultTransform.m_LocalRotate);
            }
        }
    }
}