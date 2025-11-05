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

        public void SaveAsDefaultLocalTransform()
		{
			this.m_DefaultLocalPos = this.gameObject.transform.localPosition;
			this.m_DefaultLocalRot = this.gameObject.transform.localRotation;
            //this.m_DefaultLocalScale = this.gameObject.transform.localScale;
        }

        public void SetWorldMatrix(Matrix4x4 worldMatrix)
        {
            this.gameObject.transform.position = worldMatrix.GetPosition();
            this.gameObject.transform.rotation = worldMatrix.rotation;
            //this.gameObject.transform.lossyScale = worldMatrix.lossyScale;
        }

        public Vector3 GetWorldPos()
        {
            return this.gameObject.transform.position;
        }

        public Matrix4x4 GetWorldMatrix()
        {
            return this.gameObject.transform.localToWorldMatrix;
        }

        public void SetLocalRot(Quaternion rot)
        {
            this.gameObject.transform.localRotation = rot;
        }

        public Quaternion GetLocalRot()
        {
            return this.gameObject.transform.localRotation;
        }

        public Matrix4x4 GetLocalMatrix()
        {
            Matrix4x4 modelMatrix =
                Matrix4x4.Translate(this.gameObject.transform.localPosition) *
                Matrix4x4.Rotate(this.gameObject.transform.localRotation) *
                Matrix4x4.Scale(this.gameObject.transform.localScale);

            return modelMatrix;
        }


        public void ResetToDefaultLocalTransform()
		{
            this.gameObject.transform.localPosition = this.m_DefaultLocalPos;
            this.gameObject.transform.localRotation = this.m_DefaultLocalRot;
            //this.gameObject.transform.localScale = this.m_DefaultLocalScale;
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