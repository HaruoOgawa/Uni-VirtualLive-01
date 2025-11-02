using UnityEngine;

namespace mmdlib
{
    public struct SPmxMetaData
    {
        public EPmxEncodeType EncodeType;
        public int AdditionalUVCount;
        public int VertexIndexSize;
        public int TextureIndexSize;
        public int MaterialIndexSize;
        public int BoneIndexSize;
        public int MorphIndexSize;
        public int RigidIndexSize;
        public string ModelName;
        public string ModelName_EN;
        public string Comment;
        public string Comment_EN;

        public SPmxMetaData(
            EPmxEncodeType encodeType = EPmxEncodeType.UTF16,
            int additionalUVCount = 0,
            int vertexIndexSize = 0,
            int textureIndexSize = 0,
            int materialIndexSize = 0,
            int boneIndexSize = 0,
            int morphIndexSize = 0,
            int rigidIndexSize = 0,
            string modelName = "",
            string modelName_EN = "",
            string comment = "",
            string comment_EN = ""
        )
        {
            this.EncodeType = encodeType;
            this.AdditionalUVCount = additionalUVCount;
            this.VertexIndexSize = vertexIndexSize;
            this.TextureIndexSize = textureIndexSize;
            this.MaterialIndexSize = materialIndexSize;
            this.BoneIndexSize = boneIndexSize;
            this.MorphIndexSize = morphIndexSize;
            this.RigidIndexSize = rigidIndexSize;
            this.ModelName = modelName;
            this.ModelName_EN = modelName_EN;
            this.Comment = comment;
            this.Comment_EN = comment_EN;
        }
    }
}
