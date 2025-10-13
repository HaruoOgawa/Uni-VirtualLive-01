using UnityEngine;
using UnityEngine.Rendering;

public class CPostProcess
{
    CRenderTarget m_WriteRT = null;
    CRenderTarget m_ReadRT = null;

    // FXAAフィルター
    CFXAAFilter m_FXAAFilter = new CFXAAFilter();

    public CPostProcess()
    {
        m_WriteRT = new CRenderTarget();
        m_WriteRT.Create(Screen.width, Screen.height, 1, RenderTextureFormat.ARGB32, RenderTextureFormat.Depth, 24);

        m_ReadRT = new CRenderTarget();
        m_ReadRT.Create(Screen.width, Screen.height, 1, RenderTextureFormat.ARGB32, RenderTextureFormat.Depth, 24);
    }

    public bool Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera, CRenderTarget finalResultRT, CSceneController sceneController)
    {
        // ここまでの描画結果をコピー
        m_ReadRT.CopyFrameBuffer(context, commandBuffer, finalResultRT);

        // FXAA
        if (!m_FXAAFilter.Draw(context, commandBuffer, camera, m_ReadRT, m_WriteRT, sceneController)) return false;
        SwapRT(); // レンダーターゲットをスワップ

        // Bloom

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
