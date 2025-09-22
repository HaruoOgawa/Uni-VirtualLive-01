using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public abstract class ARenderPass
{
    List<ShaderTagId> m_TargetShaderTags { get; set; } = new List<ShaderTagId>();

    public ARenderPass()
    {
    }

    public void AddShaderTag(string TagName)
    {
        m_TargetShaderTags.Add(new ShaderTagId(TagName));
    }

    //public virtual CreateFrameBuffer()

    public virtual void Begin(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera)
    {

    }

    public virtual void End(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera)
    {

    }
}
