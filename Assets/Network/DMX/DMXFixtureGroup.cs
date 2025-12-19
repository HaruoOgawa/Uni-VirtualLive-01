using System;
using System.Collections.Generic;
using UnityEngine;

public class DMXFixtureGroup : MonoBehaviour
{
    [SerializeField] ushort m_Net = 0;
    [SerializeField] ushort m_SubNet = 0;
    [SerializeField] ushort m_Universe = 0;

    [SerializeField] List<string> m_Channels = new List<string>();
    [SerializeField] List<DMXFixture> m_Devices = new List<DMXFixture>();

    public ushort Net {  get { return m_Net; } }
    public ushort SubNet {  get { return m_SubNet; } }
    public ushort Universe {  get { return m_Universe; } }

    public void DispatchDMXData(byte[] DataBuffer)
    {
        int PerDeviceByteSize = sizeof(float) * m_Channels.Count;
        int TotalByteSize = PerDeviceByteSize * m_Devices.Count;
        if (TotalByteSize == 0 || TotalByteSize > DataBuffer.Length) return;

        // 各デバイスにバイト列を分けて送信
        int ByteOffset = 0;
        foreach (DMXFixture fixture in m_Devices)
        {
            // データコピー
            byte[] data = new byte[PerDeviceByteSize];
            Array.Copy(DataBuffer, ByteOffset, data, 0, PerDeviceByteSize);

            // 各デバイスにデータを渡してその中でパース
            fixture.AssignDMXData(data);

            // オフセット更新
            ByteOffset += PerDeviceByteSize;
        }
    }
}
