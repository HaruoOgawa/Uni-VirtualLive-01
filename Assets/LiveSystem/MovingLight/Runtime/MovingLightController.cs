using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using network.dmx;
using binary;

public class MovingLightController : MonoBehaviour, IDMXFixture
{
    [SerializeField] Transform m_Pan = null;
    [SerializeField] Transform m_Tilt = null;
    [SerializeField] Transform m_Emitter = null;
    [SerializeField] Transform m_LightShaft = null;
    [SerializeField] Light m_Light = null;

    Color m_DMXColor = Color.white;
    float m_DMXDimmer = 0.0f;
    float m_DMXPan = 0.0f;
    float m_DMXTilt = 0.0f;
    float m_DMXAngle = 0.0f;
    float m_DMXHeight = 0.0f;

    Quaternion m_DefaultPanRotate = Quaternion.identity;
    Quaternion m_DefaultTiltRotate = Quaternion.identity;

    void Start()
    {
        if (m_Pan != null) m_DefaultPanRotate = m_Pan.transform.localRotation;
        if (m_Tilt != null) m_DefaultTiltRotate = m_Tilt.transform.localRotation;
    }

    public void AssignDMXData(byte[] data)
    {
        // 9チャンネルある想定
        const int ExpectByteSize = 9;

        if (data.Length != ExpectByteSize) return;

        CBinaryReader Analyser = new CBinaryReader();
        if (!Analyser.Init(data)) return;

        if (!Analyser.IsValid(ExpectByteSize)) return;

        m_DMXColor = new Color(
            (float)(Analyser.GetByte()) / 255.0f,
            (float)(Analyser.GetByte()) / 255.0f,
            (float)(Analyser.GetByte()) / 255.0f,
            (float)(Analyser.GetByte()) / 255.0f
        );

        m_DMXDimmer = (float)(Analyser.GetByte()) / 255.0f;
        m_DMXTilt = Mathf.Rad2Deg * 2.0f * 3.1415f * (float)(Analyser.GetByte()) / 255.0f;
        m_DMXPan = Mathf.Rad2Deg * 2.0f * 3.1415f * (float)(Analyser.GetByte()) / 255.0f;
        m_DMXAngle = 90.0f * (float)(Analyser.GetByte()) / 255.0f; // 0 ~ 90度まで
        m_DMXHeight = 50.0f * (float)(Analyser.GetByte()) / 255.0f; // 50mまで伸びる
    }

    void Update()
    {
        // Pan
        if (m_Pan != null)
        {
            Quaternion OldRotate = m_Pan.transform.localRotation;
            Quaternion NewRotate = Quaternion.AngleAxis(m_DMXPan, Vector3.forward) * m_DefaultPanRotate;

            // イージング
            m_Pan.transform.localRotation = Quaternion.Slerp(OldRotate, NewRotate, 0.1f); 
        }

        // Tilt
        if (m_Tilt != null)
        {
            Quaternion OldRotate = m_Tilt.transform.localRotation;
            Quaternion NewRotate = Quaternion.AngleAxis(m_DMXTilt, Vector3.right) * m_DefaultTiltRotate;

            // イージング
            m_Tilt.transform.localRotation = Quaternion.Slerp(OldRotate, NewRotate, 0.1f);
        }

        if (m_Light == null || m_LightShaft == null) return;

        // ライトシャフトの値を常にライトにも反映する
        MeshRenderer renderer = m_LightShaft.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        Material material = renderer.material;
        if(material == null) return;

        Color _Color = material.GetColor("_Color");
        float _Intensity = material.GetFloat("_Intensity");
        float _Height = material.GetFloat("_Height");
        float _SpotAngle = material.GetFloat("_SpotAngle");

        m_Light.color = _Color;
        m_Light.intensity = _Intensity;
        m_Light.range = _Height;
        m_Light.spotAngle = _SpotAngle;
    }
}
