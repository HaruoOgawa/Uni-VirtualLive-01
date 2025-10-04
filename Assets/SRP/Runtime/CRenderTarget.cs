using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CRenderTarget
{
    int m_Width = 0;
    int m_Height = 0;
    int m_RenderTargetCount = 0;

    List<RenderTexture> m_ColorBuffers = new List<RenderTexture>();
    List<RenderTargetIdentifier> m_ColorRTIdentifiers = new List<RenderTargetIdentifier>();

    RenderTexture m_DepthBuffer;
    RenderTargetIdentifier m_DepthRTIdentifier;

    public CRenderTarget()
    {
    }

    public int GetWidth()
    {
        return m_Width;
    }

    public int GetHeight()
    {
        return m_Height;
    }

    public int GetRenderTargetCount()
    {
        return m_RenderTargetCount;
    }

    public List<RenderTexture> GetColorBuffers()
    {
        return m_ColorBuffers;
    }

    public RenderTexture GetColorBuffer(int Index = 0)
    {
        if(Index < 0 || Index >= m_ColorBuffers.Count) return null;

        return m_ColorBuffers[Index];
    }

    public List<RenderTargetIdentifier> GetColorRTIdentifiers()
    {
        return m_ColorRTIdentifiers;
    }

    public RenderTexture GetDepthBuffer()
    {
        return m_DepthBuffer;
    }

    public RenderTargetIdentifier GetDepthRTIdentifier()
    {
        return m_DepthRTIdentifier;
    }

    public bool Create(int width, int height, int RenderTargetCount, RenderTextureFormat ColorFormat, 
        RenderTextureFormat DepthFormat, int DepthBit)
    {
        // カラーバッファ作成
        for (int i = 0; i < RenderTargetCount; i++)
        {
            RenderTexture rt = new RenderTexture(width, height, DepthBit, ColorFormat);

            m_ColorBuffers.Add(rt);
            m_ColorRTIdentifiers.Add(rt);
        }

        // デプスバッファ作成
        {
            RenderTexture rt = new RenderTexture(width, height, DepthBit, DepthFormat);
            m_DepthBuffer = rt;
            m_DepthRTIdentifier = rt;
        }

        m_Width = width;
        m_Height = height;
        m_RenderTargetCount = RenderTargetCount;

        return true;
    }

    public bool CopyFrameBuffer(ScriptableRenderContext context, CommandBuffer commandBuffer, CRenderTarget Src, bool Color, bool Depth)
    {
        // カラーバッファをコピー
        if (Color && Src.GetRenderTargetCount() == m_RenderTargetCount)
        {
            for(int i = 0; i < m_RenderTargetCount; i++)
            {
                var SrcRT = Src.GetColorBuffers()[i];
                var DstRT = m_ColorBuffers[i];

                // CommandBuffer.Blitは名前的にOpenGLのglBlitFramebufferと見間違えてGPUで実行してパフォーマンスがよさそうだが、
                // 実際は同じ動作ではなくてむしろ近いのはCommandBuffer.CopyTextureの方
                commandBuffer.CopyTexture(SrcRT.colorBuffer, DstRT.colorBuffer);
            }
        }

        // デプスバッファコピー
        if (Depth)
        {
            var SrcRT = Src.GetDepthBuffer();
            var DstRT = m_DepthBuffer;

            // CommandBuffer.Blitは名前的にOpenGLのglBlitFramebufferと見間違えてGPUで実行してパフォーマンスがよさそうだが、
            // 実際は同じ動作ではなくてむしろ近いのはCommandBuffer.CopyTextureの方
            commandBuffer.CopyTexture(SrcRT, DstRT);
        }

        // コマンドをコンテキストに登録
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();

        return true;
    }

    public bool CopyColorBuffer(ScriptableRenderContext context, CommandBuffer commandBuffer, CRenderTarget Src)
    {
        // カラーバッファをコピー
        if (Src.GetRenderTargetCount() == m_RenderTargetCount)
        {
            for (int i = 0; i < m_RenderTargetCount; i++)
            {
                var SrcRT = Src.GetColorBuffers()[i];
                var DstRT = m_ColorBuffers[i];

                // CommandBuffer.Blitは名前的にOpenGLのglBlitFramebufferと見間違えてGPUで実行してパフォーマンスがよさそうだが、
                // 実際は同じ動作ではなくてむしろ近いのはCommandBuffer.CopyTextureの方
                commandBuffer.CopyTexture(SrcRT.colorBuffer, DstRT.colorBuffer);
            }
        }

        // コマンドをコンテキストに登録
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();

        return true;
    }

    public bool CopyDepthBuffer(ScriptableRenderContext context, CommandBuffer commandBuffer, CRenderTarget Src)
    {
        var SrcRT = Src.GetDepthBuffer();
        var DstRT = m_DepthBuffer;

        // CommandBuffer.Blitは名前的にOpenGLのglBlitFramebufferと見間違えてGPUで実行してパフォーマンスがよさそうだが、
        // 実際は同じ動作ではなくてむしろ近いのはCommandBuffer.CopyTextureの方
        commandBuffer.CopyTexture(SrcRT, DstRT);

        // コマンドをコンテキストに登録
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();

        return true;
    }
}
