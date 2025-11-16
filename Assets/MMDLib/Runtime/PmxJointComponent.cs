using System;
using Unity.Mathematics;
using UnityEngine;

namespace mmdlib
{
    public class PmxJointComponent : MonoBehaviour
    {
        [SerializeField] PmxRigidBodyComponent FixedRigidBody = null;
        [SerializeField] PmxRigidBodyComponent DynamicRigidBody = null;

        [SerializeField] GameObject FixedConnetedNode = null;
        [SerializeField] GameObject DynamicConnetedNode = null;

        [SerializeField] Vector3 LowerTransLimit = Vector3.zero;
        [SerializeField] Vector3 UpperTransLimit = Vector3.zero;
        [SerializeField] Vector3 LowerRotateLimit = Vector3.zero;
        [SerializeField] Vector3 UpperRotateLimit = Vector3.zero;

        [SerializeField] SpringJoint Joint = null;

        public PmxJointComponent()
        {
        }

        public void Init(PmxRigidBodyComponent _FixedRigidBody, PmxRigidBodyComponent _DynamicRigidBody,
            GameObject _FixedConnetedNode, GameObject _DynamicConnetedNode,
            Vector3 _LowerTransLimit, Vector3 _UpperTransLimit, Vector3 _LowerRotateLimit, Vector3 _UpperRotateLimit)
        {
            this.FixedRigidBody = _FixedRigidBody;
            this.DynamicRigidBody = _DynamicRigidBody;
            this.FixedConnetedNode = _FixedConnetedNode;
            this.DynamicConnetedNode = _DynamicConnetedNode;
            this.LowerTransLimit = _LowerTransLimit;
            this.UpperTransLimit = _UpperTransLimit;
            this.LowerRotateLimit = _LowerRotateLimit;
            this.UpperRotateLimit = _UpperRotateLimit;

            this.Joint = DynamicRigidBody.GetComponent<SpringJoint>();
        }

        void FixedUpdate()
        {
            // これらはジョイントの接続点の相対位置を見て制限を加える機能
            // なのでrigidbody.constraintsによるフリーズは使わない(重力落下とかが効かなくなる)
            // LowerTransLimit, UpperTransLimit はジョイントの接続点でどれぐらい離れることができるか
            // LowerRotateLimit, UpperRotateLimitはジョイントの接続点がどれぐらいねじれる(回転する)ことができるか
            if (this.FixedConnetedNode == null || this.DynamicConnetedNode == null) return;

            // Fixed(Dynamicを固定している方)なRigidBodyのJointと繋がっている現在のワールド座標とワールド回転
            Vector3 FixedConnectedPos = this.FixedConnetedNode.transform.position;
            //Quaternion FixedConnectedRotate = this.FixedConnetedNode.transform.rotation;

            // Dynamic(ぶら下がっている方)なRigidBodyのJointと繋がっている現在のワールド座標とワールド回転
            Vector3 DynamicConnectedPos = this.DynamicConnetedNode.transform.position;
            //Quaternion DynamicConnectedRotate = this.DynamicConnetedNode.transform.rotation;

            if(this.DynamicRigidBody.name == "アホ毛１")
            {
                float x = 0.0f;
            }

            // 回転制限
            // 回転制限はローカル回転が初期回転よりもその範囲以上動いてはいけないということ
            //if (this.DynamicRigidBody.name == "アホ毛１")
            {
                Quaternion CurrentDynamicLocalRotate = this.DynamicRigidBody.transform.localRotation;
                Vector3 CurrentEuler = CurrentDynamicLocalRotate.eulerAngles;
                CurrentEuler = CastA360ToA180(CurrentEuler); // Unity Quaternionのオイラー角は0 ～ 360で返ってくるので-180 ～ 180 に変換する

                Quaternion DefaultDynamicLocalRotate = this.DynamicRigidBody.DefaultTransform.m_LocalRotate;
                Vector3 DefaultEuler = DefaultDynamicLocalRotate.eulerAngles;
                DefaultEuler = CastA360ToA180(DefaultEuler); // Unity Quaternionのオイラー角は0 ～ 360で返ってくるので-180 ～ 180 に変換する

                // 初期状態からどれぐらい回転しているか
                Vector3 DeltaEuler = CurrentEuler - DefaultEuler;

                // 制限実行
                DeltaEuler.x = Mathf.Clamp(DeltaEuler.x, this.LowerRotateLimit.x, this.UpperRotateLimit.x);
                DeltaEuler.y = Mathf.Clamp(DeltaEuler.y, this.LowerRotateLimit.y, this.UpperRotateLimit.y);
                DeltaEuler.z = Mathf.Clamp(DeltaEuler.z, this.LowerRotateLimit.z, this.UpperRotateLimit.z);

                // 0 ～ 360に戻す
                DeltaEuler = CastA180ToA360(DeltaEuler);

                Quaternion NewDynamicLocalRotate = Quaternion.Euler(DeltaEuler) * DefaultDynamicLocalRotate;

                // ローカル回転として反映
                this.DynamicRigidBody.transform.localRotation = NewDynamicLocalRotate;
                this.DynamicRigidBody.GetComponent<Rigidbody>().rotation = NewDynamicLocalRotate;

                /*// Fixedに対してDynamicが相対的にどこまで回転することが許されているのかをチェックしてその範囲内に治める
                Quaternion DeltaRotate = Quaternion.Inverse(FixedConnectedRotate) * DynamicConnectedRotate;
                Vector3 DeltaEuler = DeltaRotate.eulerAngles;

                // Unity Quaternionのオイラー角は0 ～ 360で返ってくるので-180 ～ 180 に変換する
                DeltaEuler = CastA360ToA180(DeltaEuler);

                // 制限実行
                DeltaEuler.x = Mathf.Clamp(DeltaEuler.x, this.LowerRotateLimit.x, this.UpperRotateLimit.x);
                DeltaEuler.y = Mathf.Clamp(DeltaEuler.y, this.LowerRotateLimit.y, this.UpperRotateLimit.y);
                DeltaEuler.z = Mathf.Clamp(DeltaEuler.z, this.LowerRotateLimit.z, this.UpperRotateLimit.z);

                DeltaRotate = Quaternion.Euler(CastA180ToA360(DeltaEuler));

                this.DynamicRigidBody.transform.rotation = FixedConnectedRotate * DeltaRotate;*/
            }

            // 位置制限
            // 位置制限の時は接続点の位置計算が煩雑になるのでモデルインポート時に登録しておいたDynamicConnetedNode・FixedConnetedNodeを使用
            {
                Vector3 DeltaVec = DynamicConnectedPos - FixedConnectedPos;

                // 制限実行
                DeltaVec.x = Math.Clamp(DeltaVec.x, this.LowerTransLimit.x, this.UpperTransLimit.x);
                DeltaVec.y = Math.Clamp(DeltaVec.y, this.LowerTransLimit.y, this.UpperTransLimit.y);
                DeltaVec.z = Math.Clamp(DeltaVec.z, this.LowerTransLimit.z, this.UpperTransLimit.z);

                Vector3 NewConnectedPos = FixedConnectedPos + DeltaVec;

                // コライダー位置にはオフセットが加わっており、Dynamicの接続点とノード(Bone)の位置は一致しているものと見ている
                this.DynamicRigidBody.transform.position = NewConnectedPos;
                this.DynamicRigidBody.GetComponent<Rigidbody>().position = NewConnectedPos;
            }
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
    }
}