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

    void Start()
    {
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
        m_DMXPan = 2.0f * 3.1415f * (float)(Analyser.GetByte()) / 255.0f;
        m_DMXTilt = 2.0f * 3.1415f * (float)(Analyser.GetByte()) / 255.0f;
        m_DMXAngle = 90.0f * (float)(Analyser.GetByte()) / 255.0f;
        m_DMXHeight = 50.0f * (float)(Analyser.GetByte()) / 255.0f;
    }

    void Update()
    {
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
