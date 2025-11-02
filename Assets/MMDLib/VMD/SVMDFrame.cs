using UnityEngine;
using System.Collections.Generic;

namespace mmdlib
{
    public struct SVMDFrame
    {
        public EHumanoidBones BoneName;

        public int FrameIndex;

        public Vector3 Pos;
        public Quaternion Rot;

        public List<Vector2> XPointList;
        public List<Vector2> YPointList;
        public List<Vector2> ZPointList;
        public List<Vector2> RPointList;

        public SVMDFrame(EHumanoidBones _boneName, int _frameIndex, Vector3 _pos, Quaternion _rot,
        List<Vector2> _XPointList, List<Vector2> _YPointList, List<Vector2> _ZPointList, List<Vector2> _RPointList)
        {
            this.BoneName = _boneName;
            this.FrameIndex = _frameIndex;
            this.Pos = _pos;
            this.Rot = _rot;
            this.XPointList = _XPointList;
            this.YPointList = _YPointList;
            this.ZPointList = _ZPointList;
            this.RPointList = _RPointList;
        }
    };

    public struct SVMDSkinFrame
    {
        int FrameIndex;
        float Weight;

        SVMDSkinFrame(int f = -1, float w = 0.0f)
        {
            this.FrameIndex = f;
            this.Weight = w;
        }
    };

    public enum EHumanoidBones
    {
        None = -1,

        Hips,
        LeftUpperLeg,
        RightUpperLeg,
        LeftLowerLeg,
        RightLowerLeg,
        LeftFoot,
        RightFoot,
        Spine,
        Chest,
        UpperChest,
        Neck,
        Head,
        LeftShoulder,
        RightShoulder,
        LeftUpperArm,
        RightUpperArm,
        LeftLowerArm,
        RightLowerArm,
        LeftHand,
        RightHand,
        LeftToes,
        RightToes,
        LeftEye,
        RightEye,
        Jaw,
        LeftThumbProximal,
        LeftThumbIntermediate,
        LeftThumbDistal,
        LeftIndexProximal,
        LeftIndexIntermediate,
        LeftIndexDistal,
        LeftMiddleProximal,
        LeftMiddleIntermediate,
        LeftMiddleDistal,
        LeftRingProximal,
        LeftRingIntermediate,
        LeftRingDistal,
        LeftLittleProximal,
        LeftLittleIntermediate,
        LeftLittleDistal,
        RightThumbProximal,
        RightThumbIntermediate,
        RightThumbDistal,
        RightIndexProximal,
        RightIndexIntermediate,
        RightIndexDistal,
        RightMiddleProximal,
        RightMiddleIntermediate,
        RightMiddleDistal,
        RightRingProximal,
        RightRingIntermediate,
        RightRingDistal,
        RightLittleProximal,
        RightLittleIntermediate,
        RightLittleDistal,
        AllParent, // 全ての親(MMD用) 
        Center, // センター(MMD用)
        Group, // グルーブ(MMD用)
        LowerBody, // 下半身(MMD用)
        RightLegIKParent, // 右足IK親(MMD用)
        RightLegIK, // 右足ＩＫ(MMD用)
        RightToesIK, // 右つま先ＩＫ(MMD用)
        LeftLegIKParent, // 左足IK親(MMD用)
        LeftLegIK, // 左足ＩＫ(MMD用)
        LeftToesIK, // 左つま先ＩＫ(MMD用)

        // 最大ヒューマノイドボーン数
        Max = 64,
    };

    public enum EBlendShapeName
    {
        None = -1,

		Neutral = 0,
		Blink = 1,
		Blink_L = 2,
		Blink_R = 3,
		Joy = 4, // 喜び
		Angry = 5,
		Sorrow = 6,
		Fun = 7, // 楽しみ
		A = 8,
		I = 9,
		U = 10,
		E = 11,
		O = 12,

		// 任意シェイプ
		Optional_Shape_0 = 13,
		Optional_Shape_1 = 14,
		Optional_Shape_2 = 15,
		Optional_Shape_3 = 16,
		Optional_Shape_4 = 17,
		Optional_Shape_5 = 18,
		Optional_Shape_6 = 19,
		Optional_Shape_7 = 20,
		Optional_Shape_8 = 21,
		Optional_Shape_9 = 22,
		Optional_Shape_10 = 23,
		Optional_Shape_11 = 24,
		Optional_Shape_12 = 25,
		Optional_Shape_13 = 26,
		Optional_Shape_14 = 27,
		Optional_Shape_15 = 28,
		Optional_Shape_16 = 29,
		Optional_Shape_17 = 30,

		Max = 31,
	};
}