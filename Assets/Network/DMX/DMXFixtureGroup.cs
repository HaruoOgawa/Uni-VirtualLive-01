using System;
using System.Collections.Generic;
using UnityEngine;

namespace network.dmx
{
    public class DMXFixtureGroup : MonoBehaviour
    {
        [SerializeField] ushort m_Net = 0;
        [SerializeField] ushort m_SubNet = 0;
        [SerializeField] ushort m_Universe = 0;

        [SerializeField] List<string> m_Channels = new List<string>();
        [SerializeField] List<GameObject> m_Devices = new List<GameObject>();

        public ushort Net { get { return m_Net; } }
        public ushort SubNet { get { return m_SubNet; } }
        public ushort Universe { get { return m_Universe; } }

        List<IDMXFixture> m_DMXFixtures = new List<IDMXFixture>();

        float m_CurrentTimeCode = 0.0f;

        private void Start()
        {
            // Unityの仕様でメインスレッド以外ではGetComponentできないのでStartで事前取得しておく
            foreach (GameObject device in m_Devices)
            {
                IDMXFixture fixture = device.GetComponent<IDMXFixture>();
                if (fixture == null) continue;

                m_DMXFixtures.Add(fixture);
            }
        }

        public void DispatchDMXData(byte[] DataBuffer)
        {
            int TimeCodeByte = 4;
            int ByteSizePerDevice = m_Channels.Count; // 各チャンネルを1バイトで計算する
            int TotalByteSize = ByteSizePerDevice * m_Devices.Count + TimeCodeByte;

            if (TotalByteSize == 0 || TotalByteSize > DataBuffer.Length) return;

            byte Frame = DataBuffer[0];
            byte Second = DataBuffer[1];
            byte Minute = DataBuffer[2];
            byte Hour = DataBuffer[3];

            // タイムコードをチェックして古いデータは捨てる
            float TimeCode = ((float)Hour) * 60.0f * 60.0f + ((float)Minute) * 60.0f + ((float)Second) + ((float)Frame) / 30.0f;
            if (TimeCode < m_CurrentTimeCode) return;
            m_CurrentTimeCode = TimeCode;

            // 各デバイスにバイト列を分けて送信
            int ByteOffset = 0;

            // タイムコード分だけ飛ばす
            ByteOffset += TimeCodeByte;
            
            foreach (IDMXFixture fixture in m_DMXFixtures)
            {
                if(fixture == null) continue;

                // データコピー
                byte[] data = new byte[ByteSizePerDevice];
                Array.Copy(DataBuffer, ByteOffset, data, 0, ByteSizePerDevice);

                // 各デバイスにデータを渡してその中でパース
                fixture.AssignDMXData(data);

                // オフセット更新
                ByteOffset += ByteSizePerDevice;
            }
        }
    }
}