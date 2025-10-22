using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.XR.XRDisplaySubsystem;

namespace srp
{
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

            Vector4 _TexelSize = new Vector4();
            _TexelSize.x = 1.0f / writeRT.GetWidth();
            _TexelSize.y = 1.0f / writeRT.GetHeight();
            m_Material.SetVector("_TexelSize", _TexelSize);

            sceneController.DrawFullScreen(context, commandBuffer, camera, m_Material);

            m_RenderPass.End(context, commandBuffer, camera);

            return true;
        }
    }

}
