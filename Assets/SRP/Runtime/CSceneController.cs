using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

public class CSceneController
{
    CullingResults m_CullingResults;

    public CSceneController()
    {
    }

    public void Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera, SPassDescriptor passDescriptor)
    {
        // ビューフラスタムカリング
        if (!Cull(context, camera)) return;

        //m_CullingResults.visibleLights

        // 不透明ジオメトリの描画
        if (passDescriptor.DrawOpaque)
        {
            {
                // 描画設定
                // カメラ距離でソートする
                var sortingSettings = new SortingSettings(camera)
                {
                    // 一般的な不透明オブジェクトの手前から描画するソートを採用する
                    criteria = SortingCriteria.CommonOpaque
                };

                // 描画対象レンダーキューの指定
                var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

                // RendererListの作成
                // このDescriptorに該当するオブジェクトのリストをエンジンから引っ張ってくるイメージ
                var rendererListDesc = new RendererListDesc(passDescriptor.TargetShaderTags.ToArray(), m_CullingResults, camera)
                {
                    sortingCriteria = sortingSettings.criteria,
                    renderQueueRange = filteringSettings.renderQueueRange,
                    layerMask = filteringSettings.layerMask,
                    renderingLayerMask = filteringSettings.renderingLayerMask
                };

                // 描画ジオメトリリストを取得
                var rendererList = context.CreateRendererList(rendererListDesc);

                // CommandBufferを使用してRendererListを描画
                commandBuffer.DrawRendererList(rendererList);
            }

            // SkyBoxの描画 (RendererList APIを使用)
            if(passDescriptor.DrawSky)
            {
                var skyboxRendererList = context.CreateSkyboxRendererList(camera);
                commandBuffer.DrawRendererList(skyboxRendererList);
            }
        }

        // 透明ジオメトリの描画
        // 透明ジオメトリの深度はバッファに書き込まれないのでスカイボックスよりも前に
        // 描画していると上書きされて消えてしまう
        if(passDescriptor.DrawTransparent)
        {
            // 描画設定
            // カメラ距離でソートする
            var sortingSettings = new SortingSettings(camera)
            {
                // 透明オブジェクトは奥から手前の順でソートする
                criteria = SortingCriteria.CommonTransparent
            };

            // 描画対象レンダーキューの指定
            var filteringSettings = new FilteringSettings(RenderQueueRange.transparent);

            // RendererListの作成
            // このDescriptorに該当するオブジェクトのリストをエンジンから引っ張ってくるイメージ
            var rendererListDesc = new RendererListDesc(passDescriptor.TargetShaderTags.ToArray(), m_CullingResults, camera)
            {
                sortingCriteria = sortingSettings.criteria,
                renderQueueRange = filteringSettings.renderQueueRange,
                layerMask = filteringSettings.layerMask,
                renderingLayerMask = filteringSettings.renderingLayerMask
            };

            // 描画ジオメトリリストを取得
            var rendererList = context.CreateRendererList(rendererListDesc);

            // CommandBufferを使用してRendererListを描画
            commandBuffer.DrawRendererList(rendererList);
        }
    }

    bool Cull(ScriptableRenderContext context, Camera camera)
    {
        // ここで実際にカリングをしているというわけではないが、
        // カリング用のパラメーター(たぶんCULL_NONEとかCULL_FRONTみたいな)
        // やつが正常に取れればカリングがGraphics API側で正常に
        // 機能しているということを表し、処理を継続するかどうかを
        // 決めている？
        ScriptableCullingParameters p;
        if (camera.TryGetCullingParameters(out p))
        {
            // コンテキスト経由でグラフィックAPIにカリング設定をする？
            // いや、ビューフラスタムカリングを実行している
            // 画面外のやつのドローコールをそもそもスキップするやつ
            m_CullingResults = context.Cull(ref p);

            return true;
        }

        return false;
    }
}
