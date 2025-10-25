using UnityEngine;
using UnityEngine.Rendering;

namespace srp
{
    [CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
    public class CCustomRenderPipelineAsset : RenderPipelineAsset
    {
        [SerializeField] bool PerObjLight = true;

        protected override RenderPipeline CreatePipeline()
        {
            return new CCustomRenderPipeline(PerObjLight);
        }
    }
}