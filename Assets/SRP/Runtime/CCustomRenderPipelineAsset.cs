using UnityEngine;
using UnityEngine.Rendering;

namespace srp
{
    [CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
    public class CCustomRenderPipelineAsset : RenderPipelineAsset
    {
        [SerializeField] SRenderSettings settings = new SRenderSettings(new SPostProcessSettings(1.0f, 1.0f));

        protected override RenderPipeline CreatePipeline()
        {
            return new CCustomRenderPipeline(settings);
        }
    }
}