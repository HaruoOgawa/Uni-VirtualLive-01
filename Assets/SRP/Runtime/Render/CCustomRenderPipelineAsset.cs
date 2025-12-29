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
        [SerializeField] List<CPostProcessFeature> processFeatures = new List<CPostProcessFeature>();

        protected override RenderPipeline CreatePipeline()
        {
            return new CCustomRenderPipeline(processFeatures);
        }
    }
}