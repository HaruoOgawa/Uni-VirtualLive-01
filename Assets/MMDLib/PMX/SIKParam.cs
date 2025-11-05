using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace mmdlib
{
    [System.Serializable]

    public class SIKLink
    {
        // リンクボーンのボーンIndex
       public int IKLinkBoneIndex = -1;

        // 角度制限
        public bool IsLimitAngle = false;
        public Vector3 LowerAngle = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 UpperAngle = new Vector3(0.0f, 0.0f, 0.0f);

        public SIKLink(int iKLinkBoneIndex, bool isLimitAngle, Vector3 lowerAngle, Vector3 upperAngle)
        {
            IKLinkBoneIndex = iKLinkBoneIndex;
            IsLimitAngle = isLimitAngle;
            LowerAngle = lowerAngle;
            UpperAngle = upperAngle;
        }
    };

    // CCD-IKに則ったパラメーター
    [System.Serializable]

    public class SIKParam
    {
        // IKターゲットボーンのボーンIndex
        public int IKTargetBoneIndex = -1;

        // IKループ回数
        public int IKLoopCount = 0;

        // IKループ計算時の1回あたりの制限角度(ラジアン角)
        public float LimitedAngle = 0.0f;

        // IKリンクリスト
        public List<SIKLink> IKLinkList = new List<SIKLink>();

        public SIKParam(int Index, int Loop, float Angle, List<SIKLink> Link)
        {
            IKTargetBoneIndex = Index;
            IKLoopCount = Loop;
            LimitedAngle = Angle;
            IKLinkList = Link;
        }
    };
}
