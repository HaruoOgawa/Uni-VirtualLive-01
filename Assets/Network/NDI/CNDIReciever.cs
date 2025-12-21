using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace network.ndi
{
    public unsafe class CNDIReciever
    {
        // C++実装
        // 呼び出し規約をC#とC++側で明示的に合わせないとうまく動作しない
        [DllImport("Haru86_NDILib", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CNDIReceiver_Constructor();

        [DllImport("Haru86_NDILib", CallingConvention = CallingConvention.Cdecl)]
        private static extern void CNDIReceiver_Destructor(IntPtr pObj);

        [DllImport("Haru86_NDILib", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool CNDIReceiver_Initialize(IntPtr pObj);

        [DllImport("Haru86_NDILib", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool CNDIReceiver_FetchPixelData(IntPtr pObj, ref IntPtr pPixelData, ref int PixelByteSize,
            ref int TextureWidth, ref int TextureHeight);

        // C#実装
        IntPtr m_pObj = IntPtr.Zero;
        Texture2D m_StoredTexture;

        public CNDIReciever()
        {
            m_pObj = CNDIReceiver_Constructor();

            // BGRAしか想定していない
            m_StoredTexture = new Texture2D(1, 1, TextureFormat.BGRA32, false);
        }

        public void Release()
        {
            CNDIReceiver_Destructor(m_pObj);
            m_pObj = IntPtr.Zero;
        }

        public bool Initialize()
        {
            return CNDIReceiver_Initialize(m_pObj);
        }

        public bool FetchPixelData(RenderTexture renderTexture)
        {
            if (renderTexture == null) return false;

            IntPtr pPixelData = IntPtr.Zero;
            int PixelByteSize = 0;
            int TextureWidth = 0;
            int TextureHeight = 0;

            bool result = CNDIReceiver_FetchPixelData(m_pObj, ref pPixelData, ref PixelByteSize, ref TextureWidth, ref TextureHeight);

            if(result && PixelByteSize != 0 && TextureWidth != 0 && TextureHeight != 0)
            {
                // レンダーテクスチャのフォーマットが想定外のものであれば再設定する
                if(renderTexture.width != TextureWidth || renderTexture.height != TextureHeight)
                {
                    ValidateRenderTexture(renderTexture, TextureWidth, TextureHeight);
                }

                // サイズが違えば変更する
                if (m_StoredTexture.width != TextureWidth || m_StoredTexture.height != TextureHeight)
                {
                    m_StoredTexture.Reinitialize(TextureWidth, TextureHeight);
                }

                // ピクセルデータを準備
                NativeArray<byte> PixelArray = new NativeArray<byte>(PixelByteSize, Allocator.Temp);
                UnsafeUtility.MemCpy(PixelArray.GetUnsafePtr<byte>(), pPixelData.ToPointer(), PixelByteSize);

                // テクスチャにコピー
                m_StoredTexture.SetPixelData(PixelArray, 0);
                m_StoredTexture.Apply();

                // Texture2DをRenderTextureにコピー
                Graphics.Blit(m_StoredTexture, renderTexture);
            }

            return result;
        }

        public void ValidateRenderTexture(RenderTexture renderTexture, int width, int heigth)
        {
            if (renderTexture == null) return;

            // いったん解放
            renderTexture.Release();

            // レンダーテクスチャをNDIに適切な形式に変更
            renderTexture.format = RenderTextureFormat.BGRA32;
            renderTexture.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_SRGB;
            renderTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            renderTexture.stencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;

            renderTexture.width = width;
            renderTexture.height = heigth;

            // 再生成
            renderTexture.Create();
        }
    }
}