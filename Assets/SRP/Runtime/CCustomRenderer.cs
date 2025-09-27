using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SPassDescriptor
{
    public List<ShaderTagId> TargetShaderTags = new List<ShaderTagId>();
    public bool DrawSky = false;
    public bool DrawOpaque = true;
    public bool DrawTransparent = true;
}

public class CCustomRenderer
{
    // シーン
    CSceneController m_SceneController = new CSceneController();

    // コマンドバッファ
    CommandBuffer m_CommandBuffer = new CommandBuffer();

    // フォアグラウンドレンダーパス
    CRenderPass m_ForegroundPass = new CRenderPass("ForegroundPass");

    // デファードレンダリング
    // GBufferパス
    CRenderPass m_GBufferGenPass = new CRenderPass("GBufferGenPass");

    // GBufferライティングパス
    CRenderPass m_GBufferLightPass = new CRenderPass("GBufferLightPass");

    public CCustomRenderer()
    {
        Create();
    }

    void Create()
    {
        // ForegroundPass
        {
            m_ForegroundPass.AddShaderTag("SRPDefaultUnlit");
            m_ForegroundPass.AddShaderTag("Always");
            m_ForegroundPass.AddShaderTag("ForwardBase");
            m_ForegroundPass.AddShaderTag("PrepassBase");
            m_ForegroundPass.AddShaderTag("Vertex");
            m_ForegroundPass.AddShaderTag("VertexLMRGBM");
            m_ForegroundPass.AddShaderTag("VertexLM");
        }

        // GBufferGenPass
        {
            m_GBufferGenPass.AddShaderTag("CustomGBufferGen");
            CRenderTarget renderTarget = new CRenderTarget();
            renderTarget.Create(Screen.width, Screen.height, 5, RenderTextureFormat.ARGB32, RenderTextureFormat.Depth, 24);

            m_GBufferGenPass.SetRenderTarget(renderTarget);
        }

        // GBufferLightPass
        {
            m_GBufferLightPass.AddShaderTag("CustomGBufferLight");

            CRenderTarget renderTarget = new CRenderTarget();
            renderTarget.Create(Screen.width, Screen.height, 1, RenderTextureFormat.ARGB32, RenderTextureFormat.Depth, 24);

            m_GBufferLightPass.SetRenderTarget(renderTarget);
        }
    }

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        // デファードレンダリング
        {
            // GBuffer生成
            {
                SPassDescriptor descriptor = new SPassDescriptor();
                descriptor.TargetShaderTags = m_GBufferGenPass.GetTargetShaderTags();

                m_GBufferGenPass.Begin(context, m_CommandBuffer, camera);
                m_SceneController.Draw(context, m_CommandBuffer, camera, descriptor);
                m_GBufferGenPass.End(context, m_CommandBuffer, camera);
            }

            // GBufferライティング
            {
                SPassDescriptor descriptor = new SPassDescriptor();
                descriptor.TargetShaderTags = m_GBufferLightPass.GetTargetShaderTags();

                m_GBufferLightPass.Begin(context, m_CommandBuffer, camera);
                m_SceneController.DrawDeferredLight(context, m_CommandBuffer, camera, descriptor, m_GBufferGenPass.GetRenderTarget());
                m_GBufferLightPass.End(context, m_CommandBuffer, camera);
            }
        }

        // フォアグラウンドレンダリング
        {
            SPassDescriptor descriptor = new SPassDescriptor();
            descriptor.TargetShaderTags = m_ForegroundPass.GetTargetShaderTags();
            descriptor.DrawSky = true;

            m_ForegroundPass.Begin(context, m_CommandBuffer, camera);
            m_SceneController.Draw(context, m_CommandBuffer, camera, descriptor);
            m_SceneController.DrawGizmo(context, m_CommandBuffer, camera);
            m_ForegroundPass.End(context, m_CommandBuffer, camera);
        }
    }
}
