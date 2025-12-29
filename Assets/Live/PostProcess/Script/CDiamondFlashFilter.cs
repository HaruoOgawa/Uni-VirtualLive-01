using UnityEngine;
using UnityEngine.Rendering;
using srp.render;

namespace srp.postprocess
{
    [CreateAssetMenu(fileName = "DiamondFlashFilter", menuName = "Scriptable Objects/PostProcessFeature/DiamondFlashFilter")]
    public class CDiamondFlashFilter : CPostProcessFeature
    {
        [SerializeField] bool Enabled = true;

        [SerializeField] string TargetTag = string.Empty;

        CRenderPass m_RenderPass = null;

        Material m_Material = null;

        public CDiamondFlashFilter()
        {
        }

        public override void Release()
        {
            if (m_RenderPass != null)
            {
                m_RenderPass.Release();
                m_RenderPass = null;
            }

            m_Material = null;
        }

        public override bool Create(int ScreenWidth, int ScreenHeight)
        {
            Release();

            m_RenderPass = new CRenderPass("DiamondFlashPass");

            m_Material = new Material(Shader.Find("SRP/DiamondFlashEffect"));

            return true;
        }

        public override bool Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera,
            CRenderTarget readRT, CRenderTarget writeRT, CSceneController sceneController)
        {
            if (!Enabled) return false;

            if (camera.tag != TargetTag || m_RenderPass == null || m_Material == null) return false;

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
