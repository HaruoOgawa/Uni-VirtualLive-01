using System.Collections.Generic;
using UnityEngine;

public class CPmxMorphTarget
{
    // モーフ名
    string m_MorphName;
    string m_MorphName_EN;

    // グループモーフ
    // 未実装

    // 頂点モーフ
    Dictionary<int, Vector3> m_VertexMorphList = new Dictionary<int, Vector3>();

    // ボーンモーフ
    // 未実装

    // UVモーフ
    // 未実装

    // 追加UV1モーフ
    // 未実装

    // 追加UV2モーフ
    // 未実装

    // 追加UV3モーフ
    // 未実装

    // 追加UV4モーフ
    // 未実装

    // 材質モーフ
    // 未実装

    public CPmxMorphTarget(string MorphName, string MorphName_EN)
    {
        m_MorphName = MorphName;
        m_MorphName_EN = MorphName_EN;
    }

    public void AddVertexMorph(int VertexIndex, Vector3 Offset)
    {
        if(m_VertexMorphList.ContainsKey(VertexIndex)) return;

        m_VertexMorphList.Add(VertexIndex, Offset);
    }

	public Dictionary<int, Vector3> GetVertexMorphList()
    {
        return m_VertexMorphList;
    }
}
