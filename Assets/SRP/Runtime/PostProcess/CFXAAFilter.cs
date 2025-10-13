using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.XR.XRDisplaySubsystem;

public class CFXAAFilter
{
    CRenderPass m_RenderPass = new CRenderPass("FXAAPass");

    Material m_Material = null;

    public CFXAAFilter()
    {
        m_Material = new Material(Shader.Find("SRP/FXAA_PostProcess"));
    }

    public bool Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera,
        CRenderTarget readRT, CRenderTarget writeRT, CSceneController sceneController)
    {
        // レンダーテクスチャを更新
        m_RenderPass.SetRenderTarget(writeRT);

        // 描画開始
        m_RenderPass.Begin(context, commandBuffer, camera);
        m_Material.SetTexture("_MainTex", readRT.GetColorBuffer());

        sceneController.DrawFullScreen(context, commandBuffer, camera, m_Material);

        m_RenderPass.End(context, commandBuffer, camera);

        return true;
    }
}
