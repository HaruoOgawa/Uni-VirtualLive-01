using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CRenderPass : ARenderPass
{
    public CRenderPass(string PassName) : base(PassName)
    {
    }

    public override void Begin(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera)
    {
        // プロファイラの表記用にバッファ名にカメラ名を割り当てる
        commandBuffer.name = camera.name;
        //commandBuffer.name = m_PassName;

        // カメラのビュープロジェクション行列を設定する
        context.SetupCameraProperties(camera);

        if(m_RenderTarget != null)
        {
            commandBuffer.SetRenderTarget(m_RenderTarget.GetColorBuffers().ToArray(), m_RenderTarget.GetDepthBuffer());
            //commandBuffer.BeginRenderPass(m_Width, m_Height, 1, );
        }

        // これでMRTとかのテクスチャをこのパスで描画するシェーダーに渡すことができる
        //commandBuffer.SetGlobalTexture

        // フレームバッファ(フレームテクスチャ)の初期化コマンドを発行
        commandBuffer.ClearRenderTarget(true, true, UnityEngine.Color.clear);

        // プロファイラ(例えばFrame Debugger)への記録開始
        commandBuffer.BeginSample(BufferName);

        // コマンドをコンテキストに登録
        ExecuteBuffer(context, commandBuffer);
    }

    public override void End(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera)
    {
        // レンダーパス終了
        if (m_RenderTarget != null)
        {
            //commandBuffer.EndRenderPass();
        }

        // プロファイラの記録終了
        commandBuffer.EndSample(BufferName);

        // コマンドをコンテキストに登録
        ExecuteBuffer(context, commandBuffer);

        // コンテキストに積み上げられたコマンドを全て実行する
        context.Submit();
    }
}
