using UnityEngine;
using System.Collections.Generic;

namespace mmdlib
{
    public class CPmxMesh
    {
        private readonly List<float> m_PositionAttribute;
        private readonly List<float> m_NormalAttribute;
        private readonly List<float> m_UVAttribute;
        private readonly List<float> m_TangentAttribute;
        private readonly List<uint> m_UIntBoneAttribute;
        private readonly List<byte> m_ByteBoneAttribute;
        private readonly List<ushort> m_UShortBoneAttribute;
        private readonly List<float> m_WeightAttribute;
        private readonly List<List<float>> m_AdditionalUVAttribute;
        private readonly List<uint> m_UIntIndices;
        private readonly List<byte> m_ByteIndices;
        private readonly List<ushort> m_UShortIndices;

        public CPmxMesh(
            List<float> positionAttribute,
            List<float> normalAttribute,
            List<float> uvAttribute,
            List<float> tangentAttribute,
            List<uint> uintBoneAttribute,
            List<byte> byteBoneAttribute,
            List<ushort> uShortBoneAttribute,
            List<float> weightAttribute,
            List<List<float>> additionalUVAttribute,
            List<uint> uintIndices,
            List<byte> byteIndices,
            List<ushort> uShortIndices)
        {
            m_PositionAttribute = positionAttribute;
            m_NormalAttribute = normalAttribute;
            m_UVAttribute = uvAttribute;
            m_TangentAttribute = tangentAttribute;
            m_UIntBoneAttribute = uintBoneAttribute;
            m_ByteBoneAttribute = byteBoneAttribute;
            m_UShortBoneAttribute = uShortBoneAttribute;
            m_WeightAttribute = weightAttribute;
            m_AdditionalUVAttribute = additionalUVAttribute;
            m_UIntIndices = uintIndices;
            m_ByteIndices = byteIndices;
            m_UShortIndices = uShortIndices;
        }

        public IReadOnlyList<float> GetPositionAttribute() => m_PositionAttribute;
        public IReadOnlyList<float> GetNormalAttribute() => m_NormalAttribute;
        public IReadOnlyList<float> GetUVAttribute() => m_UVAttribute;
        public IReadOnlyList<float> GetTangentAttribute() => m_TangentAttribute;

        // MetaData.BoneIndexSizeに応じてバイト数が変わる
        public IReadOnlyList<uint> GetUIntBoneAttribute() => m_UIntBoneAttribute;
        public IReadOnlyList<byte> GetByteBoneAttribute() => m_ByteBoneAttribute;
        public IReadOnlyList<ushort> GetUShortBoneAttribute() => m_UShortBoneAttribute;

        public IReadOnlyList<float> GetWeightAttribute() => m_WeightAttribute;
        public IReadOnlyList<IReadOnlyList<float>> GetAdditionalUVAttribute() => m_AdditionalUVAttribute;

        // インデックスバッファ
        // MetaData.VertexIndexSizeに応じてバイト数が変わる
        public IReadOnlyList<uint> GetUIntIndices() => m_UIntIndices;
        public IReadOnlyList<byte> GetByteIndices() => m_ByteIndices;
        public IReadOnlyList<ushort> GetUShortIndices() => m_UShortIndices;
    }
}