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

    [System.Serializable]
    public enum EPmxPhysicsType
    {
        STATIC = 0, // É{Å[Éìí«è](static)
        DYNAMIC = 1, // ï®óùââéZ(dynamic)
        DYNAMIC_BONE_ALIGNMENT = 2, // ï®óùââéZ + Boneà íuçáÇÌÇπ
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