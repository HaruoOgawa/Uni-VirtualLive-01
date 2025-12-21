using srp.postprocess;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace srp
{
    public class CCustomRenderer
    {
        int m_CurrentScreenWidth = 0;
        int m_CurrentScreenHeight = 0;

        SRenderSettings m_Settings;
        List<CPostProcessFeature> m_ProcessFeatures = new List<CPostProcessFeature>();

        // シーン
        CSceneController m_SceneController = null;

        // コマンドバッファ
        CommandBuffer m_CommandBuffer = null;

        // 最終的に画面に描画されるレンダーターゲット
        CRenderTarget m_FinalResultRT = null;

        // デファードレンダリング GBufferパス
        CRenderPass m_GBufferGenPass = null;

        // デファードレンダリング
        CRenderPass m_GBufferLightPass = null; // GBufferライティングパス
        CRenderPass m_GBufferIndirectLightPass = null; // GBuffer間接照明パス

        // フォアグラウンドレンダーパス
        CRenderPass m_ForegroundPass = null;

        // シャドウマップパス
        SShadowDescriptor m_ShadowDescriptor = null;
        CRenderPass m_ShadowMapPass = null;

        // ポストプロセス
        CPostProcess m_PostProcess = null;

        // 最終描画結果
        CRenderPass m_MainResultPass = null;

        public CCustomRenderer(SRenderSettings settings, List<CPostProcessFeature> processFeatures)
        {
            m_Settings = settings;
            m_ProcessFeatures = processFeatures;

            m_SceneController = new CSceneController();
            m_CommandBuffer = new CommandBuffer();
        }

        bool Create(int ScreenWidth, int ScreenHeight)
        {
            // FinalResultRT
            {
                m_FinalResultRT = new CRenderTarget();
                m_FinalResultRT.Create(ScreenWidth, ScreenHeight, 1, RenderTextureFormat.ARGBFloat, RenderTextureFormat.Depth, 24);
            }

            // ShadowMapPass
            {
                m_ShadowDescriptor = new SShadowDescriptor();
                m_ShadowMapPass = new CRenderPass("ShadowMapPass");

                // ShadowMap描画のRenderList API CreateShadowListは内部的に自動でShadowCasterのShaderPassのみが収集されるのでこれは不要
                //m_ShadowMapPass.AddShaderTag("ShadowCaster");

                m_ShadowDescriptor.Distance = 25.0f;
                m_ShadowDescriptor.Resolution = 4096;

                CRenderTarget renderTarget = new CRenderTarget();
                renderTarget.Create(m_ShadowDescriptor.Resolution, m_ShadowDescriptor.Resolution, 1, RenderTextureFormat.Shadowmap, RenderTextureFormat.Depth, 24);

                m_ShadowMapPass.SetRenderTarget(renderTarget);
            }

            // GBufferGenPass
            {
                m_GBufferGenPass = new CRenderPass("GBufferGenPass");

                m_GBufferGenPass.AddShaderTag("CustomGBufferGen");
                CRenderTarget renderTarget = new CRenderTarget();
                renderTarget.Create(ScreenWidth, ScreenHeight, 5, RenderTextureFormat.ARGBFloat, RenderTextureFormat.Depth, 24);

                m_GBufferGenPass.SetRenderTarget(renderTarget);
            }

            // GBufferLightPass
            {
                m_GBufferLightPass = new CRenderPass("GBufferLightPass"); // GBufferライティングパス

                m_GBufferLightPass.AddShaderTag("CustomGBufferLight");

                CRenderTarget renderTarget = new CRenderTarget();
                renderTarget.Create(ScreenWidth, ScreenHeight, 1, RenderTextureFormat.ARGBFloat, RenderTextureFormat.Depth, 24);

                m_GBufferLightPass.SetRenderTarget(renderTarget);
            }

            // GBufferIndirectLightPass
            {
                m_GBufferIndirectLightPass = new CRenderPass("GBufferIndirectLightPass"); // GBuffer間接照明パス

                m_GBufferIndirectLightPass.AddShaderTag("CustomGBufferIndirectLight");

                CRenderTarget renderTarget = new CRenderTarget();
                renderTarget.Create(ScreenWidth, ScreenHeight, 1, RenderTextureFormat.ARGBFloat, RenderTextureFormat.Depth, 24);

                m_GBufferIndirectLightPass.SetRenderTarget(renderTarget);
            }

            // ForegroundPass
            {
                m_ForegroundPass = new CRenderPass("ForegroundPass");

                m_ForegroundPass.AddShaderTag("SRPDefaultUnlit");
                m_ForegroundPass.AddShaderTag("SRPDefaultUnlit_Outline");
                m_ForegroundPass.AddShaderTag("Always");
                m_ForegroundPass.AddShaderTag("ForwardBase");
                m_ForegroundPass.AddShaderTag("PrepassBase");
                m_ForegroundPass.AddShaderTag("Vertex");
                m_ForegroundPass.AddShaderTag("VertexLMRGBM");
                m_ForegroundPass.AddShaderTag("VertexLM");

                CRenderTarget renderTarget = new CRenderTarget();
                renderTarget.Create(ScreenWidth, ScreenHeight, 1, RenderTextureFormat.ARGBFloat, RenderTextureFormat.Depth, 24);

                m_ForegroundPass.SetRenderTarget(renderTarget);
            }

            // MainResultPass
            {
                m_MainResultPass = new CRenderPass("MainResultPass");

                m_MainResultPass.AddShaderTag("SRPDefaultUnlit");
                m_MainResultPass.AddShaderTag("SRPDefaultUnlit_Outline");
                m_MainResultPass.AddShaderTag("Always");
                m_MainResultPass.AddShaderTag("ForwardBase");
                m_MainResultPass.AddShaderTag("PrepassBase");
                m_MainResultPass.AddShaderTag("Vertex");
                m_MainResultPass.AddShaderTag("VertexLMRGBM");
                m_MainResultPass.AddShaderTag("VertexLM");

                // RenderTargetを指定しなかったらUnity内部で現在のカメラの最終結果描画用フレームバッファがバインドされる
            }

            // PostProcess
            {
                m_PostProcess = new CPostProcess();
                if (!m_PostProcess.Create(ScreenWidth, ScreenHeight, m_ProcessFeatures)) return false;
            }

            return true;
        }

        void Release()
        {
            if(m_FinalResultRT != null)
            {
                m_FinalResultRT.Release();
                m_FinalResultRT = null;
            }

            if (m_GBufferGenPass != null)
            {
                m_GBufferGenPass.Release();
                m_GBufferGenPass = null;
            }

            if (m_GBufferLightPass != null)
            {
                m_GBufferLightPass.Release();
                m_GBufferLightPass = null;
            }

            if (m_GBufferIndirectLightPass != null)
            {
                m_GBufferIndirectLightPass.Release();
                m_GBufferIndirectLightPass = null;
            }

            if (m_ForegroundPass != null)
            {
                m_ForegroundPass.Release();
                m_ForegroundPass = null;
            }

            m_ShadowDescriptor = null;

            if (m_ShadowMapPass != null)
            {
                m_ShadowMapPass.Release();
                m_ShadowMapPass = null;
            }

            if (m_PostProcess != null)
            {
                m_PostProcess.Release(m_ProcessFeatures);
                m_PostProcess = null;
            }

            if (m_MainResultPass != null)
            {
                m_MainResultPass.Release();
                m_MainResultPass = null;
            }
        }

        public bool Render(ScriptableRenderContext context, Camera camera, Camera mainCamera)
        {
            // メインカメラの画面サイズが変わっていればリサイズを実行し、資材を再生する
            if (mainCamera.pixelWidth != m_CurrentScreenWidth || mainCamera.pixelHeight != m_CurrentScreenHeight)
            {
                if (!Resize(mainCamera.pixelWidth, mainCamera.pixelHeight)) return false;
            }

            // Scene Previewカメラはクラッシュしたり何かと問題が発生するのでスキップする
            if (camera.cameraType == CameraType.Preview) return true;

            // 平面反射用のカメラか
            bool IsPlannerReflection = (camera.tag == "PLANNER_REFLECT_CAMERA");

            // カメラを現在メインで使用中のカメラに対して鏡反射の位置に配置する
            if(IsPlannerReflection)
            {
                RecalcPlannerTransform(ref camera, mainCamera);
            }

            // カメラ位置に基づいてビューフラスタムカリングを実行
            if (!m_SceneController.ExecuteCulling(context, camera, m_ShadowDescriptor)) return false;

            // ライト情報を準備
            m_SceneController.PrepareLightArray(context, m_CommandBuffer, true);

            // シャドウマッピング
            {
                if (!m_ShadowMapPass.Begin(context, m_CommandBuffer, camera, IsPlannerReflection, true, true)) return false;
                if (!m_SceneController.DrawShadowMap(context, m_CommandBuffer, camera, m_ShadowDescriptor)) return false;
                if (!m_ShadowMapPass.End(context, m_CommandBuffer, camera, IsPlannerReflection)) return false;
            }

            // デファードレンダリング
            {
                // GBuffer生成
                {
                    SPassDescriptor descriptor = new SPassDescriptor();
                    descriptor.TargetShaderTags = m_GBufferGenPass.GetTargetShaderTags();

                    if (!m_GBufferGenPass.Begin(context, m_CommandBuffer, camera, IsPlannerReflection)) return false;
                    m_SceneController.Draw(context, m_CommandBuffer, camera, descriptor, null);
                    if (!m_GBufferGenPass.End(context, m_CommandBuffer, camera, IsPlannerReflection)) return false;
                }

                // GBufferライティング
                {
                    // デファードライトパスにGBufferパスの深度をコピーする
                    if (!m_GBufferLightPass.GetRenderTarget().CopyDepthBuffer(context, m_CommandBuffer, m_GBufferGenPass.GetRenderTarget())) return false;

                    SPassDescriptor descriptor = new SPassDescriptor();
                    descriptor.TargetShaderTags = m_GBufferLightPass.GetTargetShaderTags();

                    if (!m_GBufferLightPass.Begin(context, m_CommandBuffer, camera, false, true, false)) return false;
                    m_SceneController.DrawDeferredLight(context, m_CommandBuffer, camera, descriptor, m_GBufferGenPass.GetRenderTarget(), m_ShadowMapPass.GetRenderTarget());
                    if (!m_GBufferLightPass.End(context, m_CommandBuffer, camera, false)) return false;
                }

                // GBufferライティング(間接照明)
                {
                    // デファードライトパスにGBufferLightPassのカラー・深度をコピーする
                    if (!m_GBufferIndirectLightPass.GetRenderTarget().CopyFrameBuffer(context, m_CommandBuffer, m_GBufferLightPass.GetRenderTarget())) return false;

                    SPassDescriptor descriptor = new SPassDescriptor();
                    descriptor.TargetShaderTags = m_GBufferIndirectLightPass.GetTargetShaderTags();

                    if (!m_GBufferIndirectLightPass.Begin(context, m_CommandBuffer, camera, IsPlannerReflection, false, false)) return false;
                    m_SceneController.DrawDeferredIndirectLight(context, m_CommandBuffer, camera, descriptor, m_GBufferGenPass.GetRenderTarget());
                    if (!m_GBufferIndirectLightPass.End(context, m_CommandBuffer, camera, IsPlannerReflection)) return false;
                }
            }

            // フォアグラウンドレンダリング
            {
                // フォアグラウンドパス(ForegroundPass)にGBufferLightPassのカラー・深度をコピーする
                if (!m_ForegroundPass.GetRenderTarget().CopyFrameBuffer(context, m_CommandBuffer, m_GBufferIndirectLightPass.GetRenderTarget())) return false;

                SPassDescriptor descriptor = new SPassDescriptor();
                descriptor.TargetShaderTags = m_ForegroundPass.GetTargetShaderTags();
                descriptor.DrawSky = true;

                if (!m_ForegroundPass.Begin(context, m_CommandBuffer, camera, IsPlannerReflection, false, false)) return false;
                m_SceneController.Draw(context, m_CommandBuffer, camera, descriptor, m_ShadowMapPass.GetRenderTarget());
                m_SceneController.DrawGizmo(context, m_CommandBuffer, camera);
                if (!m_ForegroundPass.End(context, m_CommandBuffer, camera, IsPlannerReflection)) return false;
            }

            // リアルタイムGI
            {
                // 未対応
            }

            // ここまでの描画結果をいったん最終描画先にコピーしておく
            {
                if (!m_FinalResultRT.CopyFrameBuffer(context, m_CommandBuffer, m_ForegroundPass.GetRenderTarget())) return false;
            }

            // ポストプロセス
            if (!m_PostProcess.Draw(context, m_CommandBuffer, camera, m_FinalResultRT, m_SceneController, 
                m_Settings.PostProcessSettings, m_ProcessFeatures)) return false;

            // 最終描画結果
            {
                if (!m_MainResultPass.Begin(context, m_CommandBuffer, camera, IsPlannerReflection, true, true)) return false;
                m_SceneController.DrawFullScreenRT(context, m_CommandBuffer, camera, m_FinalResultRT.GetColorBuffer());
                if (!m_MainResultPass.End(context, m_CommandBuffer, camera, IsPlannerReflection)) return false;
            }

            return true;
        }

        bool Resize(int NewScreenWidth, int NewScreenHeight)
        {
            //Debug.LogFormat("OnResize / NewScreenWidth: {0}, NewScreenHeight: {1}", NewScreenWidth, NewScreenHeight);

            Release();

            // Screen.width、Screen.heightは想定外の値を返すことがあるのでカメラのプロジェクションからサイズを受け取るようにする
            m_CurrentScreenWidth = NewScreenWidth;
            m_CurrentScreenHeight = NewScreenHeight;

            if (!Create(m_CurrentScreenWidth, m_CurrentScreenHeight)) return false;

            return true;
        }

        // メインカメラに対して面対称な位置に移動させる
        void RecalcPlannerTransform(ref Camera ReflectCamera, Camera mainCamera)
        {
            if (mainCamera == null) return;

            // メインカメラと解像度が違えばリサイズして再生成
            if(mainCamera.pixelWidth != ReflectCamera.targetTexture.width || mainCamera.pixelHeight != ReflectCamera.targetTexture.height)
            {
                ReflectCamera.targetTexture.Release();

                ReflectCamera.targetTexture.width = mainCamera.pixelWidth;
                ReflectCamera.targetTexture.height = mainCamera.pixelHeight;

                ReflectCamera.targetTexture.Create();
            }

            // 平面反射用Plane
            Transform Plane = ReflectCamera.transform.parent;
            if (Plane == null) return;

            PlannerReflection component = null;
            if (Plane.TryGetComponent<PlannerReflection>(out component))
            {
                if(component.Plane != null)
                {
                    Plane = component.Plane.transform;
                }
            }

            Vector3 PlaneNormal = Plane.up;
            Vector3 PlanePos = Plane.position;
            float d = - Vector3.Dot(PlaneNormal, PlanePos);

            Matrix4x4 refMatrix = CalcReflectionMatrix(new Vector4(PlaneNormal.x, PlaneNormal.y, PlaneNormal.z, d));

            ReflectCamera.worldToCameraMatrix = mainCamera.worldToCameraMatrix * refMatrix;

            Vector3 cNormal = ReflectCamera.worldToCameraMatrix.MultiplyVector(PlaneNormal);
            Vector3 cPos = ReflectCamera.worldToCameraMatrix.MultiplyPoint(PlanePos);
            Vector4 ClipPlane = new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPos, cNormal));

            ReflectCamera.projectionMatrix = mainCamera.CalculateObliqueMatrix(ClipPlane);

            /*// メインカメラ情報
            Vector3 forward = mainCamera.transform.forward;
            Vector3 up = mainCamera.transform.up;
            Vector3 right = mainCamera.transform.right;
            Vector3 center = mainCamera.transform.position;

            // ワールド座標系から反射平面座標系に変換
            Vector3 PlannerForward = Plane.worldToLocalMatrix.MultiplyVector(forward);
            Vector3 PlannerUp = Plane.worldToLocalMatrix.MultiplyVector(up);
            Vector3 PlannerRight = Plane.worldToLocalMatrix.MultiplyVector(right);
            Vector3 PlannerCenter = Plane.worldToLocalMatrix.MultiplyPoint(center);

            // 反射平面を中心に面対称な位置に変換
            PlannerForward.y *= -1.0f;
            PlannerUp.y *= -1.0f;
            PlannerRight.y *= -1.0f;
            PlannerCenter.y *= -1.0f;

            // 反射平面座標系からワールド座標系に戻す
            PlannerForward = Plane.localToWorldMatrix.MultiplyVector(PlannerForward);
            PlannerUp = Plane.localToWorldMatrix.MultiplyVector(PlannerUp);
            PlannerRight = Plane.localToWorldMatrix.MultiplyVector(PlannerRight);
            PlannerCenter = Plane.localToWorldMatrix.MultiplyPoint(PlannerCenter);

            // Forward・Upを更新したら回転も更新されそうだが、なぜか変わらないのでピッチ回転も明示的に反転させる
            Vector3 PlannerEuler = mainCamera.transform.eulerAngles;
            PlannerEuler.x *= -1.0f;

            // 反射カメラに反射計算を行ったtransformを反映する
            ReflectCamera.transform.forward = PlannerForward;
            ReflectCamera.transform.up = PlannerUp;
            ReflectCamera.transform.right = Vector3.Cross(PlannerForward, PlannerUp);
            ReflectCamera.transform.position = PlannerCenter;
            ReflectCamera.transform.rotation = Quaternion.Euler(PlannerEuler);

            // その他情報もメインカメラに合わせる
            ReflectCamera.aspect = mainCamera.aspect;
            ReflectCamera.fieldOfView = mainCamera.fieldOfView;
            ReflectCamera.nearClipPlane = mainCamera.nearClipPlane;
            ReflectCamera.farClipPlane = mainCamera.farClipPlane;

            //
            Vector3 pNormal = (-1.0f) * Plane.up;
            Vector3 pPos = Plane.position;

            Vector3 cNormal = ReflectCamera.worldToCameraMatrix.MultiplyVector(pNormal);
            Vector3 cPos = ReflectCamera.worldToCameraMatrix.MultiplyPoint(pPos);

            Vector4 clipPlane = new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPos, cNormal));

            ReflectCamera.projectionMatrix = ReflectCamera.CalculateObliqueMatrix(clipPlane);*/
        }

        private Matrix4x4 CalcReflectionMatrix(Vector4 n)
        {
            var refMatrix = new Matrix4x4
            {
                m00 = 1f - 2f * n.x * n.x,
                m01 = -2f * n.x * n.y,
                m02 = -2f * n.x * n.z,
                m03 = -2f * n.x * n.w,
                m10 = -2f * n.x * n.y,
                m11 = 1f - 2f * n.y * n.y,
                m12 = -2f * n.y * n.z,
                m13 = -2f * n.y * n.w,
                m20 = -2f * n.x * n.z,
                m21 = -2f * n.y * n.z,
                m22 = 1f - 2f * n.z * n.z,
                m23 = -2f * n.z * n.w,
                m30 = 0F,
                m31 = 0F,
                m32 = 0F,
                m33 = 1F
            };

            return refMatrix;
        }
    }

}