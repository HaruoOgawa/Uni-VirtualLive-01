using UnityEngine;

namespace mmdlib
{
    [System.Serializable]
    public class CPmxTransform
    {
        public Vector3 m_LocalPos = new Vector3();
        public Quaternion m_LocalRotate = Quaternion.identity;
        public Vector3 m_LocalScale = Vector3.one;

        public Matrix4x4 m_WorldMatrix = Matrix4x4.identity;

        public CPmxTransform()
        {
        }

        public CPmxTransform(Vector3 LocalPos, Quaternion LocalRotate, Vector3 LocalScale)
        {
            this.m_LocalPos = LocalPos;
            this.m_LocalRotate = LocalRotate;
            this.m_LocalScale = LocalScale;
        }

        public Matrix4x4 GetLocalMatrix()
        {
            return
                Matrix4x4.Translate(m_LocalPos) *
                Matrix4x4.Rotate(m_LocalRotate) *
                Matrix4x4.Scale(m_LocalScale);
        }

        public Vector3 GetLocalPos()
        {
            return m_LocalPos;
        }

        public void SetLocalPos(Vector3 Pos)
        {
            m_LocalPos = Pos;
        }

        public Quaternion GetLocalRot()
        {
            return m_LocalRotate;
        }

        public void SetLocalRot(Quaternion Rot)
        {
            m_LocalRotate = Rot;
        }

        public Vector3 GetLocalScale()
        {
            return m_LocalScale;
        }

        public void SetLocalScale(Vector3 Scale)
        {
            m_LocalScale = Scale;
        }

        public Matrix4x4 GetWorldMatrix()
        {
            return m_WorldMatrix;
        }

        public void SetWorldMatrix(Matrix4x4 matrix)
        {
            m_WorldMatrix = matrix;
        }

        public Vector3 GetWorldPos()
        {
            return m_WorldMatrix.GetPosition();
        }
    }
}