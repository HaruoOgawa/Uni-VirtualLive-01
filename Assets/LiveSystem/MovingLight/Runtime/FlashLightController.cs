using UnityEngine;
using network.dmx;
using binary;

namespace livesystem
{
    public class FlashLightController : MonoBehaviour, IDMXFixture
    {
        Color m_DMXColor = Color.black;
        float m_DMXDimmer = 0.0f;

        // プレファブのインスタンス単位でマテリアルに違う値をセットするためにMaterialPropertyBlockを使用
        MaterialPropertyBlock m_ProperyBlock = null;

        void Start()
        {
            // 実行時にメモリを確保しないとnull扱いになる
            m_ProperyBlock = new MaterialPropertyBlock();
        }

        public void AssignDMXData(byte[] data)
        {
            // 5チャンネルある想定
            const int ExpectByteSize = 5;

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
            m_DMXColor = Color.Lerp(m_DMXColor, DMXColor, 0.1f);

            // 少し古い値を受信して急激に値が変わることがあるのでイージングを入れる
            float NewDMXDimmer = 2.0f * (float)(Analyser.GetByte()) / 255.0f;
            m_DMXDimmer = Mathf.Lerp(m_DMXDimmer, NewDMXDimmer, 0.1f);
        }

        void Update()
        {
            var meshRenderer = this.gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) return;

            Color FinalColor = m_DMXColor;
            FinalColor.r += m_DMXDimmer;
            FinalColor.g += m_DMXDimmer;
            FinalColor.b += m_DMXDimmer;

            //Debug.LogFormat("[{1}] FinalColor: {0}", FinalColor, this.gameObject.transform.parent.parent.name);

            for (int m = 0; m < meshRenderer.sharedMaterials.Length; m++)
            {
                var material = meshRenderer.sharedMaterials[m];

                if (material == null) continue;

                if (material.name == "Flash_Emit")
                {
                    meshRenderer.GetPropertyBlock(m_ProperyBlock, m);
                    m_ProperyBlock.SetColor("_EmissiveColor", FinalColor);
                    meshRenderer.SetPropertyBlock(m_ProperyBlock, m);
                }
            }
        }
    }

}