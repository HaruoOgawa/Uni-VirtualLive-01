using mmdlib;
using UnityEngine;

public class CPmxBone
{
    string m_BoneName;
    string m_BoneName_EN;

    Vector3 m_Pos;

    int m_ParentBoneIndex;

    int m_DeformLayer;

    // 接続(影響を受ける)ボーン
    int m_ConnectBoneIndex;

    // 回転付与・移動付与
    // 付与とは他のボーンに付いて行くということ
    // 付与親ボーンのボーンIndex
    int m_GrantParentBoneIndex;

    // 付与率
    float m_GrantRate;

    // 回転付与
    bool m_RotateGrant;

    // 移動付与
    bool m_MoveGrant;

    // ローカル軸
    bool m_UseLoacalAxis;
    Quaternion m_LocalAxis;

    // IK
    SIKParam m_IKParam;

    public CPmxBone(string boneName, string boneNameEN, Vector3 pos, int parentBoneIndex, int deformLayer, ushort boneFlag)
    {
        m_BoneName = boneName;
        m_BoneName_EN = boneNameEN;
        m_Pos = pos;
        m_ParentBoneIndex = parentBoneIndex;
        m_DeformLayer = deformLayer;
        m_ConnectBoneIndex = -1;
        m_GrantParentBoneIndex = -1;
        m_GrantRate = 0.0f;
        m_RotateGrant = false;
        m_MoveGrant = false;
        m_UseLoacalAxis = false;
        m_LocalAxis = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f);
        m_IKParam = null;
        // 必要に応じて随時実装
        ReadBoneFlag(boneFlag);
    }

    public void ReadBoneFlag(int boneFlag)
    {
    }

    public string GetBoneName()
    {
        return m_BoneName;
    }

    public string GetBoneName_EN()
    {
        return m_BoneName_EN;
    }

    public Vector3 GetPos()
    {
        return m_Pos;
    }

    public int GetParentBoneIndex()
    {
        return m_ParentBoneIndex;
    }

    public int GetDeformLayer()
    {
        return m_DeformLayer;
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
    public void SetRotateGrant(int grantParentBoneIndex, float grantRate)
    {
        m_RotateGrant = true;
        m_GrantParentBoneIndex = grantParentBoneIndex;
        m_GrantRate = grantRate;
    }

    public bool IsRotateGrant()
    {
        return m_RotateGrant;
    }

    // 移動付与
    public void SetMoveGrant(int grantParentBoneIndex, float grantRate)
    {
        m_MoveGrant = true;
        m_GrantParentBoneIndex = grantParentBoneIndex;
        m_GrantRate = grantRate;
    }

    public bool IsMoveGrant()
    {
        return m_MoveGrant;
    }

    // ローカル軸
    public bool IsUseLoacalAxis()
    {
        return m_UseLoacalAxis;
    }

	public void SetLocalAxis(Vector3 XAxisVector, Vector3 ZAxisVector)
	{
		// 用途不明. 計算も見直しが必要
		m_UseLoacalAxis = true;

        Vector3 YAxisVector = Vector3.Cross(XAxisVector, ZAxisVector);

        Matrix4x4 rotMat = Matrix4x4.identity;
        
        rotMat.m00 = XAxisVector.x;
		rotMat.m01 = XAxisVector.y;
		rotMat.m02 = XAxisVector.z;

		rotMat.m20 = YAxisVector.x;
		rotMat.m21 = YAxisVector.y;
		rotMat.m22 = YAxisVector.z;

		rotMat.m10 = ZAxisVector.x;
		rotMat.m11 = ZAxisVector.y;
		rotMat.m12 = ZAxisVector.z;

        /*rotMat[0][0] = XAxisVector.x;
		rotMat[1][0] = XAxisVector.y;
		rotMat[2][0] = XAxisVector.z;

		rotMat[0][1] = YAxisVector.x;
		rotMat[1][1] = YAxisVector.y;
		rotMat[2][1] = YAxisVector.z;

		rotMat[0][2] = ZAxisVector.x;
		rotMat[1][2] = ZAxisVector.y;
		rotMat[2][2] = ZAxisVector.z;*/

        m_LocalAxis = rotMat.rotation;

        /*Vector3 LocalAxisVector = glm::normalize(XAxisVector + YAxisVector + ZAxisVector);
		Vector3 DefaultAxisVector = glm::normalize(Vector3(1.0f, 0.0f, 0.0f) + Vector3(0.0f, 1.0f, 0.0f) + Vector3(0.0f, 0.0f, 1.0f));

		m_LocalAxis = glm::normalize(math::CTransform::CalcTwoVectorRotate(DefaultAxisVector, LocalAxisVector));*/
        //m_LocalAxis = math::CTransform::CalcTwoVectorRotate(Vector3(1.0f, 0.0f, 0.0f), XAxisVector) * math::CTransform::CalcTwoVectorRotate(Vector3(0.0f, 0.0f, 1.0f), ZAxisVector);
    }

    public Quaternion GetLocalAxis()
	{
		return m_LocalAxis;
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

	public Matrix4x4 GetWorldMatrix()
	{
        Matrix4x4 WorldMatrix =
            Matrix4x4.Scale(new Vector3(1.0f, 1.0f, 1.0f)) *
            Matrix4x4.Rotate(m_LocalAxis) *
            Matrix4x4.Translate(m_Pos);

		return WorldMatrix;
	}
}
