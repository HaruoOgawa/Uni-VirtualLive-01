using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class MovingLightController : MonoBehaviour
{
    public Material LightMaterial = null;

    [SerializeField] Transform m_Pan = null;
    [SerializeField] Transform m_Tilt = null;
    [SerializeField] Transform m_Emitter = null;
    [SerializeField] Transform m_LightShaft = null;
    [SerializeField] Light m_Light = null;

    void Start()
    {
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
