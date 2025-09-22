using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CCustomRenderPipeline : RenderPipeline
{
    CCustomRenderer m_Renderer = new CCustomRenderer();

    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            m_Renderer.Render(context, cameras[i]);
        }
    }
}
