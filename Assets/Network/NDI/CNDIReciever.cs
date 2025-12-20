using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace network.ndi
{
    public  class CNDIReciever
    {
        // C++é¿ëï
        // åƒÇ—èoÇµãKñÒÇC#Ç∆C++ë§Ç≈ñæé¶ìIÇ…çáÇÌÇπÇ»Ç¢Ç∆Ç§Ç‹Ç≠ìÆçÏÇµÇ»Ç¢
        [DllImport("Haru86_NDILib", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CNDIReceiver_Constructor();

        [DllImport("Haru86_NDILib", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CNDIReceiver_Destructor(IntPtr pObj);

        [DllImport("Haru86_NDILib", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CNDIReceiver_NDITestFunc(IntPtr pObj, int a, int b);

        // C#é¿ëï
        IntPtr m_pObj = IntPtr.Zero;
        public CNDIReciever()
        {
            m_pObj = CNDIReceiver_Constructor();
        }

        ~CNDIReciever()
        {
            CNDIReceiver_Destructor(m_pObj);
            m_pObj = IntPtr.Zero;
        }

        public int NDITestFunc(int a, int b)
        {
            return CNDIReceiver_NDITestFunc(m_pObj, a, b);
        }
    }
}