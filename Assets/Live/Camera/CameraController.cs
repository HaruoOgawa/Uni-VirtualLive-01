using binary;
using network.dmx;
using UnityEngine;

namespace camera
{
    public class CameraController : MonoBehaviour, IDMXFixture
    {
        Vector3 m_WorldPos = Vector3.zero;
        Vector3 m_CenterPos = Vector3.zero;
        float m_ZAngle = 0.0f;

        void Start()
        {

        }

        void Update()
        {
            if(Vector3.Distance(m_WorldPos, m_CenterPos) != 0.0f)
            {
                Camera.main.transform.position = m_WorldPos;

                Vector3 Up = Quaternion.AngleAxis(m_ZAngle, Vector3.forward) * Vector3.up;
                Camera.main.transform.LookAt(m_CenterPos, Up);
            }
        }

        public void AssignDMXData(byte[] data)
        {
            const int ExpectByteSize = sizeof(float) * 7;

            if (data.Length != ExpectByteSize) return;

            CBinaryReader Analyser = new CBinaryReader();
            if (!Analyser.Init(data)) return;

            if (!Analyser.IsValid(ExpectByteSize)) return;

            Vector3 NewWorldPos = new Vector3(
                Analyser.GetFloat(),
                Analyser.GetFloat(),
                Analyser.GetFloat()
            );
            
            m_WorldPos = Vector3.Lerp(m_WorldPos, NewWorldPos, 0.1f);

            m_ZAngle = Mathf.Lerp(m_ZAngle, Analyser.GetFloat(), 0.1f);

            Vector3 NewCenterPos = new Vector3(
                Analyser.GetFloat(),
                Analyser.GetFloat(),
                Analyser.GetFloat()
            );

            m_CenterPos = Vector3.Lerp(m_CenterPos, NewCenterPos, 0.1f);
        }
    }
}