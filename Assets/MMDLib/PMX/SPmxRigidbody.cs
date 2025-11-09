using UnityEngine;

namespace mmdlib
{
    public enum EPmxPhysicsShape
    {
        NONE = -1,
		SPHERE = 0,
		BOX = 1,
		CAPSULE = 2,
	};

    public enum EPmxPhysicsType
    {
        STATIC = 0,
		DYNAMIC = 1,
		DYNAMIC_JOINT = 2,
	};

    public class SPmxRigidbody
    {
        public string RigidbodyName = string.Empty;
        public string RigidbodyNameEN = string.Empty;
        public int RelationBoneIndex = -1;
        public ushort group = 0;
        public ushort NoneCollideGroupFlag = 0;
        public EPmxPhysicsShape PhysicsShape = EPmxPhysicsShape.NONE;
        public Vector3 Size = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 Pos = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 Rotate = new Vector3(0.0f, 0.0f, 0.0f);
        public float Mass = 0.0f;
        public float TransDamping = 0.0f;
        public float RotateDamping = 0.0f;
        public float Repulsion = 0.0f;
        public float Friction = 0.0f;
        public EPmxPhysicsType PhysicsType = EPmxPhysicsType.STATIC;

        public SPmxRigidbody()
        {
        }
    };
}