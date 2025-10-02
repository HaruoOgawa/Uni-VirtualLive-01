using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LightTransport;
using UnityEngine.Rendering;

public abstract class ARenderPass
{
    protected string BufferName = "Render Buffer";

    protected List<ShaderTagId> m_TargetShaderTags = new List<ShaderTagId>();

    protected CRenderTarget m_RenderTarget = null;

    protected string m_PassName = string.Empty;

    public ARenderPass(string PassName)
    {
        this.m_PassName = PassName;
    }

    public void AddShaderTag(string TagName)
    {
        this.m_TargetShaderTags.Add(new ShaderTagId(TagName));
    }

    public List<ShaderTagId> GetTargetShaderTags()
    {
        return this.m_TargetShaderTags;
    }

    public void SetRenderTarget(CRenderTarget RenderTarget)
    {
        this.m_RenderTarget = RenderTarget;
    }

    public CRenderTarget GetRenderTarget()
    {
        return this.m_RenderTarget;
    }
    public abstract void Begin(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera);

    public abstract void End(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera);

    protected void ExecuteBuffer(ScriptableRenderContext context, CommandBuffer commandBuffer, string BufferName)
    {
        // デバッガの表記名を登録
        commandBuffer.name = BufferName;

        // コマンドバッファ内のコマンドをまとめてコンテキストに登録する
        // コンテキストが実際にGPUに送ったりといった役割を果たす
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
    }
}
