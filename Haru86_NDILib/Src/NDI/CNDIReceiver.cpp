#include "CNDIReceiver.h"
#include <algorithm>

#include <Processing.NDI.Lib.h>
//#include <Processing.NDI.Recv.h>
//#include <Processing.NDI.Lib.cplusplus.h>

namespace network
{
	CNDIReceiver::CNDIReceiver() :
		m_NDI_find(nullptr),
		m_NDI_recv(nullptr),
		m_Connected(false)
	{
	}

	CNDIReceiver::~CNDIReceiver()
	{
		NDIlib_find_destroy(m_NDI_find);
		NDIlib_recv_destroy(m_NDI_recv);
	}

	bool CNDIReceiver::Initialize()
	{
		// NDIを配信しているソースを探す ////////////////////////////////////////////
		// ファインダーを生成 
		m_NDI_find = NDIlib_find_create_v2(nullptr);
		if (!m_NDI_find) return true;

		uint32_t no_sources = 0;
		NDIlib_find_wait_for_sources(m_NDI_find, 1000);
		const NDIlib_source_t* pSources = NDIlib_find_get_current_sources(m_NDI_find, &no_sources);

		if (no_sources == 0) return true;


		// NDIの受信準備 ///////////////////////////////////////////////////////////
		// レシーバーを作成
		m_NDI_recv = NDIlib_recv_create_v3(nullptr); // 引数には詳細設定を渡す
		if (!m_NDI_recv) return true;

		// ネットワーク接続
		NDIlib_recv_connect(m_NDI_recv, &pSources[0]); // ひとまず最初のソースを使う(必要になれば名前とかポートとかを見て判断する)

		// 接続完了
		m_Connected = true;

		return true;
	}

	bool CNDIReceiver::FetchPixelData(void*& pPixelData, int& PixelByteSize, int& TextureWidth, int& TextureHeight)
	{
		if (!m_Connected || !m_NDI_recv) return true;

		NDIlib_video_frame_v2_t videoFrame;
		NDIlib_frame_type_e frameType = NDIlib_recv_capture_v2(m_NDI_recv, &videoFrame, nullptr, nullptr, 0);

		int Width = videoFrame.xres;
		int Height = videoFrame.yres;

		if (frameType == NDIlib_frame_type_video)
		{
			// 変換処理が重いのでBGRAしか見ない
			// TouchDesignerでUYVYではなくBGRAを受け取るにはAlphaチャンネルを含める必要がある
			if (videoFrame.FourCC == NDIlib_FourCC_video_type_BGRA)
			{
				int ByteSize = videoFrame.xres * videoFrame.yres * 4;

				pPixelData = new unsigned char[ByteSize];
				PixelByteSize = ByteSize;
				TextureWidth = Width;
				TextureHeight = Height;

				std::memcpy(pPixelData, videoFrame.p_data, ByteSize);
			}
			else
			{
				return false;
			}
		}
		else
		{
			return false;
		}

		NDIlib_recv_free_video_v2(m_NDI_recv, &videoFrame);

		if (PixelByteSize == 0) return false;

		return true;
	}
}