using UnityEngine;

namespace network.dmx
{
    public interface IDMXFixture
    {
        public void AssignDMXData(byte[] data);
    }
}