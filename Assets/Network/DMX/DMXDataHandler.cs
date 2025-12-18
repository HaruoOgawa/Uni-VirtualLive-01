using UnityEngine;

namespace network.dmx
{
    public class DMXDataHandler : MonoBehaviour
    {
        public DMXDataHandler()
        {
            // イベント購読
            UDPSocket.OnReceivedDMX += OnReceivedDMX;
        }

        private void OnDestroy()
        {
            // イベント購読解除
            UDPSocket.OnReceivedDMX -= OnReceivedDMX;
        }

        void OnReceivedDMX(ushort Net, ushort SubNet, ushort Universe, byte[] DataBuffer)
        {
            Debug.LogFormat("[DMXDataHandler.OnReceivedDMX] Net: {0}, SubNet: {1}, Universe: {2}, DataBuffer.Length: {3}", 
                Net, SubNet, Universe, DataBuffer.Length);
        }

        void Start()
        {

        }

        void Update()
        {

        }
    }
}