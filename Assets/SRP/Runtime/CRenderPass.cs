using UnityEngine;
using UnityEngine.Rendering;

public class CRenderPass : ARenderPass
{
    public override void Begin(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera)
    {
        // プロファイラの表記用にバッファ名にカメラ名を割り当てる
        commandBuffer.name = camera.name;

        // カメラのビュープロジェクション行列を設定する
        context.SetupCameraProperties(camera);

        //RenderTargetIdentifier renderTargetIdentifier = new RenderTargetIdentifier();
        //renderTargetIdentifier

        //commandBuffer.SetRenderTarget(renderTargetIdentifier);
        //commandBuffer.BeginRenderPass

        // フレームバッファ(フレームテクスチャ)の初期化コマンドを発行
        commandBuffer.ClearRenderTarget(true, true, Color.clear);

        // プロファイラ(例えばFrame Debugger)への記録開始
        commandBuffer.BeginSample("aaa");

        // コマンドをコンテキストに登録
        ExecuteBuffer(context, commandBuffer);
    }

    public override void End(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera)
    {
        // プロファイラの記録終了
        commandBuffer.EndSample("aaa");

        // コマンドをコンテキストに登録
        ExecuteBuffer(context, commandBuffer);

        // コンテキストに積み上げられたコマンドを全て実行する
        context.Submit();
    }
}
