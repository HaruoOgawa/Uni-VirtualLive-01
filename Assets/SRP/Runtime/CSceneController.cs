using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using static UnityEditor.PlayerSettings;

public class CSceneController
{
    // カリング結果
    CullingResults m_CullingResults;

    // ライトボリューム用メッシュ
    Mesh m_FullScreenMesh = null;
    Mesh m_SphereMesh = null;
    Mesh m_ConeMesh = null;

    // 描画に使用可能なディレクショナルライトリスト
    List<(VisibleLight visibleLight, int lightIndex)> m_VisibleDirectionalLightList = new List<(VisibleLight, int)>();
    List<Matrix4x4> m_LightViewProjMatrixList = new List<Matrix4x4>();

    // デファードライティング用マテリアル
    Material m_DeferredLightMat = null;
    Material m_DeferredIndirectLightMat = null;

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
        m_ConeMesh = CreateConeMesh();

        // マテリアル生成
        m_DeferredLightMat = new Material(Shader.Find("CustomSRP/GBufferLight"));
        m_DeferredIndirectLightMat = new Material(Shader.Find("CustomSRP/GBufferIndirectLight"));
        m_FullScreenMat = new Material(Shader.Find("Hidden/FullScreen"));
    }

    public void Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera, SPassDescriptor passDescriptor, CRenderTarget ShadowMapRT)
    {
        // シャドウマップをセットする
        if (ShadowMapRT != null) SetRTTextures(commandBuffer, ShadowMapRT, true, "SRP_ShadowMap_");

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

                lightFlag |= PerObjectData.ReflectionProbes;

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

    // シャドウマップ描画
    public bool DrawShadowMap(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera, SShadowDescriptor shadowDescriptor)
    {
        if (m_CullingResults == null) return false;

        // ライトが存在しない
        if(m_VisibleDirectionalLightList.Count == 0) return true;

        // シャドウマップの分割数
        int splitNum = (m_VisibleDirectionalLightList.Count <= 1) ? 1 : 2;

        // シャドウマップの分割セル単位の解像度
        int splitResolution = shadowDescriptor.Resolution / splitNum;

        //
        m_LightViewProjMatrixList.Clear();

        //
        for (int i = 0; i < Mathf.Min(4, m_VisibleDirectionalLightList.Count); i++)
        {
            var visibleLight = m_VisibleDirectionalLightList[i];

            // シャドウマップカメラのビューポートを再計算
            // ビューポートはフレームバッファのどの範囲に描画するか
            SetShadowCameraViewPort(context, commandBuffer, splitNum, i, splitResolution);
           
            // シャドウマップ用のビュー行列・プロジェクション行列を計算
            Matrix4x4 viewMatrix = new Matrix4x4();
            Matrix4x4 projMatrix = new Matrix4x4();
            ShadowSplitData shadowSplitData = new ShadowSplitData();

            m_CullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                visibleLight.lightIndex, // CullingResultのVisibleLight内のインデックス
                0,  
                1, 
                Vector3.zero,
                shadowDescriptor.Resolution, // シャドウマップの解像度
                shadowDescriptor.NearPlaneOffset,
                out viewMatrix,
                out projMatrix,
                out shadowSplitData
            );

            // ビュー行列・プロジェクション行列を設定
            commandBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);

            Matrix4x4 viewProj = projMatrix * viewMatrix;
            //m_LightViewProjMatrixList.Add(viewProj);

            // シャドウマップの設定
            ShadowDrawingSettings settings = new ShadowDrawingSettings(m_CullingResults, visibleLight.lightIndex);

            // シャドウ描画ジオメトリリストを取得
            var rendererList = context.CreateShadowRendererList(ref settings);

            // 描画実行
            commandBuffer.DrawRendererList(rendererList);
        }

        //commandBuffer.SetGlobalMatrixArray(CShaderConstants.SRP_DirectionLight_ViewProjMatrix_List, m_LightViewProjMatrixList.ToArray());

        return true;
    }

    void SetShadowCameraViewPort(ScriptableRenderContext context, CommandBuffer commandBuffer, int splitNum, int spiltIndex, int Resolution)
    {
        Vector2 offset = new Vector2(
            (spiltIndex % splitNum),
            (spiltIndex / splitNum)
        );

        Rect rect = new Rect(offset.x * Resolution, offset.y * Resolution, Resolution, Resolution);

        commandBuffer.SetViewport(rect);
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
        SPassDescriptor passDescriptor, CRenderTarget GBufferRT, CRenderTarget ShadowMapRT)
    {
        // GBufferをセット
        SetRTTextures(commandBuffer, GBufferRT, true, "SRP_GBuffer_");

        // シャドウマップをセットする
        if (ShadowMapRT != null) SetRTTextures(commandBuffer, ShadowMapRT, true, "SRP_ShadowMap_");

        // カメラ情報セット
        SetCamera(commandBuffer, camera);

        // 各ライトボリュームの描画
        if (!DrawLights(context, commandBuffer, int.MaxValue)) return false;

        return true;
    }

    // 間接照明の描画
    public bool DrawDeferredIndirectLight(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera,
        SPassDescriptor passDescriptor, CRenderTarget GBufferRT)
    {
        // GBufferをセット
        SetRTTextures(commandBuffer, GBufferRT, true, "SRP_GBuffer_");

        // カメラ情報セット
        SetCamera(commandBuffer, camera);

        if (!DrawIndirectLight(context, commandBuffer)) return false;

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

        // 正弦定理より半径を求める
        float height = light.range;

        float spotAngle = math.radians(light.spotAngle) * 0.5f;
        float radius = (height / math.sin(math.PI * 0.5f - spotAngle)) * math.sin(spotAngle);

        // スポットライトのスケール行列を構築
        Matrix4x4 scaleMat = new Matrix4x4(
            new Vector4(radius, 0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, radius, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, height, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
        );

        // スポットライトは回転情報も大切
        Matrix4x4 worldMat = light.localToWorldMatrix * scaleMat;

        // 描画実行
        commandBuffer.DrawMesh(m_ConeMesh, worldMat, m_DeferredLightMat);

        // 描画終了
        commandBuffer.SetKeyword(CShaderGlobalKeywordList._LIGHT_SPOT, false);

        return true;
    }

    bool DrawIndirectLight(ScriptableRenderContext context, CommandBuffer commandBuffer)
    {
        // 描画実行
        commandBuffer.DrawMesh(m_FullScreenMesh, Matrix4x4.identity, m_DeferredIndirectLightMat);

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

    public void PrepareLightArray(ScriptableRenderContext context, CommandBuffer commandBuffer, bool PerObjLight)
    {
        m_VisibleDirectionalLightList.Clear();

        // ライトの最大数を決めておく
        const int MaxMainLightCount = 4;
        const int MaxSubLightCount = 64;

        // シェーダーには決まったサイズの配列しか渡せないのでここで決め打ちしておく
        Vector4[] MainLightDirList = new Vector4[MaxMainLightCount];
        Vector4[] MainLightColorList = new Vector4[MaxMainLightCount];

        Vector4[] SubLightPosList = new Vector4[MaxSubLightCount];
        Vector4[] SubLightColorList = new Vector4[MaxSubLightCount];
        Vector4[] SubLightDirList = new Vector4[MaxSubLightCount];
        Vector4[] SubLightAngleList = new Vector4[MaxSubLightCount];

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

                        PreparePointLight(light, NumOfSubLight++, ref SubLightPosList, ref SubLightColorList, ref SubLightDirList, ref SubLightAngleList);
                    }
                    break;

                case LightType.Spot:
                    if (NumOfSubLight < MaxSubLightCount)
                    {
                        // サブライトのみライトインデックスを設定する
                        // メインライトは固定で8個まで使用する
                        lightIndex = NumOfSubLight;

                        PrepareSpotLight(light, NumOfSubLight++, ref SubLightPosList, ref SubLightColorList, ref SubLightDirList, ref SubLightAngleList);
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
        commandBuffer.SetGlobalVectorArray(CShaderConstants.SRP_Foreground_SubLightAngleArray, SubLightAngleList);
    }

    void PrepareDirectionalLight(VisibleLight light, int LightIndex, ref Vector4[] LightDirList, ref Vector4[] LightColorList)
    {
        Vector4 lightPos, spotLightDir = new Vector4();
        CalcLightParam(light, out lightPos, out spotLightDir);

        LightDirList[LightIndex] = lightPos;
        LightColorList[LightIndex] = light.finalColor;

        m_VisibleDirectionalLightList.Add((light, LightIndex));
    }

    void PreparePointLight(VisibleLight light, int LightIndex, ref Vector4[] LightPosList, ref Vector4[] LightColorList, 
        ref Vector4[] LightDirList, ref Vector4[] LightAngleList)
    {
        Vector4 lightPos, spotLightDir = new Vector4();
        CalcLightParam(light, out lightPos, out spotLightDir);

        lightPos.w = 1.0f / Mathf.Max(0.0001f, light.range * light.range);

        LightPosList[LightIndex] = lightPos;
        LightColorList[LightIndex] = light.finalColor;
        LightDirList[LightIndex] = spotLightDir;
        LightAngleList[LightIndex] = new Vector4(0.0f, 1.0f);
    }

    void PrepareSpotLight(VisibleLight visibleLight, int LightIndex, ref Vector4[] LightPosList, ref Vector4[] LightColorList, 
        ref Vector4[] LightDirList, ref Vector4[] LightAngleList)
    {
        Vector4 lightPos, spotLightDir = new Vector4();
        CalcLightParam(visibleLight, out lightPos, out spotLightDir);

        lightPos.w = 1.0f / Mathf.Max(0.0001f, visibleLight.range * visibleLight.range);

        LightPosList[LightIndex] = lightPos;
        LightColorList[LightIndex] = visibleLight.finalColor;
        LightDirList[LightIndex] = spotLightDir;

        //
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.light.innerSpotAngle);
        float outterCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv = 1.0f / Mathf.Max(innerCos - outterCos, 0.001f);

        LightAngleList[LightIndex] = new Vector4(angleRangeInv, -outterCos * angleRangeInv);
    }

    void SetDeferredLight(ScriptableRenderContext context, CommandBuffer commandBuffer, VisibleLight visibleLight)
    {
        // ライト
        Vector4 lightPos, spotLightDir = new Vector4();
        CalcLightParam(visibleLight, out lightPos, out spotLightDir);

        lightPos.w = 1.0f / Mathf.Max(0.0001f, visibleLight.range * visibleLight.range);

        // 角度減衰用パラメーター
        Vector4 spotAngle = new Vector4();
        if(visibleLight.lightType == LightType.Spot)
        {
            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.light.innerSpotAngle);
            float outterCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
            float angleRangeInv = 1.0f / Mathf.Max(innerCos - outterCos, 0.001f);

            spotAngle = new Vector4(angleRangeInv, -outterCos * angleRangeInv);
        }
        else
        {
            spotAngle = new Vector4(0.0f, 1.0f);
        }

        commandBuffer.SetGlobalVector(CShaderConstants.SRP_Deferred_LightPos, lightPos);
        commandBuffer.SetGlobalColor(CShaderConstants.SRP_Deferred_LightColor, visibleLight.finalColor);
        commandBuffer.SetGlobalVector(CShaderConstants.SRP_Deferred_LightDir, spotLightDir);
        commandBuffer.SetGlobalVector(CShaderConstants.SRP_Deferred_SpotAngle, spotAngle);
    }

    void CalcLightParam(VisibleLight light, out Vector4 lightPos, out Vector4 soptLightDir)
    {
        lightPos = Vector4.zero;
        soptLightDir = Vector4.zero;

        var localToWorldMat = light.localToWorldMatrix;

        if (light.lightType == LightType.Directional)
        {
            // ワールド行列の2列目にライト方向が入っている
            Vector4 dir = localToWorldMat.GetColumn(2);
            lightPos = new Vector4(dir.x, dir.y, dir.z, 0.0f);
        }
        else
        {
            // そのほかはいつも通り平行移動成分から取得
            Vector4 pos = localToWorldMat.GetColumn(3);
            lightPos = new Vector4(pos.x, pos.y, pos.z, 0.0f);

            // スポットライト情報を計算
            if(light.lightType == LightType.Spot)
            {
                // ワールド行列の2列目にライト方向が入っている
                soptLightDir = localToWorldMat.GetColumn(2);
            }
        }
    }

    void SetCamera(CommandBuffer commandBuffer, Camera camera)
    {
        commandBuffer.SetGlobalVector(CShaderConstants.SRP_CameraPos, camera.transform.position);
    }

    public bool ExecuteCulling(ScriptableRenderContext context, Camera camera, SShadowDescriptor shadowDescriptor)
    {
        // ここで実際にカリングをしているというわけではないが、
        // カリング用のパラメーター(たぶんCULL_NONEとかCULL_FRONTみたいな)
        // やつが正常に取れればカリングがGraphics API側で正常に
        // 機能しているということを表し、処理を継続するかどうかを
        // 決めている？
        ScriptableCullingParameters p;
        if (camera.TryGetCullingParameters(out p))
        {
            // シャドウカメラの距離を指定
            p.shadowDistance = shadowDescriptor.Distance;

            //
            p.shadowNearPlaneOffset = shadowDescriptor.NearPlaneOffset;

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
        List<Vector3> positions = new List<Vector3>();
        List<int> indices = new List<int>();

        float row = 32.0f, column = 32.0f, rad = 1.0f;

        for (float i = 0.0f; i <= row; i++)
        {
            float r = math.PI / row * i;
            float ry = math.cos(r);
            float rr = math.sin(r);
            for (float ii = 0.0f; ii <= column; ii++)
            {
                float tr = math.PI2 / column * ii;
                float tx = rr * rad * math.cos(tr);
                float ty = ry * rad;
                float tz = rr * rad * math.sin(tr);

                positions.Add(new Vector3(tx, ty, tz));
            }
        }

        for (int i = 0; i < row; i++)
        {
            for (int ii = 0; ii < (int)column; ii++)
            {
                int r = ((int)column + 1) * i + ii;

                indices.Add((int)r);
                indices.Add((int)(r + 1));
                indices.Add((int)(r + (int)(column) + 2));
                indices.Add((int)(r));
                indices.Add((int)(r + (int)(column) + 2));
                indices.Add((int)(r + (int)(column) + 1));
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt16;
        mesh.vertices = positions.ToArray();
        mesh.triangles = indices.ToArray();

        return mesh;
    }

    static Mesh CreateConeMesh()
    {
        List<Vector3> positions = new List<Vector3>();
        List<int> indices = new List<int>();

        float height = 1.0f;
        float radius = 1.0f;
        float segment = 36.0f;

        // 側面
        {
            // 一番上
            positions.Add(new Vector3(0.0f, 0.0f, 0.0f));

            //
            float rate = math.PI2 / segment;
            for (float n = 0.0f; n < segment; n++)
            {
                float angle = n * rate;

                float x = math.cos(angle) * radius;
                float y = math.sin(angle) * radius;
                float z = height;

                positions.Add(new Vector3(x, y, z));
            }

            for (float n = 0.0f; n < segment; n++)
            {
                indices.Add(0);
                indices.Add((int) n + 1);

                if(n + 2.0 >= segment)
                {
                    indices.Add(1);
                }
                else
                {
                    indices.Add((int)n + 2);
                }
            }
        }

        // 底面
        // 蓋は閉めない

        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt16;
        mesh.vertices = positions.ToArray();
        mesh.triangles = indices.ToArray();

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
