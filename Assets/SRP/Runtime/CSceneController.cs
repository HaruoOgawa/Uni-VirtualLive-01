using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

public class CSceneController
{
    // カリング結果
    CullingResults m_CullingResults;

    // ライトボリューム用メッシュ
    Mesh m_FullScreenMesh = null;
    Mesh m_SphereMesh = null;
    Mesh m_HemisphereMesh = null;

    // デファードライティング用マテリアル
    Material m_DeferredLightMat = null;

    // フルスクリーン描画用マテリアル
    Material m_FullScreenMat = null;

    public CSceneController()
    {
        Init();
    }

    void Init()
    {
        //
        CShaderGlobalKeywordList.InitKeywordList();

        // ライトボリューム用メッシュを作成
        m_FullScreenMesh = CreateFullscreenMesh();
        m_SphereMesh = CreateSphereMesh();
        m_HemisphereMesh = CreateHemisphereMesh();

        // マテリアル生成
        m_DeferredLightMat = new Material(Shader.Find("CustomSRP/GBufferLight"));
        m_FullScreenMat = new Material(Shader.Find("Hidden/FullScreen"));
    }

    public void Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera, SPassDescriptor passDescriptor)
    {
        // ビューフラスタムカリング
        if (!Cull(context, camera)) return;

        // フォアグラウンドライトの設定
        SetForegroundLightArray(context, commandBuffer, passDescriptor.PerObjLight);

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

                // PerObjLight: オブジェクト単位で別のライトを反映できるようにするかどうか
                PerObjectData lightFlag = PerObjectData.None;
                if (passDescriptor.PerObjLight)
                {
                    // Shaderでunity_LightDataやunity_LightIndicesをUnityEngineから受け取るにはこれらのフラグが必須
                    lightFlag = PerObjectData.LightData | PerObjectData.LightIndices;
                }

                // RendererListの作成
                // このDescriptorに該当するオブジェクトのリストをエンジンから引っ張ってくるイメージ
                var rendererListDesc = new RendererListDesc(passDescriptor.TargetShaderTags.ToArray(), m_CullingResults, camera)
                {
                    sortingCriteria = sortingSettings.criteria,
                    renderQueueRange = filteringSettings.renderQueueRange,
                    layerMask = filteringSettings.layerMask,
                    renderingLayerMask = filteringSettings.renderingLayerMask,
                    rendererConfiguration = lightFlag // オブジェクト単位の描画設定
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

            // PerObjLight: オブジェクト単位で別のライトを反映できるようにするかどうか
            PerObjectData lightFlag = PerObjectData.None;
            if (passDescriptor.PerObjLight)
            {
                // Shaderでunity_LightDataやunity_LightIndicesをUnityEngineから受け取るにはこれらのフラグが必須
                lightFlag = PerObjectData.LightData | PerObjectData.LightIndices;
            }

            // RendererListの作成
            // このDescriptorに該当するオブジェクトのリストをエンジンから引っ張ってくるイメージ
            var rendererListDesc = new RendererListDesc(passDescriptor.TargetShaderTags.ToArray(), m_CullingResults, camera)
            {
                sortingCriteria = sortingSettings.criteria,
                renderQueueRange = filteringSettings.renderQueueRange,
                layerMask = filteringSettings.layerMask,
                renderingLayerMask = filteringSettings.renderingLayerMask,
                rendererConfiguration = lightFlag // オブジェクト単位の描画設定
            };

            // 描画ジオメトリリストを取得
            var rendererList = context.CreateRendererList(rendererListDesc);

            // CommandBufferを使用してRendererListを描画
            commandBuffer.DrawRendererList(rendererList);
        }
    }

    public bool DrawGizmo(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera)
    {
        // Handles.ShouldRenderGizmosはギズモを描画する設定になっているかどうか
        if (Handles.ShouldRenderGizmos() && camera.cameraType == CameraType.SceneView)
        {
            commandBuffer.DrawRendererList(context.CreateGizmoRendererList(camera, GizmoSubset.PreImageEffects));
            commandBuffer.DrawRendererList(context.CreateGizmoRendererList(camera, GizmoSubset.PostImageEffects));
        }

        return true;
    }

    public bool DrawDeferredLight(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera,
        SPassDescriptor passDescriptor, CRenderTarget GBufferRT)
    {
        // ビューフラスタムカリング
        if (!Cull(context, camera)) return false;

        // GBufferをセット
        SetRTTextures(commandBuffer, GBufferRT, true, "SRP_GBuffer_");

        // カメラ情報セット
        SetCamera(commandBuffer, camera);

        // 各ライトボリュームの描画
        if (!DrawLights(context, commandBuffer, int.MaxValue)) return false;

        return true;
    }

    bool DrawLights(ScriptableRenderContext context, CommandBuffer commandBuffer, int maxLightCount)
    {
        // 各ライトボリュームの描画
        for (int i = 0; i < m_CullingResults.visibleLights.Length; i++)
        {
            // ライト数はForegroundでは8個までDefferedでは無制限
            if (i >= maxLightCount) break;

            var light = m_CullingResults.visibleLights[i];

            // 描画実行
            switch (light.lightType)
            {
                case LightType.Directional:
                    if (!DrawDirectionalLight(context, commandBuffer, light)) return false;
                    break;

                case LightType.Point:
                    if (!DrawPointLight(context, commandBuffer, light)) return false;
                    break;

                case LightType.Spot:
                    if (!DrawSpotLight(context, commandBuffer, light)) return false;
                    break;

                default:
                    break;
            }
        }

        return true;
    }

    bool DrawDirectionalLight(ScriptableRenderContext context, CommandBuffer commandBuffer, VisibleLight light)
    {
        // ライト情報をセット
        SetDeferredLight(context, commandBuffer, light);

        // 描画開始
        commandBuffer.SetKeyword(CShaderGlobalKeywordList._LIGHT_DIRECTIONAL, true);

        // 描画実行
        commandBuffer.DrawMesh(m_FullScreenMesh, Matrix4x4.identity, m_DeferredLightMat);

        // 描画終了
        commandBuffer.SetKeyword(CShaderGlobalKeywordList._LIGHT_DIRECTIONAL, false);

        return true;
    }

    bool DrawPointLight(ScriptableRenderContext context, CommandBuffer commandBuffer, VisibleLight light)
    {
        // ライト情報をセット
        SetDeferredLight(context, commandBuffer, light);

        // 描画開始
        commandBuffer.SetKeyword(CShaderGlobalKeywordList._LIGHT_POINT, true);

        // ライトのRangeプロパティをスフィアのサイズにする。回転成分は不要
        Vector4 worldPos = light.localToWorldMatrix.GetColumn(3);
        float range = light.range;

        // Unity C#は列優先
        Matrix4x4 worldMat = new Matrix4x4(
            new Vector4(range, 0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, range, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, range, 0.0f),
            new Vector4(worldPos.x, worldPos.y, worldPos.z, 1.0f)
        );

        // 描画実行
        commandBuffer.DrawMesh(m_SphereMesh, worldMat, m_DeferredLightMat);

        // 描画終了
        commandBuffer.SetKeyword(CShaderGlobalKeywordList._LIGHT_POINT, false);

        return true;
    }
    
    bool DrawSpotLight(ScriptableRenderContext context, CommandBuffer commandBuffer, VisibleLight light)
    {
        // ライト情報をセット
        SetDeferredLight(context, commandBuffer, light);

        // 描画開始
        commandBuffer.SetKeyword(CShaderGlobalKeywordList._LIGHT_SPOT, true);

        // スポットライトは回転情報も大切
        Matrix4x4 worldMat = light.localToWorldMatrix;

        // 描画実行
        commandBuffer.DrawMesh(m_HemisphereMesh, worldMat, m_DeferredLightMat);

        // 描画終了
        commandBuffer.SetKeyword(CShaderGlobalKeywordList._LIGHT_SPOT, false);

        return true;
    }

    public void DrawFullScreenRT(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera, RenderTexture rt)
    {
        m_FullScreenMat.SetTexture("_MainTex", rt);

        var keyword = (camera.cameraType == CameraType.SceneView) ? CShaderGlobalKeywordList.UNITY_SCENE_VIEW : CShaderGlobalKeywordList.UNITY_GAME_VIEW;

        // 描画実行
        commandBuffer.SetKeyword(keyword, true);
        commandBuffer.DrawMesh(m_FullScreenMesh, Matrix4x4.identity, m_FullScreenMat);
        commandBuffer.SetKeyword(keyword, false);
    }

    void SetRTTextures(CommandBuffer commandBuffer, CRenderTarget renderTarget, 
        bool Color, string BaseColorName, bool Depth = false, string BaseDepthName = "")
    {
        // カラーテクスチャをセット
        if(Color)
        {
            for (int i = 0; i < renderTarget.GetColorBuffers().Count; i++)
            {
                var ColorBuffer = renderTarget.GetColorBuffers()[i];

                string ShaderName = BaseColorName + i.ToString();
                int ShaderID = Shader.PropertyToID(ShaderName);
                commandBuffer.SetGlobalTexture(ShaderID, ColorBuffer);
            }
        }

        // デプステクスチャをセット
        if (Depth)
        {
            var DepthBuffer = renderTarget.GetDepthBuffer();

            string ShaderName = BaseDepthName;
            int ShaderID = Shader.PropertyToID(ShaderName);
            commandBuffer.SetGlobalTexture(ShaderID, DepthBuffer);
        }
    }

    void SetForegroundLightArray(ScriptableRenderContext context, CommandBuffer commandBuffer, bool PerObjLight)
    {
        // ライトの最大数を決めておく
        const int MaxMainLightCount = 8;
        const int MaxSubLightCount = 64;

        // シェーダーには決まったサイズの配列しか渡せないのでここで決め打ちしておく
        Vector4[] MainLightDirList = new Vector4[MaxMainLightCount];
        Vector4[] MainLightColorList = new Vector4[MaxMainLightCount];

        Vector4[] SubLightPosList = new Vector4[MaxSubLightCount];
        Vector4[] SubLightColorList = new Vector4[MaxSubLightCount];
        Vector4[] SubLightDirList = new Vector4[MaxSubLightCount];

        int NumOfMainLight = 0;
        int NumOfSubLight = 0;

        //
        NativeArray<int> lightIndexMap = m_CullingResults.GetLightIndexMap(Allocator.Temp);

        // 各ライトボリュームの描画
        for (int i = 0; i < m_CullingResults.visibleLights.Length; i++)
        {
            var light = m_CullingResults.visibleLights[i];

            int lightIndex = -1;

            switch (light.lightType)
            {
                case LightType.Directional:
                    if(NumOfMainLight < MaxMainLightCount)
                    {
                        PrepareDirectionalLight(light, NumOfMainLight++, ref MainLightDirList, ref MainLightColorList);
                    }
                    break;

                case LightType.Point:
                    if(NumOfSubLight < MaxSubLightCount)
                    {
                        // サブライトのみライトインデックスを設定する
                        // メインライトは固定で8個まで使用する
                        lightIndex = NumOfSubLight;

                        PreparePointLight(light, NumOfSubLight++, ref SubLightPosList, ref SubLightColorList, ref SubLightDirList);
                    }
                    break;

                case LightType.Spot:
                    if (NumOfSubLight < MaxSubLightCount)
                    {
                        // サブライトのみライトインデックスを設定する
                        // メインライトは固定で8個まで使用する
                        lightIndex = NumOfSubLight;

                        PrepareSpotLight(light, NumOfSubLight++, ref SubLightPosList, ref SubLightColorList, ref SubLightDirList);
                    }
                    break;

                default:
                    break;
            }

            lightIndexMap[i] = lightIndex;
        }

        // ShaderLabが持つfloat4[2]型のunity_LightIndicesに設定されるオブジェクトの近くにあるライトのインデックスリストを更新する
        m_CullingResults.SetLightIndexMap(lightIndexMap);

        // メモリ解放
        lightIndexMap.Dispose();

        // MainLight
        commandBuffer.SetGlobalInt(CShaderConstants.SRP_Foreground_MainLightCount, NumOfMainLight);
        commandBuffer.SetGlobalVectorArray(CShaderConstants.SRP_Foreground_MainLightDirArray, MainLightDirList);
        commandBuffer.SetGlobalVectorArray(CShaderConstants.SRP_Foreground_MainLightColorArray, MainLightColorList);

        // SubLight
        commandBuffer.SetGlobalInt(CShaderConstants.SRP_Foreground_SubLightCount, NumOfSubLight);
        commandBuffer.SetGlobalVectorArray(CShaderConstants.SRP_Foreground_SubLightPosArray, SubLightPosList);
        commandBuffer.SetGlobalVectorArray(CShaderConstants.SRP_Foreground_SubLightColorArray, SubLightColorList);
        commandBuffer.SetGlobalVectorArray(CShaderConstants.SRP_Foreground_SubLightDirArray, SubLightDirList);
    }

    void PrepareDirectionalLight(VisibleLight light, int LightIndex, ref Vector4[] LightDirList, ref Vector4[] LightColorList)
    {
        Vector4 lightPos, spotLightDir = new Vector4();
        CalcLightParam(light, out lightPos, out spotLightDir);

        LightDirList[LightIndex] = lightPos;
        LightColorList[LightIndex] = light.finalColor;
    }

    void PreparePointLight(VisibleLight light, int LightIndex, ref Vector4[] LightPosList, ref Vector4[] LightColorList, ref Vector4[] LightDirList)
    {
        Vector4 lightPos, spotLightDir = new Vector4();
        CalcLightParam(light, out lightPos, out spotLightDir);

        LightPosList[LightIndex] = lightPos;
        LightColorList[LightIndex] = light.finalColor;
        LightDirList[LightIndex] = spotLightDir;
    }

    void PrepareSpotLight(VisibleLight light, int LightIndex, ref Vector4[] LightPosList, ref Vector4[] LightColorList, ref Vector4[] LightDirList)
    {
        Vector4 lightPos, spotLightDir = new Vector4();
        CalcLightParam(light, out lightPos, out spotLightDir);

        LightPosList[LightIndex] = lightPos;
        LightColorList[LightIndex] = light.finalColor;
        LightDirList[LightIndex] = spotLightDir;
    }

    void SetDeferredLight(ScriptableRenderContext context, CommandBuffer commandBuffer, VisibleLight light)
    {
        // ライト
        Vector4 lightPos, spotLightDir = new Vector4();
        CalcLightParam(light, out lightPos, out spotLightDir);

        commandBuffer.SetGlobalVector(CShaderConstants.SRP_Deferred_LightPos, lightPos);
        commandBuffer.SetGlobalColor(CShaderConstants.SRP_Deferred_LightColor, light.finalColor);
    }

    void CalcLightParam(VisibleLight light, out Vector4 lightPos, out Vector4 soptLightDir)
    {
        lightPos = Vector4.zero;
        soptLightDir = Vector4.zero;

        var localToWorldMat = light.localToWorldMatrix;

        if (light.lightType == LightType.Directional)
        {
            // ディレクショナルライトの場合、ワールド行列の2列目にライト方向が入っている
            Vector4 dir = localToWorldMat.GetColumn(2);
            lightPos = new Vector4(dir.x, dir.y, dir.z, 0.0f);
        }
        else
        {
            // そのほかはいつも通り平行移動成分から取得
            Vector4 pos = localToWorldMat.GetColumn(3);
            lightPos = new Vector4(pos.x, pos.y, pos.z, 1.0f);

            // スポットライト情報を計算
            if(light.lightType == LightType.Spot)
            {

            }
        }
    }

    void SetCamera(CommandBuffer commandBuffer, Camera camera)
    {
        commandBuffer.SetGlobalVector(CShaderConstants.SRP_CameraPos, camera.transform.position);
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

    static Mesh CreateSphereMesh()
    {
        // This icosaedron has been been slightly inflated to fit an unit sphere.
        // This is the same geometry as built-in deferred.

        Vector3[] positions =
        {
                new Vector3(0.000f,  0.000f, -1.070f), new Vector3(0.174f, -0.535f, -0.910f),
                new Vector3(-0.455f, -0.331f, -0.910f), new Vector3(0.562f,  0.000f, -0.910f),
                new Vector3(-0.455f,  0.331f, -0.910f), new Vector3(0.174f,  0.535f, -0.910f),
                new Vector3(-0.281f, -0.865f, -0.562f), new Vector3(0.736f, -0.535f, -0.562f),
                new Vector3(0.296f, -0.910f, -0.468f), new Vector3(-0.910f,  0.000f, -0.562f),
                new Vector3(-0.774f, -0.562f, -0.478f), new Vector3(0.000f, -1.070f,  0.000f),
                new Vector3(-0.629f, -0.865f,  0.000f), new Vector3(0.629f, -0.865f,  0.000f),
                new Vector3(-1.017f, -0.331f,  0.000f), new Vector3(0.957f,  0.000f, -0.478f),
                new Vector3(0.736f,  0.535f, -0.562f), new Vector3(1.017f, -0.331f,  0.000f),
                new Vector3(1.017f,  0.331f,  0.000f), new Vector3(-0.296f, -0.910f,  0.478f),
                new Vector3(0.281f, -0.865f,  0.562f), new Vector3(0.774f, -0.562f,  0.478f),
                new Vector3(-0.736f, -0.535f,  0.562f), new Vector3(0.910f,  0.000f,  0.562f),
                new Vector3(0.455f, -0.331f,  0.910f), new Vector3(-0.174f, -0.535f,  0.910f),
                new Vector3(0.629f,  0.865f,  0.000f), new Vector3(0.774f,  0.562f,  0.478f),
                new Vector3(0.455f,  0.331f,  0.910f), new Vector3(0.000f,  0.000f,  1.070f),
                new Vector3(-0.562f,  0.000f,  0.910f), new Vector3(-0.957f,  0.000f,  0.478f),
                new Vector3(0.281f,  0.865f,  0.562f), new Vector3(-0.174f,  0.535f,  0.910f),
                new Vector3(0.296f,  0.910f, -0.478f), new Vector3(-1.017f,  0.331f,  0.000f),
                new Vector3(-0.736f,  0.535f,  0.562f), new Vector3(-0.296f,  0.910f,  0.478f),
                new Vector3(0.000f,  1.070f,  0.000f), new Vector3(-0.281f,  0.865f, -0.562f),
                new Vector3(-0.774f,  0.562f, -0.478f), new Vector3(-0.629f,  0.865f,  0.000f),
            };

        int[] indices =
        {
                0,  1,  2,  0,  3,  1,  2,  4,  0,  0,  5,  3,  0,  4,  5,  1,  6,  2,
                3,  7,  1,  1,  8,  6,  1,  7,  8,  9,  4,  2,  2,  6, 10, 10,  9,  2,
                8, 11,  6,  6, 12, 10, 11, 12,  6,  7, 13,  8,  8, 13, 11, 10, 14,  9,
                10, 12, 14,  3, 15,  7,  5, 16,  3,  3, 16, 15, 15, 17,  7, 17, 13,  7,
                16, 18, 15, 15, 18, 17, 11, 19, 12, 13, 20, 11, 11, 20, 19, 17, 21, 13,
                13, 21, 20, 12, 19, 22, 12, 22, 14, 17, 23, 21, 18, 23, 17, 21, 24, 20,
                23, 24, 21, 20, 25, 19, 19, 25, 22, 24, 25, 20, 26, 18, 16, 18, 27, 23,
                26, 27, 18, 28, 24, 23, 27, 28, 23, 24, 29, 25, 28, 29, 24, 25, 30, 22,
                25, 29, 30, 14, 22, 31, 22, 30, 31, 32, 28, 27, 26, 32, 27, 33, 29, 28,
                30, 29, 33, 33, 28, 32, 34, 26, 16,  5, 34, 16, 14, 31, 35, 14, 35,  9,
                31, 30, 36, 30, 33, 36, 35, 31, 36, 37, 33, 32, 36, 33, 37, 38, 32, 26,
                34, 38, 26, 38, 37, 32,  5, 39, 34, 39, 38, 34,  4, 39,  5,  9, 40,  4,
                9, 35, 40,  4, 40, 39, 35, 36, 41, 41, 36, 37, 41, 37, 38, 40, 35, 41,
                40, 41, 39, 41, 38, 39,
            };


        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt16;
        mesh.vertices = positions;
        mesh.triangles = indices;

        return mesh;
    }

    static Mesh CreateHemisphereMesh()
    {
        // TODO reorder for pre&post-transform cache optimisation.
        // This capped hemisphere shape is in unit dimensions. It will be slightly inflated in the vertex shader
        // to fit the cone analytical shape.
        Vector3[] positions =
        {
                new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(1.000000f, 0.000000f, 0.000000f),
                new Vector3(0.923880f, 0.382683f, 0.000000f), new Vector3(0.707107f, 0.707107f, 0.000000f),
                new Vector3(0.382683f, 0.923880f, 0.000000f), new Vector3(-0.000000f, 1.000000f, 0.000000f),
                new Vector3(-0.382684f, 0.923880f, 0.000000f), new Vector3(-0.707107f, 0.707107f, 0.000000f),
                new Vector3(-0.923880f, 0.382683f, 0.000000f), new Vector3(-1.000000f, -0.000000f, 0.000000f),
                new Vector3(-0.923880f, -0.382683f, 0.000000f), new Vector3(-0.707107f, -0.707107f, 0.000000f),
                new Vector3(-0.382683f, -0.923880f, 0.000000f), new Vector3(0.000000f, -1.000000f, 0.000000f),
                new Vector3(0.382684f, -0.923879f, 0.000000f), new Vector3(0.707107f, -0.707107f, 0.000000f),
                new Vector3(0.923880f, -0.382683f, 0.000000f), new Vector3(0.000000f, 0.000000f, 1.000000f),
                new Vector3(0.707107f, 0.000000f, 0.707107f), new Vector3(0.000000f, -0.707107f, 0.707107f),
                new Vector3(0.000000f, 0.707107f, 0.707107f), new Vector3(-0.707107f, 0.000000f, 0.707107f),
                new Vector3(0.816497f, -0.408248f, 0.408248f), new Vector3(0.408248f, -0.408248f, 0.816497f),
                new Vector3(0.408248f, -0.816497f, 0.408248f), new Vector3(0.408248f, 0.816497f, 0.408248f),
                new Vector3(0.408248f, 0.408248f, 0.816497f), new Vector3(0.816497f, 0.408248f, 0.408248f),
                new Vector3(-0.816497f, 0.408248f, 0.408248f), new Vector3(-0.408248f, 0.408248f, 0.816497f),
                new Vector3(-0.408248f, 0.816497f, 0.408248f), new Vector3(-0.408248f, -0.816497f, 0.408248f),
                new Vector3(-0.408248f, -0.408248f, 0.816497f), new Vector3(-0.816497f, -0.408248f, 0.408248f),
                new Vector3(0.000000f, -0.923880f, 0.382683f), new Vector3(0.923880f, 0.000000f, 0.382683f),
                new Vector3(0.000000f, -0.382683f, 0.923880f), new Vector3(0.382683f, 0.000000f, 0.923880f),
                new Vector3(0.000000f, 0.923880f, 0.382683f), new Vector3(0.000000f, 0.382683f, 0.923880f),
                new Vector3(-0.923880f, 0.000000f, 0.382683f), new Vector3(-0.382683f, 0.000000f, 0.923880f)
            };

        int[] indices =
        {
                0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 5, 4, 0, 6, 5, 0,
                7, 6, 0, 8, 7, 0, 9, 8, 0, 10, 9, 0, 11, 10, 0, 12,
                11, 0, 13, 12, 0, 14, 13, 0, 15, 14, 0, 16, 15, 0, 1, 16,
                22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 14, 24, 34, 35,
                22, 16, 36, 23, 37, 2, 27, 35, 38, 25, 4, 37, 26, 39, 6, 30,
                38, 40, 28, 8, 39, 29, 41, 10, 33, 40, 34, 31, 12, 41, 32, 36,
                15, 22, 24, 18, 23, 22, 19, 24, 23, 3, 25, 27, 20, 26, 25, 18,
                27, 26, 7, 28, 30, 21, 29, 28, 20, 30, 29, 11, 31, 33, 19, 32,
                31, 21, 33, 32, 13, 14, 34, 15, 24, 14, 19, 34, 24, 1, 35, 16,
                18, 22, 35, 15, 16, 22, 17, 36, 37, 19, 23, 36, 18, 37, 23, 1,
                2, 35, 3, 27, 2, 18, 35, 27, 5, 38, 4, 20, 25, 38, 3, 4,
                25, 17, 37, 39, 18, 26, 37, 20, 39, 26, 5, 6, 38, 7, 30, 6,
                20, 38, 30, 9, 40, 8, 21, 28, 40, 7, 8, 28, 17, 39, 41, 20,
                29, 39, 21, 41, 29, 9, 10, 40, 11, 33, 10, 21, 40, 33, 13, 34,
                12, 19, 31, 34, 11, 12, 31, 17, 41, 36, 21, 32, 41, 19, 36, 32
            };

        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt16;
        mesh.vertices = positions;
        mesh.triangles = indices;

        return mesh;
    }

    static Mesh CreateFullscreenMesh()
    {
        // TODO reorder for pre&post-transform cache optimisation.
        // Simple full-screen triangle.
        Vector3[] positions =
        {
                new Vector3(-1.0f,  1.0f, 0.0f),
                new Vector3(-1.0f, -1.0f, 0.0f),
                new Vector3(1.0f,  1.0f, 0.0f),
                new Vector3(1.0f,  -1.0f, 0.0f)
        };

        Vector2[] uvs =
        {
            new Vector2(0.0f, 1.0f),
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(1.0f, 0.0f),
        };

        //int[] indices = { 0, 1, 2, 2, 1, 3 };
        int[] indices = { 0, 2, 1, 1, 2, 3 };

        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt16;
        mesh.vertices = positions;
        mesh.uv = uvs;
        mesh.triangles = indices;

        return mesh;
    }
}
