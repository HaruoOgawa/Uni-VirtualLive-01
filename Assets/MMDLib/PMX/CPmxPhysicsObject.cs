using UnityEngine;

namespace mmdlib
{
    [System.Serializable]
    public struct CPmxPhysicsObject
    {
        public EPmxPhysicsType PhysicsType;
        public GameObject PhysicsObj;

        public CPmxPhysicsObject(EPmxPhysicsType _physicsType, GameObject _physicsObj)
        {
            this.PhysicsType = _physicsType;
            this.PhysicsObj = _physicsObj;
        }
    }
}

