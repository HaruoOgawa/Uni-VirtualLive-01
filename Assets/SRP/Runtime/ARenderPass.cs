using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LightTransport;
using UnityEngine.Rendering;

public abstract class ARenderPass
{
    List<ShaderTagId> m_TargetShaderTags = new List<ShaderTagId>();

    public ARenderPass()
    {
    }

    public void AddShaderTag(string TagName)
    {
        m_TargetShaderTags.Add(new ShaderTagId(TagName));
    }

    public List<ShaderTagId> GetTargetShaderTags()
    {
        return m_TargetShaderTags;
    }

    //public virtual CreateFrameBuffer()

    public abstract void Begin(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera);

    public abstract void End(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera);

    protected void ExecuteBuffer(ScriptableRenderContext context, CommandBuffer commandBuffer)
    {
        // コマンドバッファ内のコマンドをまとめてコンテキストに登録する
        // コンテキストが実際にGPUに送ったりといった役割を果たす
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
    }
}
