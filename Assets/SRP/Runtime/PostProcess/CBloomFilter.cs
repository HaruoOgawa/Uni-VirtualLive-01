using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CBloomFilter
{
    Material m_BrightnessMat = null;
    Material m_BlurMat = null;
    Material m_MixMat = null;

    CRenderPass m_BrightnessPass = null; 
    List<CRenderPass> m_ReducePassList = new List<CRenderPass>();
    CRenderPass m_MixPass = null;

    const int m_NumOfReducePass = 3;

    public CBloomFilter()
    {
        m_BrightnessMat = new Material(Shader.Find("SRP/BloomBrigtness"));
        m_BlurMat = new Material(Shader.Find("SRP/BloomBlur1Pass"));
        m_MixMat = new Material(Shader.Find("SRP/BloomMix"));

        // BrightnessPass
        {
            m_BrightnessPass = new CRenderPass("BrightnessPass");

            CRenderTarget renderTarget = new CRenderTarget();
            renderTarget.Create(Screen.width, Screen.height, 1, RenderTextureFormat.ARGB32, RenderTextureFormat.Depth, 24);

            m_BrightnessPass.SetRenderTarget(renderTarget);
        }

        // ReducePass
        /*for(int i = 0; i < m_NumOfReducePass; i++)
        {
            string PassName = "ReducePass"
            var ReducePass = new CRenderPass()
        }*/
    }

    public bool Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera,
        CRenderTarget readRT, CRenderTarget writeRT, CSceneController sceneController)
    {
        // ‚Æ‚è‚ ‚¦‚·’è”Œˆ‚ß‘Å‚¿
        float Threshold = 1.0f;
        float Intencity = 1.0f;

        // BrightnessPass
        {
            m_BrightnessPass.Begin(context, commandBuffer, camera, true, true);
            m_BrightnessMat.SetTexture("_MainTex", readRT.GetColorBuffer());
            m_BrightnessMat.SetFloat("_Threshold", Threshold);
            m_BrightnessMat.SetFloat("_Intencity", Intencity);
            sceneController.DrawFullScreen(context, commandBuffer, camera, m_BrightnessMat);
            m_BrightnessPass.End(context, commandBuffer, camera);
        }

        // ReducePass


        return true;
    }
}
