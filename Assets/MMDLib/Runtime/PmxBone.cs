using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI.Table;

namespace mmdlib
{
    public class PmxBone : MonoBehaviour
    {
        public Vector3 m_DefaultLocalPos = Vector3.zero;
        public Quaternion m_DefaultLocalRot = Quaternion.identity;
        public Vector3 m_DefaultLocalScale = Vector3.one;

        public EHumanoidBones m_BoneName = EHumanoidBones.None;

        public EHumanoidBones m_ParentBoneName = EHumanoidBones.None;

        // 物理オブジェクト
        public List<(EPmxPhysicsType PhysicsType, GameObject PhysicsObj)> m_PhysicsObjectList = new List<(EPmxPhysicsType PhysicsType, GameObject PhysicsObj)> ();

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

        public PmxBone GetParentNode()
        {
            var parent = this.gameObject.transform.parent;
            if (parent == null) return null;

            return parent.GetComponent<PmxBone>();
        }

        public EHumanoidBones GetBoneName()
		{
			return m_BoneName;
		}

        // 物理オブジェクト
        public void AddPhysicsObject(EPmxPhysicsType PhysicsType, GameObject PhysicsObj)
	    {
		    if (PhysicsObj == null) return;

		    m_PhysicsObjectList.Add((PhysicsType, PhysicsObj));
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
    }
}