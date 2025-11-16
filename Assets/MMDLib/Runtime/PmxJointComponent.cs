using System;
using Unity.Mathematics;
using UnityEngine;

namespace mmdlib
{
    public class PmxJointComponent : MonoBehaviour
    {
        [SerializeField] PmxRigidBodyComponent FixedRigidBody = null;
        [SerializeField] PmxRigidBodyComponent DynamicRigidBody = null;

        [SerializeField] Vector3 LowerTransLimit = Vector3.zero;
        [SerializeField] Vector3 UpperTransLimit = Vector3.zero;
        [SerializeField] Vector3 LowerRotateLimit = Vector3.zero;
        [SerializeField] Vector3 UpperRotateLimit = Vector3.zero;

        [SerializeField] SpringJoint Joint = null;

        public PmxJointComponent()
        {
        }

        public void Init(PmxRigidBodyComponent _FixedRigidBody, PmxRigidBodyComponent DynamicRigidBody,
            Vector3 _LowerTransLimit, Vector3 _UpperTransLimit, Vector3 _LowerRotateLimit, Vector3 _UpperRotateLimit)
        {
            this.FixedRigidBody = _FixedRigidBody;
            this.DynamicRigidBody = DynamicRigidBody;
            this.LowerTransLimit = _LowerTransLimit;
            this.UpperTransLimit = _UpperTransLimit;
            this.LowerRotateLimit = _LowerRotateLimit;
            this.UpperRotateLimit = _UpperRotateLimit;

            this.Joint = DynamicRigidBody.GetComponent<SpringJoint>();
        }

        void FixedUpdate()
        {
            if (this.FixedRigidBody == null || this.DynamicRigidBody == null || this.Joint == null) return;

            Vector3 worldConnectedAnchor = this.DynamicRigidBody.transform.rotation * this.Joint.connectedAnchor;

            // Dynamic(ぶら下がっている方)なRigidBodyのJointと繋がっている現在のワールド座標とワールド回転
            Vector3 DynamicConnectedPos = this.DynamicRigidBody.transform.position + worldConnectedAnchor;
            Quaternion DynamicConnectedRotate = this.DynamicRigidBody.transform.rotation;

            // Fixed(Dynamicを固定している方)なRigidBodyのJointと繋がっている現在のワールド座標とワールド回転
            Vector3 FixedConnectedPos = GetFixedConnectedPos(DynamicConnectedPos);
            Quaternion FixedConnectedRotate = this.FixedRigidBody.transform.rotation;

            // 回転制限
            {
                // Fixedに対してDynamicが相対的にどこまで回転することが許されているのかをチェックしてその範囲内に治める
                Quaternion DeltaRotate = Quaternion.Inverse(FixedConnectedRotate) * DynamicConnectedRotate;
                Vector3 DeltaEuler = DeltaRotate.eulerAngles;

                // Unity Quaternionのオイラー角は0 ～ 360で返ってくるので-180 ～ 180 に変換する
                DeltaEuler = CastA360ToA180(DeltaEuler);

                // 制限実行
                DeltaEuler.x = Mathf.Clamp(DeltaEuler.x, this.LowerRotateLimit.x, this.UpperRotateLimit.x);
                DeltaEuler.y = Mathf.Clamp(DeltaEuler.y, this.LowerRotateLimit.y, this.UpperRotateLimit.y);
                DeltaEuler.z = Mathf.Clamp(DeltaEuler.z, this.LowerRotateLimit.z, this.UpperRotateLimit.z);

                DeltaRotate = Quaternion.Euler(CastA180ToA360(DeltaEuler));

                this.DynamicRigidBody.transform.rotation = FixedConnectedRotate * DeltaRotate;
            }

            // 位置制限
        }

        // 0 ～ 360 の角度を -180 ～ 180 に直す
        Vector3 CastA360ToA180(Vector3 SrcEuler)
        {
            Vector3 DstEuler = SrcEuler;

            // 0 ～ 360 の間を確約する
            DstEuler.x = math.fmod(DstEuler.x, 360.0f);
            DstEuler.y = math.fmod(DstEuler.y, 360.0f);
            DstEuler.z = math.fmod(DstEuler.z, 360.0f);

            if (DstEuler.x > 180.0f) DstEuler.x = (-1.0f) * (360.0f - DstEuler.x);
            if (DstEuler.y > 180.0f) DstEuler.y = (-1.0f) * (360.0f - DstEuler.y);
            if (DstEuler.z > 180.0f) DstEuler.z = (-1.0f) * (360.0f - DstEuler.z);

            return DstEuler;
        }

        // -180 ～ 180の角度を 0 ～ 360 に直す
        Vector3 CastA180ToA360(Vector3 SrcEuler)
        {
            Vector3 DstEuler = SrcEuler;

            if (DstEuler.x < 0.0f) DstEuler.x = 360.0f - DstEuler.x;
            if (DstEuler.y < 0.0f) DstEuler.y = 360.0f - DstEuler.y;
            if (DstEuler.z < 0.0f) DstEuler.z = 360.0f - DstEuler.z;

            return DstEuler;
        }

        Vector3 GetFixedConnectedPos(Vector3 DynamicConnectedPos)
        {
            Vector3 FixedConnectedPos = Vector3.zero;
            /*Collider fixedCollider = this.FixedRigidBody.GetComponent<Collider>();
           
            Type type = fixedCollider.GetType();

            if (type == typeof(SphereCollider))
            {
                SphereCollider sphereCollider = (SphereCollider)fixedCollider;

                // コライダーの座標からDynamicの接続点までの方向にスフィアコライダーの半径分だけ動かしたのが
                // SphereColliderの時のFixedのJoint接続点
                Vector3 ColliderWorldCenter = this.FixedRigidBody.transform.rotation * sphereCollider.center;
                Vector3 ColliderWorldPos = this.FixedRigidBody.transform.position + ColliderWorldCenter;

                Vector3 Dir = Vector3.Normalize(DynamicConnectedPos - ColliderWorldPos);

                FixedConnectedPos = ColliderWorldPos + Dir * sphereCollider.radius;
            }
            else if (type == typeof(CapsuleCollider))
            {
                CapsuleCollider capsuleCollider = (CapsuleCollider)fixedCollider;

                // ローカルのカプセルコライダーの方向
                Vector3 LocalCapsuleDir = new Vector3(
                    (capsuleCollider.direction == 0) ? 1.0f : 0.0f,
                    (capsuleCollider.direction == 1) ? 1.0f : 0.0f,
                    (capsuleCollider.direction == 2) ? 1.0f : 0.0f
                );


                Vector3 ColliderWorldPos = this.FixedRigidBody.transform.position + ColliderWorldCenter;
            }
            else if (type == typeof(BoxCollider))
            {

            }*/

            return FixedConnectedPos;
        }
    }
}