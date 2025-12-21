#pragma once

#include <vector>

struct NDIlib_find_instance_type;
struct NDIlib_recv_instance_type;

namespace network
{
	class CNDIReceiver
	{
		NDIlib_find_instance_type* m_NDI_find;
		NDIlib_recv_instance_type* m_NDI_recv;

		bool m_Connected;

	private:

	public:
		CNDIReceiver();
		virtual ~CNDIReceiver();

		bool Initialize();

		bool FetchPixelData(void*& pPixelData, int& PixelByteSize, int& TextureWidth, int& TextureHeight);
	};
}