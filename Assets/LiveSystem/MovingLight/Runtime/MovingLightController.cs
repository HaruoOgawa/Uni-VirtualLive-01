using UnityEngine;
using network.dmx;
using binary;

namespace livesystem
{
    public class MovingLightController : MonoBehaviour, IDMXFixture
    {
        [SerializeField] Transform m_Pan = null;
        [SerializeField] Transform m_Tilt = null;
        [SerializeField] Transform m_Emitter = null;
        [SerializeField] Transform m_LightShaft = null;
        [SerializeField] Light m_Light = null;

        Color m_DMXColor = Color.white;
        float m_DMXDimmer = 1.0f;
        float m_DMXPan = 0.0f;
        float m_DMXTilt = 0.0f;
        float m_DMXAngle = 0.0f;
        float m_DMXHeight = 0.0f;
        float m_DMXMulIntensity = 1.0f;
        float m_DMXZAngle = 0.0f;

        Quaternion m_DefaultPanRotate = Quaternion.identity;
        Quaternion m_DefaultTiltRotate = Quaternion.identity;
        float m_DefaultIntensity = 1.0f;

        // プレファブのインスタンス単位でマテリアルに違う値をセットするためにMaterialPropertyBlockを使用
        MaterialPropertyBlock m_ProperyBlock = null;
        MaterialPropertyBlock m_EmitterProperyBlock = null;

        void Start()
        {
            // 実行時にメモリを確保しないとnull扱いになる
            m_ProperyBlock = new MaterialPropertyBlock();
            m_EmitterProperyBlock = new MaterialPropertyBlock();

            if (m_Pan != null) m_DefaultPanRotate = m_Pan.transform.localRotation;
            if (m_Tilt != null) m_DefaultTiltRotate = m_Tilt.transform.localRotation;
            if (m_Light != null) m_DefaultIntensity = m_Light.intensity;

            // バウンディングボックスのサイズを再計算する
            if(m_LightShaft != null)
            {
                MeshRenderer renderer = m_LightShaft.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    float len = 100.0f;
                    float r = 20.0f;
                    
                    Vector3 StartPos = m_LightShaft.transform.localPosition;
                    Vector3 EndPos = len * Vector3.up + StartPos;

                    Vector3 Center = (EndPos + StartPos) * 0.5f;
                    Vector3 Size = new Vector3(r, len, r);

                    renderer.bounds = new Bounds(Center, Size);
                }
            }
        }

        public void AssignDMXData(byte[] data)
        {
            // 11チャンネルある想定
            const int ExpectByteSize = 11;

            if (data.Length != ExpectByteSize) return;

            CBinaryReader Analyser = new CBinaryReader();
            if (!Analyser.Init(data)) return;

            if (!Analyser.IsValid(ExpectByteSize)) return;

            // 少し古い値を受信して急激に値が変わることがあるのでイージングを入れる
            Color DMXColor = new Color(
                (float)(Analyser.GetByte()) / 255.0f,
                (float)(Analyser.GetByte()) / 255.0f,
                (float)(Analyser.GetByte()) / 255.0f,
                (float)(Analyser.GetByte()) / 255.0f
            );
            m_DMXColor = Color.Lerp(m_DMXColor, DMXColor, 0.5f);

            float NewDMXDimmer = (float)(Analyser.GetByte()) / 255.0f;
            m_DMXTilt = Mathf.Rad2Deg * 2.0f * 3.1415f * (float)(Analyser.GetByte()) / 255.0f;
            m_DMXPan = Mathf.Rad2Deg * 2.0f * 3.1415f * (float)(Analyser.GetByte()) / 255.0f;
            float NewDMXAngle = 180.0f * (float)(Analyser.GetByte()) / 255.0f; // 0 ~ 90度まで
            float NewDMXHeight = 50.0f * (float)(Analyser.GetByte()) / 255.0f; // 50mまで伸びる
            float NewMulIntensity = (float)(Analyser.GetByte()) / 255.0f;
            m_DMXZAngle = Mathf.Rad2Deg * 2.0f * 3.1415f * (float)(Analyser.GetByte()) / 255.0f;

            // 少し古い値を受信して急激に値が変わることがあるのでイージングを入れる
            m_DMXDimmer = Mathf.Lerp(m_DMXDimmer, NewDMXDimmer, 0.1f);
            m_DMXAngle = Mathf.Lerp(m_DMXAngle, NewDMXAngle, 0.1f);
            m_DMXHeight = Mathf.Lerp(NewDMXHeight, NewDMXHeight, 0.1f);
            m_DMXMulIntensity = Mathf.Lerp(m_DMXMulIntensity, NewMulIntensity, 0.1f);
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

            // Emitter
            if (m_Emitter != null)
            {
                Quaternion OldRotate = m_Emitter.transform.localRotation;
                Quaternion NewRotate = Quaternion.AngleAxis(m_DMXZAngle, Vector3.forward);

                m_Emitter.transform.localRotation = Quaternion.Slerp(OldRotate, NewRotate, 0.1f);

                MeshRenderer renderer = m_Emitter.GetComponent<MeshRenderer>();
                if(renderer != null)
                {
                    Material material = renderer.material;
                    if(material != null)
                    {
                        // プロパティブロック取得
                        renderer.GetPropertyBlock(m_EmitterProperyBlock);

                        Color FinalColor = m_DMXColor;
                        if(FinalColor.a > 0.001)
                        {
                            FinalColor.r += m_DMXDimmer;
                            FinalColor.g += m_DMXDimmer;
                            FinalColor.b += m_DMXDimmer;
                        }

                        m_EmitterProperyBlock.SetColor("_EmissiveColor", FinalColor);

                        // プロパティブロック再設定
                        renderer.SetPropertyBlock(m_EmitterProperyBlock);
                    }
                }
            }

            // LightShaft
            if (m_LightShaft != null)
            {
                // ライトシャフトの値を常にライトにも反映する
                MeshRenderer renderer = m_LightShaft.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    Material material = renderer.material;
                    if (material != null)
                    {
                        // プロパティブロック取得
                        renderer.GetPropertyBlock(m_ProperyBlock);

                        m_ProperyBlock.SetColor("_Color", m_DMXColor);
                        m_ProperyBlock.SetFloat("_Dimmer", m_DMXDimmer);
                        m_ProperyBlock.SetFloat("_Height", m_DMXHeight);
                        m_ProperyBlock.SetFloat("_SpotAngle", m_DMXAngle);

                        // プロパティブロック再設定
                        renderer.SetPropertyBlock(m_ProperyBlock);
                    }
                }
            }

            // Light
            if (m_Light != null)
            {
                m_Light.color = m_DMXColor;
                m_Light.intensity = m_DefaultIntensity * m_DMXMulIntensity;
                m_Light.range = m_DMXHeight;
                m_Light.spotAngle = m_DMXAngle;
            }
        }
    }
}