using Unity.VisualScripting;
using UnityEngine;

namespace mmdlib
{
    public class PmxBone : MonoBehaviour
    {
		Vector3 m_DefaultPos = Vector3.zero;
        Quaternion m_DefaultRot = Quaternion.identity;
		Vector3 m_DefaultScale = Vector3.one;

        EHumanoidBones m_BoneName = EHumanoidBones.None;

        EHumanoidBones m_ParentBoneName = EHumanoidBones.None;

        // 回転付与・移動付与
        // 付与とは他のボーンに付いて行くということ
        // 付与親ボーンのボーンIndex
        int m_GrantParentBoneIndex = -1;

        // 付与率
        float m_GrantRate = 0.0f;

        // 回転付与
        bool m_RotateGrant = false;

        // 移動付与
        bool m_MoveGrant = false;

        // IK
        SIKParam m_IKParam = null;

		public void SaveDefaultTransform()
		{
			m_DefaultPos = this.gameObject.transform.localPosition;
			m_DefaultRot = this.gameObject.transform.localRotation;
			m_DefaultScale = this.gameObject.transform.localScale;
        }

		public void ResetToDefaultTransform()
		{
			this.gameObject.transform.localPosition = m_DefaultPos;
			this.gameObject.transform.localRotation = m_DefaultRot;
			this.gameObject.transform.localScale	= m_DefaultScale;
		}


        public EHumanoidBones GetBoneName()
		{
			return m_BoneName;
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