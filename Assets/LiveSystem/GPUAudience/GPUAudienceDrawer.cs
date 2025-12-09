using NUnit.Framework;
using UnityEngine;
using System.Collections;

namespace livesystem
{
    public class GPUAudienceDrawer : MonoBehaviour
    {
        [SerializeField] int InstanceCount = 1;
        [SerializeField] GameObject target = null;
        [SerializeField] Material material = null;

        SkinnedMeshRenderer[] MeshRenderers;
        void Start()
        {
            if (target == null) return;

            MeshRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        void Update()
        {
            if(material == null) return;

            foreach (var renderer in MeshRenderers)
            {
                Mesh mesh = renderer.sharedMesh;
                if(mesh == null) continue;

                for(int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                {
                    RenderParams renderParams = new RenderParams(material);

                    Graphics.RenderMeshPrimitives(renderParams, mesh, subMeshIndex, InstanceCount);
                }
            }
        }
    }
}
