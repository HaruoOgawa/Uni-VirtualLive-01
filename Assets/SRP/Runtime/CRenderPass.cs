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
        // カメラのビュープロジェクション行列を設定する
        context.SetupCameraProperties(camera);

        if(m_RenderTarget != null)
        {
            commandBuffer.SetRenderTarget(m_RenderTarget.GetColorBuffers().ToArray(), m_RenderTarget.GetDepthBuffer());
            //commandBuffer.BeginRenderPass(m_Width, m_Height, 1, );
        }

        // フレームバッファ(フレームテクスチャ)の初期化コマンドを発行
        commandBuffer.ClearRenderTarget(true, true, UnityEngine.Color.clear);

        // プロファイラ(例えばFrame Debugger)への記録開始
        commandBuffer.BeginSample(BufferName);

        // プロファイラ開始コマンドをコンテキストに登録
        ExecuteBuffer(context, commandBuffer, camera.name);
    }

    public override void End(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera)
    {
        // レンダーパス終了
        if (m_RenderTarget != null)
        {
            //commandBuffer.EndRenderPass();
        }

        // これまでの描画コマンドをコンテキストに登録
        ExecuteBuffer(context, commandBuffer, m_PassName);

        // プロファイラの記録終了
        commandBuffer.EndSample(BufferName);

        // プロファイラ終了コマンドをコンテキストに登録
        ExecuteBuffer(context, commandBuffer, camera.name);

        // コンテキストに積み上げられたコマンドを全て実行する
        context.Submit();
    }
}
