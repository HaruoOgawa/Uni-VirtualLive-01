using UnityEngine;

namespace mmdlib
{
    public class CPmxTexture
    {
		string m_FilePath = string.Empty;

        public CPmxTexture()
		{
		}

		public string GetFilePath()
		{
			return m_FilePath;
		}

		public void SetFilePath(string FilePath)
		{
			m_FilePath = FilePath;
		}
	}
}