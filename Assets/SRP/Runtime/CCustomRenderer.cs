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
    CRenderPass m_ForegroundPass = new CRenderPass();

    public CCustomRenderer()
    {
        Create();
    }

    void Create()
    {
        //
        m_ForegroundPass.AddShaderTag("SRPDefaultUnlit");
        m_ForegroundPass.AddShaderTag("Always");
        m_ForegroundPass.AddShaderTag("ForwardBase");
        m_ForegroundPass.AddShaderTag("PrepassBase");
        m_ForegroundPass.AddShaderTag("Vertex");
        m_ForegroundPass.AddShaderTag("VertexLMRGBM");
        m_ForegroundPass.AddShaderTag("VertexLM");

        //

    }

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        // フォアグラウンドレンダリング
        {
            SPassDescriptor descriptor = new SPassDescriptor();
            descriptor.TargetShaderTags = m_ForegroundPass.GetTargetShaderTags();
            descriptor.DrawSky = true;

            m_ForegroundPass.Begin(context, m_CommandBuffer, camera);
            m_SceneController.Draw(context, m_CommandBuffer, camera, descriptor);
            m_ForegroundPass.End(context, m_CommandBuffer, camera);
        }

        /**
         * GBufferを描画
         * GBufferPass.Begin(context, camera);
         * SceneController().Draw(GBufferPass.GetTargetShaderList(), IsDrawSkyBox)
         * GBufferPass.End(context, camera);
         * 
         */
    }
}
