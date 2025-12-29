using srp.data;
using srp.render;
using UnityEngine;
using UnityEngine.Rendering;

namespace srp.postprocess
{
    [CreateAssetMenu(fileName = "LightShaftFilter", menuName = "Scriptable Objects/PostProcessFeature/LightShaftFilter")]
    public class CLightShaftFilter : CPostProcessFeature
    {
        [SerializeField] bool Enabled = true;

        CRenderPass m_LightShaftGeometryPass = null;
        CRenderPass m_LightShaftMixPass = null;

        Material m_Material = null;

        public CLightShaftFilter()
        {
        }

        public override void Release()
        {
            if(m_LightShaftGeometryPass != null)
            {
                m_LightShaftGeometryPass.Release();
                m_LightShaftGeometryPass = null;
            }

            if (m_LightShaftMixPass != null)
            {
                m_LightShaftMixPass.Release();
                m_LightShaftMixPass = null;
            }

            m_Material = null;
        }

        public override bool Create(int ScreenWidth, int ScreenHeight)
        {
            Release();

            // LightShaftGeometryPass
            {
                m_LightShaftGeometryPass = new CRenderPass("LightShaftGeometryPass");

                m_LightShaftGeometryPass.AddShaderTag("SRPLightShaftPass");

                CRenderTarget renderTarget = new CRenderTarget();
                renderTarget.Create(ScreenWidth, ScreenHeight, 1, RenderTextureFormat.ARGBFloat, RenderTextureFormat.Depth, 24);

                m_LightShaftGeometryPass.SetRenderTarget(renderTarget);
            }

            // LightShaftMixPass
            {
                m_LightShaftMixPass = new CRenderPass("LightShaftMixPass");
            }

            m_Material = new Material(Shader.Find("SRP/LightShaftMixEffect"));

            return true;
        }

        public override bool Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera,
            CRenderTarget readRT, CRenderTarget writeRT, CSceneController sceneController)
        {
            if (!Enabled) return false;

            if (m_LightShaftGeometryPass == null || m_LightShaftMixPass == null || m_Material == null) return false;

            // LightShaftGeometryPass
            {
                SPassDescriptor descriptor = new SPassDescriptor();
                descriptor.TargetShaderTags = m_LightShaftGeometryPass.GetTargetShaderTags();
                descriptor.DrawSky = false;

                if (!m_LightShaftGeometryPass.Begin(context, commandBuffer, camera, false)) return false;
                sceneController.Draw(context, commandBuffer, camera, descriptor, null);
                if (!m_LightShaftGeometryPass.End(context, commandBuffer, camera, false)) return false;
            }

            // レンダーテクスチャを更新
            m_LightShaftMixPass.SetRenderTarget(writeRT);

            // LightShaftMixPass
            {
                if (!m_LightShaftMixPass.Begin(context, commandBuffer, camera, false)) return false;
                m_Material.SetTexture("_MainTex", readRT.GetColorBuffer());
                m_Material.SetTexture("_LightShaftTex", m_LightShaftGeometryPass.GetRenderTarget().GetColorBuffer());

                Vector4 _TexelSize = new Vector4();
                _TexelSize.x = 1.0f / writeRT.GetWidth();
                _TexelSize.y = 1.0f / writeRT.GetHeight();
                m_Material.SetVector("_TexelSize", _TexelSize);

                sceneController.DrawFullScreen(context, commandBuffer, camera, m_Material);

                if (!m_LightShaftMixPass.End(context, commandBuffer, camera, false)) return false;
            }

            return true;
        }
    }
}