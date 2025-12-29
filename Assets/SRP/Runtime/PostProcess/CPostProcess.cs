using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using srp.render;

namespace srp.postprocess
{
    public class CPostProcess
    {
        CRenderTarget m_WriteRT = null;
        CRenderTarget m_ReadRT = null;

        public CPostProcess()
        {
        }

        public bool Create(int ScreenWidth, int ScreenHeight, List<CPostProcessFeature> processFeatures)
        {
            m_WriteRT = new CRenderTarget();
            m_WriteRT.Create(ScreenWidth, ScreenHeight, 1, RenderTextureFormat.ARGBFloat, RenderTextureFormat.Depth, 24);

            m_ReadRT = new CRenderTarget();
            m_ReadRT.Create(ScreenWidth, ScreenHeight, 1, RenderTextureFormat.ARGBFloat, RenderTextureFormat.Depth, 24);

            foreach (var feature in processFeatures)
            {
                if(feature == null) continue;

                if(!feature.Create(ScreenWidth, ScreenHeight)) return false;
            }

            return true;
        }

        public void Release(List<CPostProcessFeature> processFeatures)
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

            foreach (var feature in processFeatures)
            {
                if (feature == null) continue;

                feature.Release();
            }
        }

        public bool Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera, CRenderTarget finalResultRT, 
            CSceneController sceneController,  List<CPostProcessFeature> processFeatures)
        {
            // ここまでの描画結果をコピー
            m_ReadRT.CopyFrameBuffer(context, commandBuffer, finalResultRT);

            // カスタムポストプロセス
            foreach (var feature in processFeatures)
            {
                if(feature == null) continue;

                if (!feature.Draw(context, commandBuffer, camera, m_ReadRT, m_WriteRT, sceneController)) continue;

                SwapRT(); // レンダーターゲットをスワップ
            }

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
