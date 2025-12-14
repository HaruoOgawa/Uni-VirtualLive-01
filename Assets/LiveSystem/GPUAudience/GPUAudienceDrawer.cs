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
                var invBindposes = mesh.bindposes;
                for (int b = 0; b < invBindposes.Length; b++)
                {
                    invBindposes[b] = invBindposes[b].inverse;
                }
                
                InvBindPoseBuffer.SetData(invBindposes);
                InvBindPoseBufferList.Add(InvBindPoseBuffer);

                // ボーンウェイトインデックスのバッファを作成
                ComputeBuffer boneWeightIndexBuffer = new ComputeBuffer(mesh.boneWeights.Length, (sizeof(float) * 4 + sizeof(int) * 4));
                boneWeightIndexBuffer.SetData(mesh.boneWeights);
                BoneWeightIndexBufferList.Add(boneWeightIndexBuffer);
            }

            // 座標を準備
            Matrix4x4[] WorldMatrixArray = new Matrix4x4[InstanceCount];

            for(int i = 0; i < InstanceCount; i++)
            {
                int RowIndex = i % RowCount;
                int ColumnIndex = (i - RowIndex) / RowCount;

                Vector3 pos = new Vector3(
                      Width * (float)(RowIndex), 0.0f, Height * (float)(ColumnIndex)  
                );

                Quaternion quat = Quaternion.identity;

                Matrix4x4 WorldMatrix = Matrix4x4.Translate(pos) * Matrix4x4.Rotate(quat);

                WorldMatrixArray[i] = WorldMatrix;
            }

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
                    RenderParams renderParams = new RenderParams(material);
                    renderParams.matProps = propertyBlock;

                    // 描画実行
                    Graphics.RenderMeshPrimitives(renderParams, mesh, subMeshIndex, InstanceCount);
                }
            }
        }
    }
}
