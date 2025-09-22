using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

public class CameraRenderer
{
    // Vulkanとかでもよくあるコマンドバッファ
    const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer { name = bufferName };

    ScriptableRenderContext context;
    Camera camera;

    CullingResults cullingResults;

    // SRPのUnlitシェーダー識別子
    // ShaderのLightModeタグがこれと一致するものが描画される
    //static ShaderTagId unlitShaderTagId = new ShaderTagId("UniversalForward");
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    //レガシーシェーダーの識別子
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    // GBuffer用のシェーダー識別子
    static ShaderTagId gbufferShaderTagId = new ShaderTagId("CustomGBuffer");

    // エラーマテリアル
    static Material ErrorMaterial = null;

    // カメラごとに異なる描画手法を使うためにこのようにカメラごとにレンダラーをわけることができるようにな設計になっている
    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        DrawGBufferPass();
        DrawForegroundPass();
    }

    void DrawGBufferPass()
    {

    }

    void DrawForegroundPass()
    {
        // ビューフラスタムカリングで画面外のオブジェクトの描画をスキップ
        if (!Cull())
        {
            // カリングの設定に失敗
            return;
        }

        PrepareBuffer();
        Setup();
        DrawVisibleGeometry();
        DrawUnSupportedShaders();
        Submit();
    }

    void PrepareBuffer()
    {
        buffer.name = camera.name;
        //buffer.render()
        //context.CreateShadowRendererList
    }

    void Setup()
    {
        // カメラのビュープロジェクション行列を設定する
        context.SetupCameraProperties(camera);

        // フレームバッファ(フレームテクスチャ)の初期化コマンドを発行
        buffer.ClearRenderTarget(true, true, Color.clear);

        // プロファイラ(例えばFrame Debugger)への記録開始
        buffer.BeginSample(bufferName);

        // コマンドをコンテキストに登録
        ExecuteBuffer();
    }

    void DrawVisibleGeometry()
    {
        // 不透明ジオメトリの描画
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
                var rendererListDesc = new RendererListDesc(unlitShaderTagId, cullingResults, camera)
                {
                    sortingCriteria = sortingSettings.criteria,
                    renderQueueRange = filteringSettings.renderQueueRange,
                    layerMask = filteringSettings.layerMask,
                    renderingLayerMask = filteringSettings.renderingLayerMask
                };

                // 描画ジオメトリリストを取得
                var rendererList = context.CreateRendererList(rendererListDesc);

                // CommandBufferを使用してRendererListを描画
                buffer.DrawRendererList(rendererList);
            }

            // SkyBoxの描画 (RendererList APIを使用)
            {
                var skyboxRendererList = context.CreateSkyboxRendererList(camera);
                buffer.DrawRendererList(skyboxRendererList);
            }
        }

        // 透明ジオメトリの描画
        // 透明ジオメトリの深度はバッファに書き込まれないのでスカイボックスよりも前に
        // 描画していると上書きされて消えてしまう
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
            var rendererListDesc = new RendererListDesc(unlitShaderTagId, cullingResults, camera)
            {
                sortingCriteria = sortingSettings.criteria,
                renderQueueRange = filteringSettings.renderQueueRange,
                layerMask = filteringSettings.layerMask,
                renderingLayerMask = filteringSettings.renderingLayerMask
            };

            // 描画ジオメトリリストを取得
            var rendererList = context.CreateRendererList(rendererListDesc);

            // CommandBufferを使用してRendererListを描画
            buffer.DrawRendererList(rendererList);
            //buffer.SetRenderTarget()
        }
    }

    void DrawGBufferGeometry()
    {
        // GBufferでは不透明オブジェクトしか考慮しない
        //context.BeginRenderPass()

    }
    void DrawUnSupportedShaders()
    {
        if(ErrorMaterial  == null)
        {
            ErrorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        // 旧式のシェーダーを描画(Standardシェーダーとか)
        var sortingSettings = new SortingSettings(camera);
        var filteringSettings = FilteringSettings.defaultValue;

        var rendererListDesc = new RendererListDesc(legacyShaderTagIds, cullingResults, camera)
        {
            sortingCriteria = sortingSettings.criteria,
            renderQueueRange = filteringSettings.renderQueueRange,
            layerMask = filteringSettings.layerMask,
            renderingLayerMask = filteringSettings.renderingLayerMask,
            //overrideMaterial = ErrorMaterial
        };

        var renderList = context.CreateRendererList(rendererListDesc);

        buffer.DrawRendererList(renderList);
    }

    void Submit()
    {
        // プロファイラの記録終了
        buffer.EndSample(bufferName);

        // コマンドをコンテキストに登録
        ExecuteBuffer();

        // コンテキストに積み上げられたコマンドを全て実行する
        context.Submit();
    }

    void ExecuteBuffer()
    {
        // コマンドバッファ内のコマンドをまとめてコンテキストに登録する
        // コンテキストが実際にGPUに送ったりといった役割を果たす
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool Cull()
    {
        // ここで実際にカリングをしているというわけではないが、
        // カリング用のパラメーター(たぶんCULL_NONEとかCULL_FRONTみたいな)
        // やつが正常に取れればカリングがGraphics API側で正常に
        // 機能しているということを表し、処理を継続するかどうかを
        // 決めている？
        ScriptableCullingParameters p;
        if(camera.TryGetCullingParameters(out p))
        {
            // コンテキスト経由でグラフィックAPIにカリング設定をする？
            // いや、ビューフラスタムカリングを実行している
            // 画面外のやつのドローコールをそもそもスキップするやつ
            cullingResults = context.Cull(ref p);

            return true;
        }

        return false;
    }
}
