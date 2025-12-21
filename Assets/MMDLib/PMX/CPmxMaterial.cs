using UnityEngine;

namespace mmdlib
{
    public enum EPmxSphereMode
    {
        None = 0, // 無効
		Sph = 1,  // 乗算
		Spa = 2,  // 加算
		SubTexture = 3, // 追加UV1のx,yをUV参照して通常テクスチャ描画を行う
	};

    public class CPmxMaterial
    {
        string m_MaterialName;
        string m_MaterialName_EN;

        Vector4 m_Diffuse;

        Vector4 m_Specular;
        float m_SpecularCoef;

        Vector4 m_Ambient;

        // 描画フラグ(DrawBitFlag)
        bool m_DrawDoubleSlided;
        bool m_DrawGroundShadow;
        bool m_DrawSelfShadowMap;
        bool m_DrawSelfShadow;
        bool m_DrawEdge;

        // エッジカラー
        Vector4 m_EdgeColor;

        // エッジサイズ
        float m_EdgeSize;

        // メインテクスチャの参照インデックス
        int m_MainTexIndex;

        // スフィアテクスチャの参照インデックス
        int m_SphereTexIndex;

        // スフィアモード
        EPmxSphereMode m_SphereMode;

        // トゥーンテクスチャ
        int m_ToonTexIndex;
        int m_SharedToonTexIndex;

        // メモ : 自由欄／スクリプト記述／エフェクトへのパラメータ配置など
        string m_MaterialDescription;

        // 材質に対応する面(頂点)数 (必ず3の倍数になる)
        int m_MatRefIndiceCount;

        public CPmxMaterial(
            string MaterialName,
            string MaterialName_EN,
            Vector4 Diffuse,
            Vector4 Specular,
            float SpecularCoef,
            Vector4 Ambient,
            byte DrawBitFlag,
            Vector4 EdgeColor,
            float EdgeSize,
            int MainTexIndex,
            int SphereTexIndex,
            EPmxSphereMode SphereMode,
            int ToonTexIndex,
            int SharedToonTexIndex,
            string MaterialDescription,
            int MatRefIndiceCount)
        {
            m_MaterialName = MaterialName;
            m_MaterialName_EN = MaterialName_EN;
            m_Diffuse = Diffuse;
            m_Specular = Specular;
            m_SpecularCoef = SpecularCoef;
            m_Ambient = Ambient;
            m_EdgeColor = EdgeColor;
            m_EdgeSize = EdgeSize;
            m_MainTexIndex = MainTexIndex;
            m_SphereTexIndex = SphereTexIndex;
            m_SphereMode = SphereMode;
            m_ToonTexIndex = ToonTexIndex;
            m_SharedToonTexIndex = SharedToonTexIndex;
            m_MaterialDescription = MaterialDescription;
            m_MatRefIndiceCount = MatRefIndiceCount;
            AnalyseDrawBitFlag(DrawBitFlag);
        }

        void AnalyseDrawBitFlag(byte DrawBitFlag)
        {
            m_DrawDoubleSlided = (DrawBitFlag & 0x01) != 0;
            m_DrawGroundShadow = (DrawBitFlag & 0x02) != 0;
            m_DrawSelfShadowMap = (DrawBitFlag & 0x04) != 0;
            m_DrawSelfShadow = (DrawBitFlag & 0x08) != 0;
            m_DrawEdge = (DrawBitFlag & 0x10) != 0;
        }

        public string GetMaterialName()
        {
            return m_MaterialName;
        }

        public string GetMaterialName_EN()
        {
            return m_MaterialName_EN;
        }

        public Vector4 GetDiffuse()
        {
            return m_Diffuse;
        }

        public Vector4 GetSpecular()
        {
            return m_Specular;
        }

        public float GetSpecularCoef()
        {
            return m_SpecularCoef;
        }

        public Vector4 GetAmbient()
        {
            return m_Ambient;
        }

        // 描画フラグ(DrawBitFlag)
        public bool IsDrawDoubleSlided()
        {
            return m_DrawDoubleSlided;
        }

        public bool IsDrawGroundShadow()
        {
            return m_DrawGroundShadow;
        }

        public bool IsDrawSelfShadowMap()
        {
            return m_DrawSelfShadowMap;
        }

        public bool IsDrawSelfShadow()
        {
            return m_DrawSelfShadow;
        }

        public bool IsDrawEdge()
        {
            return m_DrawEdge;
        }

        // エッジカラー
        public Vector4 GetEdgeColor()
        {
            return m_EdgeColor;
        }

        // エッジサイズ
        public float GetEdgeSize()
        {
            return m_EdgeSize;
        }

        // メインテクスチャの参照インデックス
        public int GetMainTexIndex()
        {
            return m_MainTexIndex;
        }

        // スフィアテクスチャの参照インデックス
        public int GetSphereTexIndex()
        {
            return m_SphereTexIndex;
        }

        // スフィアモード
        public EPmxSphereMode GetSphereMode()
        {
            return m_SphereMode;
        }

        // トゥーンテクスチャ
        public int GetToonTexIndex()
        {
            return m_ToonTexIndex;
        }

        public int GetSharedToonTexIndex()
        {
            return m_SharedToonTexIndex;
        }

        // メモ : 自由欄／スクリプト記述／エフェクトへのパラメータ配置など
        public string GetMaterialDescription()
        {
            return m_MaterialDescription;
        }

        // 材質に対応する面(頂点)数 (必ず3の倍数になる)
        public int GetMatRefIndiceCount()
        {
            return m_MatRefIndiceCount;
        }
    }
}