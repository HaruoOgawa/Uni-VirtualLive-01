using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace livesystem
{
    public class GPUAudienceDrawer : MonoBehaviour
    {
        [SerializeField] int InstanceCount = 1;
        [SerializeField] GameObject target = null;
        [SerializeField] Material material = null;
        [SerializeField] List<Texture> VATList = new List<Texture>();

        [SerializeField] int RowCount = 1;
        [SerializeField] float Width = 1.0f;
        [SerializeField] float Height = 1.0f;
        [SerializeField] AnimationClip Clip = null;

        SkinnedMeshRenderer[] MeshRenderers;
        List<ComputeBuffer> InvBindPoseBufferList = new List<ComputeBuffer>();
        List<ComputeBuffer> BoneWeightIndexBufferList = new List<ComputeBuffer>();
        ComputeBuffer WorldMatrixBuffer = null;

        Bounds InstanceBounds = new Bounds();
        public GPUAudienceDrawer()
        {
        }

        void OnDestroy()
        {
            // コンピュートバッファを明示的にリリースする
            foreach(ComputeBuffer buffer in InvBindPoseBufferList)
            {
                buffer.Release();
            }

            InvBindPoseBufferList.Clear();

            foreach(ComputeBuffer buffer in BoneWeightIndexBufferList)
            {
                buffer.Release();
            }

            BoneWeightIndexBufferList.Clear();

            WorldMatrixBuffer.Release();
            WorldMatrixBuffer = null;
        }
        void Start()
        {
            if (target == null) return;

            MeshRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();

            // スキンメッシュアニメーション関連のバッファを準備
            foreach (var renderer in MeshRenderers)
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh == null) continue;

                var bones = renderer.bones;

                // 逆バインドポーズ行列のバッファを作成
                ComputeBuffer InvBindPoseBuffer = new ComputeBuffer(mesh.bindposeCount, sizeof(float) * 16);

                // バインドポーズを逆行列にする
                // mesh.bindposesは既に逆行列なので転置は不要
                var invBindposes = mesh.bindposes;
                
                InvBindPoseBuffer.SetData(invBindposes);
                InvBindPoseBufferList.Add(InvBindPoseBuffer);

                // ボーンウェイトインデックスのバッファを作成
                ComputeBuffer boneWeightIndexBuffer = new ComputeBuffer(mesh.boneWeights.Length, (sizeof(float) * 4 + sizeof(int) * 4));
                boneWeightIndexBuffer.SetData(mesh.boneWeights);
                BoneWeightIndexBufferList.Add(boneWeightIndexBuffer);
            }

            // 座標を準備
            Vector3 MinPos = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 MaxPos = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            Matrix4x4[] WorldMatrixArray = new Matrix4x4[InstanceCount];

            for(int i = 0; i < InstanceCount; i++)
            {
                int RowIndex = i % RowCount;
                int ColumnIndex = (i - RowIndex) / RowCount;

                Vector3 pos = new Vector3(
                      Width * (float)(RowIndex), 0.0f, (-1.0f) * Height * (float)(ColumnIndex)  
                );

                Quaternion quat = Quaternion.identity;

                Matrix4x4 WorldMatrix = Matrix4x4.Translate(pos) * Matrix4x4.Rotate(quat);

                WorldMatrixArray[i] = WorldMatrix;

                // バウンディングボックス用のMinMaxを更新
                if(pos.x < MinPos.x) MinPos.x = pos.x;
                if(pos.y < MinPos.y) MinPos.y = pos.y;
                if(pos.z < MinPos.z) MinPos.z = pos.z;

                if(pos.x > MaxPos.x) MaxPos.x = pos.x;
                if(pos.y > MaxPos.y) MaxPos.y = pos.y;
                if(pos.z > MaxPos.z) MaxPos.z = pos.z;
            }


            // バウンディングボックスの再計算
            Vector3 center = (MinPos + MaxPos) * 0.5f;
            Vector3 size = new Vector3(
                Mathf.Abs(MaxPos.x - MinPos.x),
                Mathf.Abs(MaxPos.y - MinPos.y),
                Mathf.Abs(MaxPos.z - MinPos.z)
            );

            // 観衆モデルの身長を考慮
            float height = 1.5f;
            size.y = height;
            center.y = height * 0.5f;

            InstanceBounds.center = center;
            InstanceBounds.size = size;

            WorldMatrixBuffer = new ComputeBuffer(InstanceCount, sizeof(float) * 16);
            WorldMatrixBuffer.SetData(WorldMatrixArray);
        }

        void Update()
        {
            if(material == null || Clip == null) return;

            float FrameRate = Clip.frameRate;
            float StartTime = 0.0f;
            float EndTime = Clip.length;
            float DeltaTime = 1.0f / FrameRate;
            int NumOfFrame = (int)((EndTime - StartTime) / DeltaTime) + 1;

            for (int rendererIndex = 0; rendererIndex < MeshRenderers.Length; rendererIndex++)
            {
                var renderer = MeshRenderers[rendererIndex];
                if (renderer == null) continue;

                Mesh mesh = renderer.sharedMesh;
                if(mesh == null) continue;

                if(rendererIndex < 0 || rendererIndex >= InvBindPoseBufferList.Count) continue;
                ComputeBuffer InvBindPoseBuffer = InvBindPoseBufferList[rendererIndex];

                if (rendererIndex < 0 || rendererIndex >= BoneWeightIndexBufferList.Count) continue;
                ComputeBuffer boneWeightIndexBuffer = BoneWeightIndexBufferList[rendererIndex];

                if (rendererIndex < 0 || rendererIndex >= VATList.Count) continue;
                Texture VAT = VATList[rendererIndex];

                for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                {
                    Material subMeshMaterial = renderer.sharedMaterials[subMeshIndex];

                    // ShaderUniformを設定
                    MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                    propertyBlock.SetMatrix("ParentWorldMatrix", this.gameObject.transform.localToWorldMatrix);
                    propertyBlock.SetBuffer("WorldMatrixList", WorldMatrixBuffer);
                    propertyBlock.SetBuffer("InvBindPoseList", InvBindPoseBuffer);
                    propertyBlock.SetBuffer("BoneWeightIndexList", boneWeightIndexBuffer);
                    propertyBlock.SetTexture("_VAT", VAT);
                    propertyBlock.SetInt("_RowCount", RowCount);
                    propertyBlock.SetFloat("_StartTime", StartTime);
                    propertyBlock.SetFloat("_EndTime", EndTime);
                    propertyBlock.SetInt("_NumOfFrame", NumOfFrame);

                    // 描画パラメーター作成
                    RenderParams renderParams = new RenderParams(subMeshMaterial);
                    renderParams.matProps = propertyBlock;
                    renderParams.worldBounds = InstanceBounds;

                    // 描画実行
                    Graphics.RenderMeshPrimitives(renderParams, mesh, subMeshIndex, InstanceCount);
                }
            }
        }
    }
}
