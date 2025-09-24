using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CRenderTarget
{
    int m_Width = 0;
    int m_Height = 0;

    List<RenderTargetIdentifier> m_ColorBuffers = new List<RenderTargetIdentifier>();

    RenderTargetIdentifier m_DepthBuffer;

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

    public List<RenderTargetIdentifier> GetColorBuffers()
    {
        return m_ColorBuffers;
    }
    
    public RenderTargetIdentifier GetDepthBuffer()
    {
        return m_DepthBuffer;
    }

    public bool Create(int width, int height, int RenderTargetCount, RenderTextureFormat ColorFormat, 
        RenderTextureFormat DepthFormat, int DepthBit)
    {
        // カラーバッファ作成
        for (int i = 0; i < RenderTargetCount; i++)
        {
            RenderTexture rt = new RenderTexture(width, height, DepthBit, ColorFormat);

            m_ColorBuffers.Add(rt);
        }

        // デプスバッファ作成
        {
            RenderTexture rt = new RenderTexture(width, height, DepthBit, DepthFormat);
            m_DepthBuffer = rt;
        }

        m_Width = width;
        m_Height = height;

        return true;
    }
}
