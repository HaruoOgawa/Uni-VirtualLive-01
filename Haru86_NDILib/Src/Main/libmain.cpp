#ifdef DLL_APP

#include <stdio.h>
#include "../NDI/CNDIReceiver.h"

#ifdef _WIN32
	#ifdef __cplusplus
		#define DLLEXPORT extern "C" __declspec(dllexport) 
	#else
		#define DLLEXPORT __declspec(dllexport) 
	#endif // __cplusplus
#else
		#define extern "C"
#endif // _WIN32

// <呼び出し規約> 関数と呼び出し元の間で、引数や戻り値の受け渡し方法を決める
// https://learn.microsoft.com/ja-jp/cpp/cpp/argument-passing-and-naming-conventions?view=msvc-170
#ifdef _WIN32
// __cdecl : 呼び出し元が引数のスタックを片付ける
#define CALLING_WAY __cdecl
// __stdcall : 呼び出し先が引数のスタックを片付ける
//#define CALLING_WAY __stdcall
#else
// Mac・Linuxの時は何もつけない
#define CALLING_WAY
#endif // _WIN32

DLLEXPORT void* CALLING_WAY CNDIReceiver_Constructor()
{
	return (void*) new network::CNDIReceiver();
}

DLLEXPORT void CALLING_WAY CNDIReceiver_Destructor(void* pObj)
{
	delete pObj;
}

DLLEXPORT bool CALLING_WAY CNDIReceiver_Initialize(void* pObj)
{
	network::CNDIReceiver* NDIReceiver = (network::CNDIReceiver*)pObj;
	if (!NDIReceiver) return false;

	return NDIReceiver->Initialize();
}

DLLEXPORT bool CALLING_WAY CNDIReceiver_FetchPixelData(void* pObj, void*& pPixelData, int& PixelByteSize, int& TextureWidth, int& TextureHeight)
{
	network::CNDIReceiver* NDIReceiver = (network::CNDIReceiver*)pObj;
	if (!NDIReceiver) return false;

	return NDIReceiver->FetchPixelData(pPixelData, PixelByteSize, TextureWidth, TextureHeight);
}

#endif