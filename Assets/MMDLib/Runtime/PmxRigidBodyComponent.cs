using UnityEngine;

namespace mmdlib
{
    [System.Serializable]
    public class PmxRigidBodyComponent : MonoBehaviour
    {
        public EPmxPhysicsType PhysicsType = EPmxPhysicsType.STATIC;
        public CPmxTransform DefaultTransform = new CPmxTransform();
        
        public PmxRigidBodyComponent()
        {
        }

        public void Init(EPmxPhysicsType _physicsType, GameObject _physicsObj )
        {
            this.PhysicsType = _physicsType;
            this.DefaultTransform = new CPmxTransform(_physicsObj.transform.localPosition, _physicsObj.transform.localRotation, _physicsObj.transform.localScale);
        }
    }
}

