using UnityEngine;
using System.Collections.Generic;

namespace mmdlib
{
    public static class CPmxHumanoidBoneMapper
    {
        public static EHumanoidBones CastStringToBoneName(string boneName)
        {
            if (boneName == "全ての親") return EHumanoidBones.AllParent;
            else if (boneName == "右足IK親") return EHumanoidBones.LeftLegIKParent;
            else if (boneName == "右足ＩＫ") return EHumanoidBones.LeftLegIK;
            else if (boneName == "右つま先ＩＫ") return EHumanoidBones.LeftToesIK;
            else if (boneName == "左足IK親") return EHumanoidBones.RightLegIKParent;
            else if (boneName == "左足ＩＫ") return EHumanoidBones.RightLegIK;
            else if (boneName == "左つま先ＩＫ") return EHumanoidBones.RightToesIK;
            else if (boneName == "センター") return EHumanoidBones.Center;
            else if (boneName == "グルーブ") return EHumanoidBones.Group;
            else if (boneName == "腰") return EHumanoidBones.Hips;
            else if (boneName == "下半身") return EHumanoidBones.LowerBody;
            else if (boneName == "上半身") return EHumanoidBones.Spine;
            else if (boneName == "上半身2") return EHumanoidBones.Chest;
            else if (boneName == "首") return EHumanoidBones.Neck;
            else if (boneName == "頭") return EHumanoidBones.Head;
            else if (boneName == "左目") return EHumanoidBones.RightEye;
            else if (boneName == "右目") return EHumanoidBones.LeftEye;
            else if (boneName == "左肩") return EHumanoidBones.RightShoulder;
            else if (boneName == "左腕") return EHumanoidBones.RightUpperArm;
            else if (boneName == "左ひじ") return EHumanoidBones.RightLowerArm;
            else if (boneName == "左手首") return EHumanoidBones.RightHand;
            else if (boneName == "右肩") return EHumanoidBones.LeftShoulder;
            else if (boneName == "右腕") return EHumanoidBones.LeftUpperArm;
            else if (boneName == "右ひじ") return EHumanoidBones.LeftLowerArm;
            else if (boneName == "右手首") return EHumanoidBones.LeftHand;
            else if (boneName == "左足") return EHumanoidBones.RightUpperLeg;
            else if (boneName == "左ひざ") return EHumanoidBones.RightLowerLeg;
            else if (boneName == "左足首") return EHumanoidBones.RightFoot;
            else if (boneName == "左つま先") return EHumanoidBones.RightToes;
            else if (boneName == "右足") return EHumanoidBones.LeftUpperLeg;
            else if (boneName == "右ひざ") return EHumanoidBones.LeftLowerLeg;
            else if (boneName == "右足首") return EHumanoidBones.LeftFoot;
            else if (boneName == "右つま先") return EHumanoidBones.LeftToes;
            else if (boneName == "左親指０") return EHumanoidBones.RightThumbProximal;
            else if (boneName == "左親指１") return EHumanoidBones.RightThumbIntermediate;
            else if (boneName == "左親指２") return EHumanoidBones.RightThumbDistal;
            else if (boneName == "左人指１") return EHumanoidBones.RightIndexProximal;
            else if (boneName == "左人指２") return EHumanoidBones.RightIndexIntermediate;
            else if (boneName == "左人指３") return EHumanoidBones.RightIndexDistal;
            else if (boneName == "左中指１") return EHumanoidBones.RightMiddleProximal;
            else if (boneName == "左中指２") return EHumanoidBones.RightMiddleIntermediate;
            else if (boneName == "左中指３") return EHumanoidBones.RightMiddleDistal;
            else if (boneName == "左薬指１") return EHumanoidBones.RightRingProximal;
            else if (boneName == "左薬指２") return EHumanoidBones.RightRingIntermediate;
            else if (boneName == "左薬指３") return EHumanoidBones.RightRingDistal;
            else if (boneName == "左小指１") return EHumanoidBones.RightLittleProximal;
            else if (boneName == "左小指２") return EHumanoidBones.RightLittleIntermediate;
            else if (boneName == "左小指３") return EHumanoidBones.RightLittleDistal;
            else if (boneName == "右親指0") return EHumanoidBones.LeftThumbProximal;
            else if (boneName == "右親指１") return EHumanoidBones.LeftThumbIntermediate;
            else if (boneName == "右親指２") return EHumanoidBones.LeftThumbDistal;
            else if (boneName == "右人指１") return EHumanoidBones.LeftIndexProximal;
            else if (boneName == "右人指２") return EHumanoidBones.LeftIndexIntermediate;
            else if (boneName == "右人指３") return EHumanoidBones.LeftIndexDistal;
            else if (boneName == "右中指１") return EHumanoidBones.LeftMiddleProximal;
            else if (boneName == "右中指２") return EHumanoidBones.LeftMiddleIntermediate;
            else if (boneName == "右中指３") return EHumanoidBones.LeftMiddleDistal;
            else if (boneName == "右薬指１") return EHumanoidBones.LeftRingProximal;
            else if (boneName == "右薬指２") return EHumanoidBones.LeftRingIntermediate;
            else if (boneName == "右薬指３") return EHumanoidBones.LeftRingDistal;
            else if (boneName == "右小指１") return EHumanoidBones.LeftLittleProximal;
            else if (boneName == "右小指２") return EHumanoidBones.LeftLittleIntermediate;
            else if (boneName == "右小指３") return EHumanoidBones.LeftLittleDistal;

            return EHumanoidBones.None;
        }

        public static string GetFullLinkBoneName(EHumanoidBones BoneName)
        {
            string Result = string.Empty;

            EHumanoidBones CurrentBone = BoneName;

            for(;;)
            {
                Result = GetStrBoneName(CurrentBone) + Result;

                CurrentBone = GetParentBoneName(CurrentBone);

                if (CurrentBone == EHumanoidBones.None) break;

                Result = "/" + Result;
            }

            return Result;
        }

        public static string GetStrBoneName(EHumanoidBones BoneName)
        {
            switch (BoneName)
            {
                case EHumanoidBones.Hips:
                    return "Hips";
                case EHumanoidBones.LeftUpperLeg:
                    return "LeftUpperLeg";
                case EHumanoidBones.RightUpperLeg:
                    return "RightUpperLeg";
                case EHumanoidBones.LeftLowerLeg:
                    return "LeftLowerLeg";
                case EHumanoidBones.RightLowerLeg:
                    return "RightLowerLeg";
                case EHumanoidBones.LeftFoot:
                    return "LeftFoot";
                case EHumanoidBones.RightFoot:
                    return "RightFoot";
                case EHumanoidBones.Spine:
                    return "Spine";
                case EHumanoidBones.Chest:
                    return "Chest";
                case EHumanoidBones.UpperChest:
                    return "UpperChest";
                case EHumanoidBones.Neck:
                    return "Neck";
                case EHumanoidBones.Head:
                    return "Head";
                case EHumanoidBones.LeftShoulder:
                    return "LeftShoulder";
                case EHumanoidBones.RightShoulder:
                    return "RightShoulder";
                case EHumanoidBones.LeftUpperArm:
                    return "LeftUpperArm";
                case EHumanoidBones.RightUpperArm:
                    return "RightUpperArm";
                case EHumanoidBones.LeftLowerArm:
                    return "LeftLowerArm";
                case EHumanoidBones.RightLowerArm:
                    return "RightLowerArm";
                case EHumanoidBones.LeftHand:
                    return "LeftHand";
                case EHumanoidBones.RightHand:
                    return "RightHand";
                case EHumanoidBones.LeftToes:
                    return "LeftToes";
                case EHumanoidBones.RightToes:
                    return "RightToes";
                case EHumanoidBones.LeftEye:
                    return "LeftEye";
                case EHumanoidBones.RightEye:
                    return "RightEye";
                case EHumanoidBones.Jaw:
                    return "Jaw";
                case EHumanoidBones.LeftThumbProximal:
                    return "LeftThumbProximal";
                case EHumanoidBones.LeftThumbIntermediate:
                    return "LeftThumbIntermediate";
                case EHumanoidBones.LeftThumbDistal:
                    return "LeftThumbDistal";
                case EHumanoidBones.LeftIndexProximal:
                    return "LeftIndexProximal";
                case EHumanoidBones.LeftIndexIntermediate:
                    return "LeftIndexIntermediate";
                case EHumanoidBones.LeftIndexDistal:
                    return "LeftIndexDistal";
                case EHumanoidBones.LeftMiddleProximal:
                    return "LeftMiddleProximal";
                case EHumanoidBones.LeftMiddleIntermediate:
                    return "LeftMiddleIntermediate";
                case EHumanoidBones.LeftMiddleDistal:
                    return "LeftMiddleDistal";
                case EHumanoidBones.LeftRingProximal:
                    return "LeftRingProximal";
                case EHumanoidBones.LeftRingIntermediate:
                    return "LeftRingIntermediate";
                case EHumanoidBones.LeftRingDistal:
                    return "LeftRingDistal";
                case EHumanoidBones.LeftLittleProximal:
                    return "LeftLittleProximal";
                case EHumanoidBones.LeftLittleIntermediate:
                    return "LeftLittleIntermediate";
                case EHumanoidBones.LeftLittleDistal:
                    return "LeftLittleDistal";
                case EHumanoidBones.RightThumbProximal:
                    return "RightThumbProximal";
                case EHumanoidBones.RightThumbIntermediate:
                    return "RightThumbIntermediate";
                case EHumanoidBones.RightThumbDistal:
                    return "RightThumbDistal";
                case EHumanoidBones.RightIndexProximal:
                    return "RightIndexProximal";
                case EHumanoidBones.RightIndexIntermediate:
                    return "RightIndexIntermediate";
                case EHumanoidBones.RightIndexDistal:
                    return "RightIndexDistal";
                case EHumanoidBones.RightMiddleProximal:
                    return "RightMiddleProximal";
                case EHumanoidBones.RightMiddleIntermediate:
                    return "RightMiddleIntermediate";
                case EHumanoidBones.RightMiddleDistal:
                    return "RightMiddleDistal";
                case EHumanoidBones.RightRingProximal:
                    return "RightRingProximal";
                case EHumanoidBones.RightRingIntermediate:
                    return "RightRingIntermediate";
                case EHumanoidBones.RightRingDistal:
                    return "RightRingDistal";
                case EHumanoidBones.RightLittleProximal:
                    return "RightLittleProximal";
                case EHumanoidBones.RightLittleIntermediate:
                    return "RightLittleIntermediate";
                case EHumanoidBones.RightLittleDistal:
                    return "RightLittleDistal";
                case EHumanoidBones.AllParent:
                    return "AllParent";
                case EHumanoidBones.Center:
                    return "Center";
                case EHumanoidBones.Group:
                    return "Group";
                case EHumanoidBones.LowerBody:
                    return "LowerBody";
                case EHumanoidBones.RightLegIKParent:
                    return "RightLegIKParent";
                case EHumanoidBones.RightLegIK:
                    return "RightLegIK";
                case EHumanoidBones.RightToesIK:
                    return "RightToesIK";
                case EHumanoidBones.LeftLegIKParent:
                    return "LeftLegIKParent";
                case EHumanoidBones.LeftLegIK:
                    return "LeftLegIK";
                case EHumanoidBones.LeftToesIK:
                    return "LeftToesIK";
                case EHumanoidBones.None:
                default:
                    break;
            }

            return string.Empty;
        }

        public static EHumanoidBones GetParentBoneName(EHumanoidBones BoneName)
        {
            switch (BoneName)
            {
                case EHumanoidBones.AllParent:
                    return EHumanoidBones.None;
                case EHumanoidBones.RightLegIKParent:
                    return EHumanoidBones.AllParent;
                case EHumanoidBones.RightLegIK:
                    return EHumanoidBones.RightLegIKParent;
                case EHumanoidBones.RightToesIK:
                    return EHumanoidBones.RightLegIK;
                case EHumanoidBones.LeftLegIKParent:
                    return EHumanoidBones.AllParent;
                case EHumanoidBones.LeftLegIK:
                    return EHumanoidBones.LeftLegIKParent;
                case EHumanoidBones.LeftToesIK:
                    return EHumanoidBones.LeftLegIK;
                case EHumanoidBones.Center:
                    return EHumanoidBones.AllParent;
                case EHumanoidBones.Group:
                    return EHumanoidBones.Center;
                case EHumanoidBones.Hips:
                    return EHumanoidBones.Group;
                case EHumanoidBones.LeftUpperLeg:
                case EHumanoidBones.RightUpperLeg:
                    return EHumanoidBones.Hips;
                case EHumanoidBones.LeftLowerLeg:
                    return EHumanoidBones.LeftUpperLeg;
                case EHumanoidBones.RightLowerLeg:
                    return EHumanoidBones.RightUpperLeg;
                case EHumanoidBones.LeftFoot:
                    return EHumanoidBones.LeftLowerLeg;
                case EHumanoidBones.RightFoot:
                    return EHumanoidBones.RightLowerLeg;
                case EHumanoidBones.Spine:
                    return EHumanoidBones.Hips;
                case EHumanoidBones.Chest:
                    return EHumanoidBones.Spine;
                case EHumanoidBones.UpperChest:
                    return EHumanoidBones.Chest;
                case EHumanoidBones.Neck:
                    return EHumanoidBones.UpperChest;
                case EHumanoidBones.Head:
                    return EHumanoidBones.Neck;
                case EHumanoidBones.LeftShoulder:
                    return EHumanoidBones.UpperChest;
                case EHumanoidBones.RightShoulder:
                    return EHumanoidBones.UpperChest;
                case EHumanoidBones.LeftUpperArm:
                    return EHumanoidBones.LeftShoulder;
                case EHumanoidBones.RightUpperArm:
                    return EHumanoidBones.RightShoulder;
                case EHumanoidBones.LeftLowerArm:
                    return EHumanoidBones.LeftUpperArm;
                case EHumanoidBones.RightLowerArm:
                    return EHumanoidBones.RightUpperArm;
                case EHumanoidBones.LeftHand:
                    return EHumanoidBones.LeftLowerArm;
                case EHumanoidBones.RightHand:
                    return EHumanoidBones.RightLowerArm;
                case EHumanoidBones.LeftToes:
                    return EHumanoidBones.LeftFoot;
                case EHumanoidBones.RightToes:
                    return EHumanoidBones.RightFoot;
                case EHumanoidBones.LeftEye:
                case EHumanoidBones.RightEye:
                case EHumanoidBones.Jaw:
                    return EHumanoidBones.Head;
                case EHumanoidBones.LeftThumbProximal:
                    return EHumanoidBones.LeftHand;
                case EHumanoidBones.LeftThumbIntermediate:
                    return EHumanoidBones.LeftThumbProximal;
                case EHumanoidBones.LeftThumbDistal:
                    return EHumanoidBones.LeftThumbIntermediate;
                case EHumanoidBones.LeftIndexProximal:
                    return EHumanoidBones.LeftHand;
                case EHumanoidBones.LeftIndexIntermediate:
                    return EHumanoidBones.LeftIndexProximal;
                case EHumanoidBones.LeftIndexDistal:
                    return EHumanoidBones.LeftIndexIntermediate;
                case EHumanoidBones.LeftMiddleProximal:
                    return EHumanoidBones.LeftHand;
                case EHumanoidBones.LeftMiddleIntermediate:
                    return EHumanoidBones.LeftMiddleProximal;
                case EHumanoidBones.LeftMiddleDistal:
                    return EHumanoidBones.LeftMiddleIntermediate;
                case EHumanoidBones.LeftRingProximal:
                    return EHumanoidBones.LeftHand;
                case EHumanoidBones.LeftRingIntermediate:
                    return EHumanoidBones.LeftRingProximal;
                case EHumanoidBones.LeftRingDistal:
                    return EHumanoidBones.LeftRingIntermediate;
                case EHumanoidBones.LeftLittleProximal:
                    return EHumanoidBones.LeftHand;
                case EHumanoidBones.LeftLittleIntermediate:
                    return EHumanoidBones.LeftLittleProximal;
                case EHumanoidBones.LeftLittleDistal:
                    return EHumanoidBones.LeftLittleIntermediate;
                case EHumanoidBones.RightThumbProximal:
                    return EHumanoidBones.RightHand;
                case EHumanoidBones.RightThumbIntermediate:
                    return EHumanoidBones.RightThumbProximal;
                case EHumanoidBones.RightThumbDistal:
                    return EHumanoidBones.RightThumbIntermediate;
                case EHumanoidBones.RightIndexProximal:
                    return EHumanoidBones.RightHand;
                case EHumanoidBones.RightIndexIntermediate:
                    return EHumanoidBones.RightIndexProximal;
                case EHumanoidBones.RightIndexDistal:
                    return EHumanoidBones.RightIndexIntermediate;
                case EHumanoidBones.RightMiddleProximal:
                    return EHumanoidBones.RightHand;
                case EHumanoidBones.RightMiddleIntermediate:
                    return EHumanoidBones.RightMiddleProximal;
                case EHumanoidBones.RightMiddleDistal:
                    return EHumanoidBones.RightMiddleIntermediate;
                case EHumanoidBones.RightRingProximal:
                    return EHumanoidBones.RightHand;
                case EHumanoidBones.RightRingIntermediate:
                    return EHumanoidBones.RightRingProximal;
                case EHumanoidBones.RightRingDistal:
                    return EHumanoidBones.RightRingIntermediate;
                case EHumanoidBones.RightLittleProximal:
                    return EHumanoidBones.RightHand;
                case EHumanoidBones.RightLittleIntermediate:
                    return EHumanoidBones.RightLittleProximal;
                case EHumanoidBones.RightLittleDistal:
                    return EHumanoidBones.RightLittleIntermediate;

                // MMD拡張ボーン
                case EHumanoidBones.LowerBody:
                    return EHumanoidBones.None;
                default:
                    break;
            }

            return EHumanoidBones.None;
        }
    }
}

