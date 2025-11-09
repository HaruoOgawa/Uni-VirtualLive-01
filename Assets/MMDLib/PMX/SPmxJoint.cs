using UnityEngine;

namespace mmdlib
{
    public enum EPmxJointType
    {
        NONE = -1,

		SPRING_6DOF = 0,
		Generic_6DOF = 1,
		P2P = 2,
		ConeTwist = 3,
		Slider = 5,
	};

    public class SPmxJoint
    {
        public string JointName = string.Empty;
        public string JointNameEN = string.Empty;
        public EPmxJointType PmxJointType = EPmxJointType.NONE;
        public int BodyAIndex = -1;
        public int BodyBIndex = -1;
        public Vector3 Pos = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 Rotate = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 LowerTransLimit = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 UpperTransLimit = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 LowerRotateLimit = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 UpperRotateLimit = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 TransSpring = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 RotateSpring = new Vector3(0.0f, 0.0f, 0.0f);

        public SPmxJoint()
        {
        }
    };
}