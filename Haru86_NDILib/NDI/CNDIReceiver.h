#pragma once

namespace network
{
	class CNDIReceiver
	{
	public:
		CNDIReceiver();
		virtual ~CNDIReceiver();

		int NDITestFunc(int a, int b);
	};
}