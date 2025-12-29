using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using srp.postprocess;
using srp.data;

namespace srp.render
{
    [CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
    public class CCustomRenderPipelineAsset : RenderPipelineAsset
    {
        [SerializeField] SRenderSettings settings = new SRenderSettings(new SPostProcessSettings(1.0f, 1.0f));
        [SerializeField] List<CPostProcessFeature> processFeatures = new List<CPostProcessFeature>();

        protected override RenderPipeline CreatePipeline()
        {
            return new CCustomRenderPipeline(settings, processFeatures);
        }
    }
}