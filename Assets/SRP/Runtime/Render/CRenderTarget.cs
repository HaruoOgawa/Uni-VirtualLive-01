using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace srp.render
{
    public class CRenderTarget : IDisposable
    {
        int m_Width = 0;
        int m_Height = 0;
        int m_RenderTargetCount = 0;
        RenderTextureFormat m_ColorFormat;
        RenderTextureFormat m_DepthFormat;
        int m_DepthBit;

        List<RenderTexture> m_ColorBuffers = new List<RenderTexture>();
        List<RenderTargetIdentifier> m_ColorRTIdentifiers = new List<RenderTargetIdentifier>();

        RenderTexture m_DepthBuffer = null;
        RenderTargetIdentifier m_DepthRTIdentifier;

        public CRenderTarget()
        {
        }

        // Disposeパターンについて
        // C#においてデストラクタはdeleteで自分でオブジェクトを破棄したときに呼ばれるメソッドではあるが、
        // GCで自動解放されたときは呼ばれない
        // デストラクタによる解放とGCによる解放の両方を検知するのがDisposeパターン
        // `public void Dispose()`がGCによって呼ばれる？
        // https://learn.microsoft.com/ja-jp/dotnet/standard/garbage-collection/implementing-dispose
        // https://qiita.com/tera1707/items/1b41ae8f38884656b9fb
        ~CRenderTarget()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            //Debug.LogFormat("Dispose => disposing: {0}", disposing);

            if (disposing)
            {
            }
            else
            {
                Release();
            }
        }

        public void Release()
        {
            if (m_ColorBuffers.Count > 0)
            {
                foreach(var buffer in m_ColorBuffers)
                {
                    if(buffer == null) continue;

                    buffer.Release();
                }

                m_ColorBuffers.Clear();
            }

            m_ColorBuffers.Clear();
            m_ColorRTIdentifiers.Clear();

            if (m_DepthBuffer != null)
            {
                m_DepthBuffer.Release();
                m_DepthBuffer = null;
            }
            //m_DepthRTIdentifier = null;
        }

        public bool IsValid()
        {
            if(m_RenderTargetCount > 0)
            {
                foreach(var rt in m_ColorBuffers)
                {
                    if (rt == null) return false;
                }
            }

            if(m_DepthBuffer == null) return false;

            return true;
        }

        public bool RecreateIfInValid()
        {
            // もし資材がUnityネイティブ側に自動破棄されていて無効な状態になっていたら再生成する
            if(!IsValid())
            {
                Release();
                if (!Create(m_Width, m_Height, m_RenderTargetCount, m_ColorFormat, m_DepthFormat, m_DepthBit)) return false;
            }

            return true;
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

                if (!rt.Create())
                {
                    Debug.LogError("[CRenderTarget] Failed to create color render target.");
                    return false;
                }

                m_ColorBuffers.Add(rt);
                m_ColorRTIdentifiers.Add(rt);
            }

            // デプスバッファ作成
            {
                RenderTexture rt = new RenderTexture(width, height, DepthBit, DepthFormat);

                if (!rt.Create())
                {
                    Debug.LogError("[CRenderTarget] Failed to create depth render target.");
                    return false;
                }

                m_DepthBuffer = rt;
                m_DepthRTIdentifier = rt;
            }

            m_Width = width;
            m_Height = height;
            m_RenderTargetCount = RenderTargetCount;
            m_ColorFormat = ColorFormat;
            m_DepthFormat = DepthFormat;
            m_DepthBit = DepthBit;

            return true;
        }

        public bool CopyFrameBuffer(ScriptableRenderContext context, CommandBuffer commandBuffer, CRenderTarget Src)
        {
            // カラーバッファをコピー
            if (Src.GetRenderTargetCount() == m_RenderTargetCount)
            {
                for(int i = 0; i < m_RenderTargetCount; i++)
                {
                    var SrcRT = Src.GetColorBuffers()[i];
                    var DstRT = m_ColorBuffers[i];

                    // まだレンダーテクスチャが生成されていない
                    if (!SrcRT.IsCreated() || !DstRT.IsCreated()) return false;

                    // CommandBuffer.Blitは名前的にOpenGLのglBlitFramebufferと見間違えてGPUで実行してパフォーマンスがよさそうだが、
                    // 実際は同じ動作ではなくてむしろ近いのはCommandBuffer.CopyTextureの方
                    commandBuffer.CopyTexture(SrcRT.colorBuffer, DstRT.colorBuffer);
                }
            }

            // デプスバッファコピー
            {
                var SrcRT = Src.GetDepthBuffer();
                var DstRT = m_DepthBuffer;

                // まだレンダーテクスチャが生成されていない
                if (!SrcRT.IsCreated() || !DstRT.IsCreated()) return false;

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

                    // まだレンダーテクスチャが生成されていない
                    if (!SrcRT.IsCreated() || !DstRT.IsCreated()) return false;

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

            if(SrcRT == null || DstRT == null) return false;

            // まだレンダーテクスチャが生成されていない
            if (!SrcRT.IsCreated() || !DstRT.IsCreated()) return false;

            // CommandBuffer.Blitは名前的にOpenGLのglBlitFramebufferと見間違えてGPUで実行してパフォーマンスがよさそうだが、
            // 実際は同じ動作ではなくてむしろ近いのはCommandBuffer.CopyTextureの方
            commandBuffer.CopyTexture(SrcRT, DstRT);

            // コマンドをコンテキストに登録
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            return true;
        }
    }
}