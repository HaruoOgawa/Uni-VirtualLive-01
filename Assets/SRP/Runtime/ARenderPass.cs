using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LightTransport;
using UnityEngine.Rendering;

public abstract class ARenderPass
{
    protected List<ShaderTagId> m_TargetShaderTags = new List<ShaderTagId>();

    protected CRenderTarget m_RenderTarget = null;

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

    public void SetRenderTarget(CRenderTarget RenderTarget)
    {
        m_RenderTarget = RenderTarget;
    }

    public CRenderTarget GetRenderTarget()
    {
        return m_RenderTarget;
    }

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
