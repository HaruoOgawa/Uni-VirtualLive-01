using UnityEngine;
using System.Collections.Generic;

namespace network.dmx
{
    public class DMXDataHandler : MonoBehaviour
    {
        [SerializeField] List<DMXFixtureGroup> m_FixtureGroupList = new List<DMXFixtureGroup>();

        Dictionary<(ushort Net, ushort SubNet, ushort Universe), DMXFixtureGroup> m_UniverseGroupMap = 
            new Dictionary<(ushort Net, ushort SubNet, ushort Universe), DMXFixtureGroup>();

        public DMXDataHandler()
        {
        }

        private void OnDestroy()
        {
            // イベント購読解除
            UDPSocket.OnReceivedDMX -= OnReceivedDMX;

            m_UniverseGroupMap.Clear();
        }

        private void Start()
        {
            // イベント購読
            UDPSocket.OnReceivedDMX += OnReceivedDMX;

            Init();
        }

        void Init()
        {
            // DMXのユニバース番号とFixtureGroupのペアを作成
            foreach (var group in m_FixtureGroupList)
            {
                m_UniverseGroupMap.Add((group.Net, group.SubNet, group.Universe), group);
            }
        }

        void OnReceivedDMX(ushort Net, ushort SubNet, ushort Universe, byte[] DataBuffer)
        {
            // 該当ユニバースのグループが登録されていれば値を渡す
            DMXFixtureGroup fixtureGroup = null;
            if (!m_UniverseGroupMap.TryGetValue((Net, SubNet, Universe), out fixtureGroup)) return;

            if (fixtureGroup == null) return;

            fixtureGroup.DispatchDMXData(DataBuffer);
        }
    }
}