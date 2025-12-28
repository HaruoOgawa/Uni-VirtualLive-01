using binary;
using network.dmx;
using UnityEngine;

namespace camera
{
    public class CameraController : MonoBehaviour, IDMXFixture
    {
        byte m_CurrentCameraEffectID = 0;

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
            const int IDByteSize = 1;
            const int ExpectByteSize = IDByteSize + sizeof(float) * 7;

            if (data.Length != ExpectByteSize) return;

            CBinaryReader Analyser = new CBinaryReader();
            if (!Analyser.Init(data)) return;

            if (!Analyser.IsValid(ExpectByteSize)) return;

            // カメラエフェクトの切り替えを検知する
            byte CameraEffectID = Analyser.GetByte();
            bool CameraChanged = (CameraEffectID != m_CurrentCameraEffectID);
            m_CurrentCameraEffectID = CameraEffectID;

            //
            Vector3 NewWorldPos = new Vector3(
                Analyser.GetFloat(),
                Analyser.GetFloat(),
                Analyser.GetFloat()
            );

            float NewZAngle = Analyser.GetFloat();

            Vector3 NewCenterPos = new Vector3(
                Analyser.GetFloat(),
                Analyser.GetFloat(),
                Analyser.GetFloat()
            );

            if(CameraChanged)
            {
                // カメラが変わった瞬間だけイージングを切る
                m_WorldPos = NewWorldPos;
                m_ZAngle = NewZAngle;
                m_CenterPos = NewCenterPos;
            }
            else
            {
                m_WorldPos = Vector3.Lerp(m_WorldPos, NewWorldPos, 0.1f);
                m_ZAngle = Mathf.Lerp(m_ZAngle, NewZAngle, 0.1f);
                m_CenterPos = Vector3.Lerp(m_CenterPos, NewCenterPos, 0.1f);
            }
        }
    }
}