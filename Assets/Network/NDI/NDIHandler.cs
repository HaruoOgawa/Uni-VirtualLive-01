using UnityEngine;
using System.Runtime.InteropServices;

namespace network.ndi
{
    public unsafe class NDIHandler : MonoBehaviour
    {
        [SerializeField] RenderTexture m_NDITexture = null;

        CNDIReciever m_NDIReciever = null;

        void Start()
        {
            m_NDIReciever = new CNDIReciever();
            m_NDIReciever.Initialize();

            // レンダーテクスチャをNDI向けに最適化
            if (m_NDITexture != null) m_NDIReciever.ValidateRenderTexture(m_NDITexture, 256, 256);
        }

        private void OnDestroy()
        {
            m_NDIReciever.Release();
            m_NDIReciever = null;
        }

        void Update()
        {
            if(m_NDIReciever != null)
            {
                m_NDIReciever.FetchPixelData(m_NDITexture);
            }
        }
    }
}

