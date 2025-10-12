using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class CCustomRenderer
{
    // シーン
    CSceneController m_SceneController = new CSceneController();

    // コマンドバッファ
    CommandBuffer m_CommandBuffer = new CommandBuffer();

    // デファードレンダリング GBufferパス
    CRenderPass m_GBufferGenPass = new CRenderPass("GBufferGenPass");

    // デファードレンダリング
    CRenderPass m_GBufferLightPass = new CRenderPass("GBufferLightPass"); // GBufferライティングパス
    CRenderPass m_GBufferIndirectLightPass = new CRenderPass("GBufferIndirectLightPass"); // GBuffer間接照明パス

    // フォアグラウンドレンダーパス
    CRenderPass m_ForegroundPass = new CRenderPass("ForegroundPass");

    // シャドウマップパス
    SShadowDescriptor m_ShadowDescriptor = new SShadowDescriptor();
    CRenderPass m_ShadowMapPass = new CRenderPass("ShadowMapPass");

    // 最終描画結果
    CRenderPass m_MainResultPass = new CRenderPass("MainResultPass");

    public CCustomRenderer()
    {
        Create();
    }

    void Create()
    {
        // ShadowMapPass
        {
            // ShadowMap描画のRenderList API CreateShadowListは内部的に自動でShadowCasterのShaderPassのみが収集されるのでこれは不要
            //m_ShadowMapPass.AddShaderTag("ShadowCaster");

            CRenderTarget renderTarget = new CRenderTarget();
            renderTarget.Create(m_ShadowDescriptor.Resolution, m_ShadowDescriptor.Resolution, 1, RenderTextureFormat.Shadowmap, RenderTextureFormat.Depth, 24);

            m_ShadowMapPass.SetRenderTarget(renderTarget);
        }

        // GBufferGenPass
        {
            m_GBufferGenPass.AddShaderTag("CustomGBufferGen");
            CRenderTarget renderTarget = new CRenderTarget();
            renderTarget.Create(Screen.width, Screen.height, 5, RenderTextureFormat.ARGBFloat, RenderTextureFormat.Depth, 24);

            m_GBufferGenPass.SetRenderTarget(renderTarget);
        }

        // GBufferLightPass
        {
            m_GBufferLightPass.AddShaderTag("CustomGBufferLight");

            CRenderTarget renderTarget = new CRenderTarget();
            renderTarget.Create(Screen.width, Screen.height, 1, RenderTextureFormat.ARGB32, RenderTextureFormat.Depth, 24);

            m_GBufferLightPass.SetRenderTarget(renderTarget);
        }

        // GBufferIndirectLightPass
        {
            m_GBufferIndirectLightPass.AddShaderTag("CustomGBufferIndirectLight");

            CRenderTarget renderTarget = new CRenderTarget();
            renderTarget.Create(Screen.width, Screen.height, 1, RenderTextureFormat.ARGB32, RenderTextureFormat.Depth, 24);

            m_GBufferIndirectLightPass.SetRenderTarget(renderTarget);
        }

        // ForegroundPass
        {
            m_ForegroundPass.AddShaderTag("SRPDefaultUnlit");
            m_ForegroundPass.AddShaderTag("Always");
            m_ForegroundPass.AddShaderTag("ForwardBase");
            m_ForegroundPass.AddShaderTag("PrepassBase");
            m_ForegroundPass.AddShaderTag("Vertex");
            m_ForegroundPass.AddShaderTag("VertexLMRGBM");
            m_ForegroundPass.AddShaderTag("VertexLM");

            CRenderTarget renderTarget = new CRenderTarget();
            renderTarget.Create(Screen.width, Screen.height, 1, RenderTextureFormat.ARGB32, RenderTextureFormat.Depth, 24);

            m_ForegroundPass.SetRenderTarget(renderTarget);
        }

        // MainResultPass
        {
            m_MainResultPass.AddShaderTag("SRPDefaultUnlit");
            m_MainResultPass.AddShaderTag("Always");
            m_MainResultPass.AddShaderTag("ForwardBase");
            m_MainResultPass.AddShaderTag("PrepassBase");
            m_MainResultPass.AddShaderTag("Vertex");
            m_MainResultPass.AddShaderTag("VertexLMRGBM");
            m_MainResultPass.AddShaderTag("VertexLM");

            //CRenderTarget renderTarget = new CRenderTarget();
            //renderTarget.Create(Screen.width, Screen.height, 1, RenderTextureFormat.ARGB32, RenderTextureFormat.Depth, 24);

            //m_MainResultPass.SetRenderTarget(renderTarget);
        }
    }

    public bool Render(ScriptableRenderContext context, Camera camera)
    {
        // カメラ位置に基づいてビューフラスタムカリングを実行
        if (!m_SceneController.ExecuteCulling(context, camera, m_ShadowDescriptor)) return false;

        // ライト情報を準備
        m_SceneController.PrepareLightArray(context, m_CommandBuffer, true);

        // シャドウマッピング
        {
            m_ShadowMapPass.Begin(context, m_CommandBuffer, camera, true, true);
            if(!m_SceneController.DrawShadowMap(context, m_CommandBuffer, camera, m_ShadowDescriptor)) return false;
            m_ShadowMapPass.End(context, m_CommandBuffer, camera);
        }

        // デファードレンダリング
        {
            // GBuffer生成
            {
                SPassDescriptor descriptor = new SPassDescriptor();
                descriptor.TargetShaderTags = m_GBufferGenPass.GetTargetShaderTags();

                m_GBufferGenPass.Begin(context, m_CommandBuffer, camera);
                m_SceneController.Draw(context, m_CommandBuffer, camera, descriptor, null);
                m_GBufferGenPass.End(context, m_CommandBuffer, camera);
            }

            // GBufferライティング
            {
                // デファードライトパスにGBufferパスの深度をコピーする
                if (!m_GBufferLightPass.GetRenderTarget().CopyDepthBuffer(context, m_CommandBuffer, m_GBufferGenPass.GetRenderTarget())) return false;

                SPassDescriptor descriptor = new SPassDescriptor();
                descriptor.TargetShaderTags = m_GBufferLightPass.GetTargetShaderTags();

                m_GBufferLightPass.Begin(context, m_CommandBuffer, camera, true, false);
                m_SceneController.DrawDeferredLight(context, m_CommandBuffer, camera, descriptor, m_GBufferGenPass.GetRenderTarget(), m_ShadowMapPass.GetRenderTarget());
                m_GBufferLightPass.End(context, m_CommandBuffer, camera);
            }
            
            // GBufferライティング(間接照明)
            {
                // デファードライトパスにGBufferLightPassのカラー・深度をコピーする
                if (!m_GBufferIndirectLightPass.GetRenderTarget().CopyFrameBuffer(context, m_CommandBuffer, m_GBufferLightPass.GetRenderTarget())) return false;

                SPassDescriptor descriptor = new SPassDescriptor();
                descriptor.TargetShaderTags = m_GBufferIndirectLightPass.GetTargetShaderTags();

                m_GBufferIndirectLightPass.Begin(context, m_CommandBuffer, camera, false, false);
                m_SceneController.DrawDeferredIndirectLight(context, m_CommandBuffer, camera, descriptor, m_GBufferGenPass.GetRenderTarget());
                m_GBufferIndirectLightPass.End(context, m_CommandBuffer, camera);
            }
        }

        // フォアグラウンドレンダリング
        {
            // フォアグラウンドパス(ForegroundPass)にGBufferLightPassのカラー・深度をコピーする
            if (!m_ForegroundPass.GetRenderTarget().CopyFrameBuffer(context, m_CommandBuffer, m_GBufferIndirectLightPass.GetRenderTarget())) return false;

            SPassDescriptor descriptor = new SPassDescriptor();
            descriptor.TargetShaderTags = m_ForegroundPass.GetTargetShaderTags();
            descriptor.DrawSky = true;

            m_ForegroundPass.Begin(context, m_CommandBuffer, camera, false, false);
            m_SceneController.Draw(context, m_CommandBuffer, camera, descriptor, m_ShadowMapPass.GetRenderTarget());
            m_SceneController.DrawGizmo(context, m_CommandBuffer, camera);
            m_ForegroundPass.End(context, m_CommandBuffer, camera);
        }

        // 最終描画結果
        {
            m_MainResultPass.Begin(context, m_CommandBuffer, camera, true, true);
            m_SceneController.DrawFullScreenRT(context, m_CommandBuffer, camera, m_ForegroundPass.GetRenderTarget().GetColorBuffer());
            m_MainResultPass.End(context, m_CommandBuffer, camera);
        }

        return true;
    }
}
