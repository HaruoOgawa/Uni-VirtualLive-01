using UnityEngine;
using UnityEngine.Rendering;
using srp.render;

namespace srp.postprocess
{
    [CreateAssetMenu(fileName = "WhiteFilter", menuName = "Scriptable Objects/PostProcessFeature/WhiteFilter")]
    public class CWhiteFilter : CPostProcessFeature
    {
        [SerializeField] [Range(0.0f, 1.0f)] float Rate = 0.0f;

        CRenderPass m_RenderPass = null;

        Material m_Material = null;

        public CWhiteFilter()
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

            m_RenderPass = new CRenderPass("WhitePass");

            m_Material = new Material(Shader.Find("SRP/WhiteOutEffect"));

            return true;
        }

        public override bool Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera,
            CRenderTarget readRT, CRenderTarget writeRT, CSceneController sceneController)
        {
            if (Rate == 0.0 || m_RenderPass == null || m_Material == null) return false;

            // レンダーテクスチャを更新
            m_RenderPass.SetRenderTarget(writeRT);

            // 描画開始
            if (!m_RenderPass.Begin(context, commandBuffer, camera, false)) return false;
            m_Material.SetTexture("_MainTex", readRT.GetColorBuffer());

            Vector4 _TexelSize = new Vector4();
            _TexelSize.x = 1.0f / writeRT.GetWidth();
            _TexelSize.y = 1.0f / writeRT.GetHeight();
            m_Material.SetVector("_TexelSize", _TexelSize);

            m_Material.SetFloat("_Rate", Rate);

            sceneController.DrawFullScreen(context, commandBuffer, camera, m_Material);

            if (!m_RenderPass.End(context, commandBuffer, camera, false)) return false;

            return true;
        }

        public void SetRate(float val)
        {
            this.Rate = val;
        }
    }
}