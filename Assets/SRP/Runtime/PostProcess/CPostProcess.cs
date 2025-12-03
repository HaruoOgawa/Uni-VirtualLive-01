using UnityEngine;
using UnityEngine.Rendering;

namespace srp
{
    public class CPostProcess
    {
        CRenderTarget m_WriteRT = null;
        CRenderTarget m_ReadRT = null;

        // FXAAフィルター
        CFXAAFilter m_FXAAFilter = null;

        // Bloomフィルター
        CBloomFilter m_BloomFilter = null;

        public CPostProcess()
        {
        }

        public bool Create(int ScreenWidth, int ScreenHeight)
        {
            m_FXAAFilter = new CFXAAFilter();
            if (!m_FXAAFilter.Create(ScreenWidth, ScreenHeight)) return false;

            m_BloomFilter = new CBloomFilter();
            if (!m_BloomFilter.Create(ScreenWidth, ScreenHeight)) return false;

            m_WriteRT = new CRenderTarget();
            m_WriteRT.Create(ScreenWidth, ScreenHeight, 1, RenderTextureFormat.ARGBFloat, RenderTextureFormat.Depth, 24);

            m_ReadRT = new CRenderTarget();
            m_ReadRT.Create(ScreenWidth, ScreenHeight, 1, RenderTextureFormat.ARGBFloat, RenderTextureFormat.Depth, 24);

            return true;
        }

        public void Release()
        {
            if(m_WriteRT != null)
            {
                m_WriteRT.Release();
                m_WriteRT = null;
            }

            if (m_ReadRT != null)
            {
                m_ReadRT.Release();
                m_ReadRT = null;
            }

            if (m_FXAAFilter != null)
            {
                m_FXAAFilter.Release();
                m_FXAAFilter = null;
            }

            if (m_BloomFilter != null)
            {
                m_BloomFilter.Release();
                m_BloomFilter = null;
            }
        }

        public bool Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera, CRenderTarget finalResultRT, CSceneController sceneController)
        {
            // ここまでの描画結果をコピー
            m_ReadRT.CopyFrameBuffer(context, commandBuffer, finalResultRT);

            // FXAA
            if (!m_FXAAFilter.Draw(context, commandBuffer, camera, m_ReadRT, m_WriteRT, sceneController)) return false;
            SwapRT(); // レンダーターゲットをスワップ

            // Bloom 
            if (!m_BloomFilter.Draw(context, commandBuffer, camera, m_ReadRT, m_WriteRT, sceneController)) return false;
            SwapRT(); // レンダーターゲットをスワップ

            // 最終描画結果を更新
            finalResultRT.CopyFrameBuffer(context, commandBuffer, m_ReadRT);

            return true;
        }

        private void SwapRT()
        {
            CRenderTarget tmp = m_ReadRT;
            m_ReadRT = m_WriteRT;
            m_WriteRT = tmp;
        }
    }
}
