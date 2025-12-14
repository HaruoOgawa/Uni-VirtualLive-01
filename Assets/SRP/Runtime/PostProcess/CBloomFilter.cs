using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace srp
{
    struct SBloomIO
    {
        public string DstPass;
        public string SrcPass;

        public SBloomIO(string dst, string src)
        {
            DstPass = dst;
            SrcPass = src;
        }
    };

    public class CBloomFilter
    {
        Material m_BrightnessMat = null;
        Material m_ReduceMat = null;
        Material m_BlurMat = null;
        Material m_MixMat = null;

        List<(SBloomIO Reduce, SBloomIO XBlur, SBloomIO YBlur)> m_ReduceBufBlurList = new List<(SBloomIO Reduce, SBloomIO XBlur, SBloomIO YBlur)>();
        Dictionary<string, CRenderPass> m_RenderPassMap = new Dictionary<string, CRenderPass>();
        CRenderPass m_BloomMixPass = null;

        string m_LastPassName = string.Empty;

        public CBloomFilter()
        {
        }

        public void Release()
        {
            m_BrightnessMat = null;
            m_ReduceMat = null;
            m_BlurMat = null;
            m_MixMat = null;

            m_ReduceBufBlurList.Clear();

            if(m_RenderPassMap.Count > 0)
            {
                foreach (var pass in m_RenderPassMap.Values)
                {
                    pass.Release();
                }

                m_RenderPassMap.Clear();
            }
            
            if(m_BloomMixPass != null)
            {
                m_BloomMixPass.Release();
                m_BloomMixPass = null;
            }

            m_LastPassName = string.Empty;
        }

        public bool Create(int ScreenWidth, int ScreenHeight)
        {
            // マテリアル作成
            m_BrightnessMat = new Material(Shader.Find("SRP/BloomBrigtness"));
            m_ReduceMat = new Material(Shader.Find("SRP/BloomReduceBuffer"));
            m_BlurMat = new Material(Shader.Find("SRP/BloomBlur1Pass"));
            m_MixMat = new Material(Shader.Find("SRP/BloomMix"));

            // ReduceBufferのIOリストを構築
            m_ReduceBufBlurList.Add((new SBloomIO("ReducePass_2x2", "BrigtnessPass"), new SBloomIO("ReducePass_2x2_XBlur", "ReducePass_2x2"), new SBloomIO("ReducePass_2x2_YBlur", "ReducePass_2x2_XBlur")));
            m_ReduceBufBlurList.Add((new SBloomIO("ReducePass_4x4", "ReducePass_2x2_YBlur"), new SBloomIO("ReducePass_4x4_XBlur", "ReducePass_4x4"), new SBloomIO("ReducePass_4x4_YBlur", "ReducePass_4x4_XBlur")));
            m_ReduceBufBlurList.Add((new SBloomIO("ReducePass_8x8", "ReducePass_4x4_YBlur"), new SBloomIO("ReducePass_8x8_XBlur", "ReducePass_8x8"), new SBloomIO("ReducePass_8x8_YBlur", "ReducePass_8x8_XBlur")));

            m_LastPassName = m_ReduceBufBlurList[m_ReduceBufBlurList.Count - 1].YBlur.DstPass;

            // レンダーパスリスト生成
            // BrigtnessPass
            {
                string PassName = "BrigtnessPass";

                CRenderPass renderPass = new CRenderPass(PassName);

                CRenderTarget renderTarget = new CRenderTarget();
                renderTarget.Create(ScreenWidth, ScreenHeight, 1, RenderTextureFormat.ARGBFloat, RenderTextureFormat.Depth, 24);

                renderPass.SetRenderTarget(renderTarget);

                m_RenderPassMap.Add(PassName, renderPass);
            }

            // Reduce and Blur Pass
            {
                for (int i = 0; i < m_ReduceBufBlurList.Count; i++)
                {
                    var ReduceBufTuple = m_ReduceBufBlurList[i];

                    int Rate = (int)Mathf.Pow(2.0f, 1.0f + (float)i);
                    int Size = 2048 / Rate;

                    m_RenderPassMap.Add(ReduceBufTuple.Reduce.DstPass, CreateRenderPass(ReduceBufTuple.Reduce.DstPass, Size, Size));
                    m_RenderPassMap.Add(ReduceBufTuple.XBlur.DstPass, CreateRenderPass(ReduceBufTuple.XBlur.DstPass, Size, Size));
                    m_RenderPassMap.Add(ReduceBufTuple.YBlur.DstPass, CreateRenderPass(ReduceBufTuple.YBlur.DstPass, Size, Size));
                }
            }

            // BloomMixPass
            {
                string PassName = "BloomMixPass";
                m_BloomMixPass = new CRenderPass(PassName);
            }

            return true;
        }

        private CRenderPass CreateRenderPass(string Name, int Width, int Height)
        {
            CRenderPass renderPass = new CRenderPass(Name);

            CRenderTarget renderTarget = new CRenderTarget();
            renderTarget.Create(Width, Height, 1, RenderTextureFormat.ARGBFloat, RenderTextureFormat.Depth, 24);

            renderPass.SetRenderTarget(renderTarget);

            return renderPass;
        }

        public bool Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera,
            CRenderTarget readRT, CRenderTarget writeRT, CSceneController sceneController, SPostProcessSettings settings)
        {

            // BrigtnessPass
            {
                if (!BeginRenderPass("BrigtnessPass", context, commandBuffer, camera)) return false;

                m_BrightnessMat.SetTexture("_MainTex", readRT.GetColorBuffer());
                m_BrightnessMat.SetFloat("_Threshold", settings.Threshold);
                m_BrightnessMat.SetFloat("_Intencity", settings.Intensity);
                sceneController.DrawFullScreen(context, commandBuffer, camera, m_BrightnessMat);

                if (!EndRenderPass("BrigtnessPass", context, commandBuffer, camera)) return false;
            }

            // Reduce and Blur Pass
            foreach (var ReduceBufTuple in m_ReduceBufBlurList)
            {
                // Reduce
                {
                    if (!BeginRenderPass(ReduceBufTuple.Reduce.DstPass, context, commandBuffer, camera)) return false;

                    if (!PrepareRenderTexture(ReduceBufTuple.Reduce.SrcPass, "_MainTex", m_ReduceMat)) return false;
                    sceneController.DrawFullScreen(context, commandBuffer, camera, m_ReduceMat);

                    if (!EndRenderPass(ReduceBufTuple.Reduce.DstPass, context, commandBuffer, camera)) return false;
                }

                // XBlur
                {
                    if (!BeginRenderPass(ReduceBufTuple.XBlur.DstPass, context, commandBuffer, camera)) return false;

                    if (!PrepareRenderTexture(ReduceBufTuple.XBlur.SrcPass, "_MainTex", m_BlurMat)) return false;
                    sceneController.DrawFullScreen(context, commandBuffer, camera, m_BlurMat);
                    m_BlurMat.SetInt("_IsXBlur", 1);

                    if (!EndRenderPass(ReduceBufTuple.XBlur.DstPass, context, commandBuffer, camera)) return false;
                }

                // YBlur
                {
                    if (!BeginRenderPass(ReduceBufTuple.YBlur.DstPass, context, commandBuffer, camera)) return false;

                    if (!PrepareRenderTexture(ReduceBufTuple.YBlur.SrcPass, "_MainTex", m_BlurMat)) return false;
                    sceneController.DrawFullScreen(context, commandBuffer, camera, m_BlurMat);
                    m_BlurMat.SetInt("_IsXBlur", 0);

                    if (!EndRenderPass(ReduceBufTuple.YBlur.DstPass, context, commandBuffer, camera)) return false;
                }
            }

            // BloomMixPass
            {
                m_BloomMixPass.SetRenderTarget(writeRT);

                m_BloomMixPass.Begin(context, commandBuffer, camera, false, true, true);

                m_MixMat.SetTexture("_MainTex", readRT.GetColorBuffer());
                if (!PrepareRenderTexture(m_LastPassName, "_BloomImage", m_MixMat)) return false;
                sceneController.DrawFullScreen(context, commandBuffer, camera, m_MixMat);

                m_BloomMixPass.End(context, commandBuffer, camera, false);
            }

            return true;
        }

        private bool BeginRenderPass(string Name, ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera)
        {
            CRenderPass renderPass = null;

            if (!m_RenderPassMap.TryGetValue(Name, out renderPass)) return false;

            if (!renderPass.Begin(context, commandBuffer, camera, false, true, true)) return false;

            return true;
        }

        private bool EndRenderPass(string Name, ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera)
        {
            CRenderPass renderPass = null;

            if (!m_RenderPassMap.TryGetValue(Name, out renderPass)) return false;

            if (!renderPass.End(context, commandBuffer, camera, false)) return false;

            return true;
        }

        private bool PrepareRenderTexture(string PassName, string UniformName, Material material)
        {
            CRenderPass renderPass = null;

            if (!m_RenderPassMap.TryGetValue(PassName, out renderPass))
            {
                return false;
            }

            CRenderTarget renderTarget = renderPass.GetRenderTarget();
            if (renderTarget == null) return false;

            material.SetTexture(UniformName, renderTarget.GetColorBuffer());

            Vector4 _TexelSize = new Vector4();
            _TexelSize.x = 1.0f / renderTarget.GetWidth();
            _TexelSize.y = 1.0f / renderTarget.GetHeight();
            material.SetVector("_TexelSize", _TexelSize);

            return true;
        }
    }

}
