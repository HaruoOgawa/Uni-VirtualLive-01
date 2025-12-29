using UnityEngine;
using UnityEngine.Rendering;
using srp.render;

namespace srp.postprocess
{
    public class CFXAAFilter
    {
        CRenderPass m_RenderPass = null;

        Material m_Material = null;

        public CFXAAFilter()
        {
        }

        public void Release()
        {
            if (m_RenderPass != null)
            {
                m_RenderPass.Release();
                m_RenderPass = null;
            }

            m_Material = null;
        }

        public bool Create(int ScreenWidth, int ScreenHeight)
        {
            m_RenderPass = new CRenderPass("FXAAPass");

            m_Material = new Material(Shader.Find("SRP/FXAA_PostProcess"));

            return true;
        }

        public bool Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera,
            CRenderTarget readRT, CRenderTarget writeRT, CSceneController sceneController)
        {
            // レンダーテクスチャを更新
            m_RenderPass.SetRenderTarget(writeRT);

            // 描画開始
            if (!m_RenderPass.Begin(context, commandBuffer, camera, false)) return false;
            m_Material.SetTexture("_MainTex", readRT.GetColorBuffer());

            Vector4 _TexelSize = new Vector4();
            _TexelSize.x = 1.0f / writeRT.GetWidth();
            _TexelSize.y = 1.0f / writeRT.GetHeight();
            m_Material.SetVector("_TexelSize", _TexelSize);

            sceneController.DrawFullScreen(context, commandBuffer, camera, m_Material);

            if (!m_RenderPass.End(context, commandBuffer, camera, false)) return false;

            return true;
        }
    }

}
