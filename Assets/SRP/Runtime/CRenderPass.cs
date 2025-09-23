using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CRenderPass : ARenderPass
{
    bool m_UseRenderTarget = false;
    int m_Width = 0;
    int m_Height = 0;

    List<RenderTargetIdentifier> m_ColorBuffers = new List<RenderTargetIdentifier>();
    
    RenderTargetIdentifier m_DepthBuffer;

    public override void Begin(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera)
    {
        // プロファイラの表記用にバッファ名にカメラ名を割り当てる
        commandBuffer.name = camera.name;

        // カメラのビュープロジェクション行列を設定する
        context.SetupCameraProperties(camera);

        if(m_UseRenderTarget)
        {
            commandBuffer.SetRenderTarget(m_ColorBuffers.ToArray(), m_DepthBuffer);

            

            //commandBuffer.BeginRenderPass(m_Width, m_Height, 1, );
        }

        // これでMRTとかのテクスチャをこのパスで描画するシェーダーに渡すことができる
        //commandBuffer.SetGlobalTexture

        // フレームバッファ(フレームテクスチャ)の初期化コマンドを発行
        commandBuffer.ClearRenderTarget(true, true, UnityEngine.Color.clear);

        // プロファイラ(例えばFrame Debugger)への記録開始
        commandBuffer.BeginSample("aaa");

        // コマンドをコンテキストに登録
        ExecuteBuffer(context, commandBuffer);
    }

    public override void End(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera)
    {
        // レンダーパス終了
        if (m_UseRenderTarget)
        {
            //commandBuffer.EndRenderPass();
        }

        // プロファイラの記録終了
        commandBuffer.EndSample("aaa");

        // コマンドをコンテキストに登録
        ExecuteBuffer(context, commandBuffer);

        // コンテキストに積み上げられたコマンドを全て実行する
        context.Submit();
    }

    public void CreateMRTRenderTarget(int width, int height, int RenderTargetCount)
    {
        // カラーバッファ作成
        for (int i = 0; i < RenderTargetCount; i++)
        {
            RenderTexture rt = new RenderTexture(width, height, 32, RenderTextureFormat.ARGBFloat);

            m_ColorBuffers.Add(rt);
        }

        // デプスバッファ作成
        {
            RenderTexture rt = new RenderTexture(width, height, 32, RenderTextureFormat.Depth);
            m_DepthBuffer = rt;
        }

        m_UseRenderTarget = true;
        m_Width = width;
        m_Height = height;
    }
}
