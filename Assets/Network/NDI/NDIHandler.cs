using UnityEngine;
using System.Runtime.InteropServices;

namespace network.ndi
{
    public class NDIHandler : MonoBehaviour
    {
        CNDIReciever m_NDIReciever = null;

        void Start()
        {
            m_NDIReciever = new CNDIReciever();

            int val = m_NDIReciever.NDITestFunc(3, 5);
            Debug.LogFormat("val: {0}", val);
        }

        private void OnDestroy()
        {
            m_NDIReciever = null;
        }

        void Update()
        {

        }
    }
}

