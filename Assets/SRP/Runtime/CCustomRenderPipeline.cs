using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CCustomRenderPipeline : RenderPipeline
{
    CCustomRenderer m_Renderer = new CCustomRenderer();

    public CCustomRenderPipeline(bool PerObjLight)
    {
    }

    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            var camera = cameras[i];

            if(!m_Renderer.Render(context, camera))
            {
                Debug.LogErrorFormat("[CCustomRenderPipeline] {0} camera failed to render.", camera.name);
                continue;
            }
        }
    }
}
