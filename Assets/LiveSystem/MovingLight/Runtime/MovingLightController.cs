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

    // プレファブのインスタンス単位でマテリアルに違う値をセットするためにMaterialPropertyBlockを使用
    MaterialPropertyBlock m_ProperyBlock = null;

    void Start()
    {
        // 実行時にメモリを確保しないとnull扱いになる
        m_ProperyBlock = new MaterialPropertyBlock();

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

        float NewDMXDimmer = (float)(Analyser.GetByte()) / 255.0f;
        m_DMXTilt = Mathf.Rad2Deg * 2.0f * 3.1415f * (float)(Analyser.GetByte()) / 255.0f;
        m_DMXPan = Mathf.Rad2Deg * 2.0f * 3.1415f * (float)(Analyser.GetByte()) / 255.0f;
        float NewDMXAngle = 180.0f * (float)(Analyser.GetByte()) / 255.0f; // 0 ~ 90度まで
        float NewDMXHeight = 50.0f * (float)(Analyser.GetByte()) / 255.0f; // 50mまで伸びる

        // 少し古い値を受信して急激に値が変わることがあるのでイージングを入れる
        m_DMXDimmer = Mathf.Lerp(m_DMXDimmer, NewDMXDimmer, 0.1f);
        m_DMXAngle = Mathf.Lerp(m_DMXAngle, NewDMXAngle, 0.1f);
        m_DMXHeight = Mathf.Lerp(NewDMXHeight, NewDMXHeight, 0.1f);
    }

    void Update()
    {
        // Pan
        if (m_Pan != null)
        {
            Quaternion OldRotate = m_Pan.transform.localRotation;
            Quaternion NewRotate = Quaternion.AngleAxis(m_DMXPan, Vector3.forward) * m_DefaultPanRotate;

            // 少し古い値を受信して急激に値が変わることがあるのでイージングを入れる
            m_Pan.transform.localRotation = Quaternion.Slerp(OldRotate, NewRotate, 0.1f); 
        }

        // Tilt
        if (m_Tilt != null)
        {
            Quaternion OldRotate = m_Tilt.transform.localRotation;
            Quaternion NewRotate = Quaternion.AngleAxis(m_DMXTilt, Vector3.right) * m_DefaultTiltRotate;

            // 少し古い値を受信して急激に値が変わることがあるのでイージングを入れる
            m_Tilt.transform.localRotation = Quaternion.Slerp(OldRotate, NewRotate, 0.1f);
        }

        if (m_LightShaft == null) return;

        // ライトシャフトの値を常にライトにも反映する
        MeshRenderer renderer = m_LightShaft.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        Material material = renderer.material;
        if(material == null) return;

        {
            // プロパティブロック取得
            renderer.GetPropertyBlock(m_ProperyBlock);

            m_ProperyBlock.SetColor("_Color", m_DMXColor);
            m_ProperyBlock.SetFloat("_Intensity", m_DMXDimmer);
            m_ProperyBlock.SetFloat("_Height", m_DMXHeight);
            m_ProperyBlock.SetFloat("_SpotAngle", m_DMXAngle);

            // プロパティブロック再設定
            renderer.SetPropertyBlock(m_ProperyBlock);
        }

        if(m_Light != null)
        {
            m_Light.color = m_DMXColor;
            m_Light.intensity = m_DMXDimmer;
            m_Light.range = m_DMXHeight;
            m_Light.spotAngle = m_DMXAngle;
        }
    }
}
