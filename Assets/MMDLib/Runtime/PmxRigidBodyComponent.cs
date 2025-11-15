using UnityEngine;

namespace mmdlib
{
    [System.Serializable]
    public class PmxRigidBodyComponent : MonoBehaviour
    {
        public EPmxPhysicsType PhysicsType = EPmxPhysicsType.STATIC;
        public CPmxTransform DefaultTransform = new CPmxTransform();
        public Vector3 LowerTransLimit = Vector3.zero;
        public Vector3 UpperTransLimit = Vector3.zero;
        public Vector3 LowerRotateLimit = Vector3.zero;
        public Vector3 UpperRotateLimit = Vector3.zero;

        public PmxRigidBodyComponent()
        {
        }

        public void Init(EPmxPhysicsType _physicsType, GameObject _physicsObj )
        {
            this.PhysicsType = _physicsType;
            this.DefaultTransform = new CPmxTransform(_physicsObj.transform.localPosition, _physicsObj.transform.localRotation, _physicsObj.transform.localScale);
        }

        public void SetLimit(Vector3 _LowerTransLimit, Vector3 _UpperTransLimit, Vector3 _LowerRotateLimit, Vector3 _UpperRotateLimit)
        {
            this.LowerTransLimit = _LowerTransLimit;
            this.UpperTransLimit = _UpperTransLimit;
            this.LowerRotateLimit = _LowerRotateLimit;
            this.UpperRotateLimit = _UpperRotateLimit;
        }
    }
}

