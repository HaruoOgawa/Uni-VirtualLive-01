using srp.postprocess;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace srp
{
    public class CCustomRenderPipeline : RenderPipeline
    {
        CCustomRenderer m_Renderer = null;

        public CCustomRenderPipeline(SRenderSettings settings, List<CPostProcessFeature> processFeatures)
        {
            m_Renderer = new CCustomRenderer(settings, processFeatures);
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            // 現在のモードを見てメインカメラを事前に決定しておく
            Camera mainCamera = null;

#if UNITY_EDITOR
            if (SceneView.lastActiveSceneView.IsFocused())
            {
                // SceneViewにフォーカスしている
                mainCamera = SceneView.lastActiveSceneView.camera;
            }
            else
            {
                // GameViewにフォーカスしている
                mainCamera = Camera.main;
            }
#else
            // 常にGameViewにフォーカス
            mainCamera = Camera.main;
#endif // UNITY_EDITOR

            // 描画実行
            for (int i = 0; i < cameras.Count; i++)
            {
                var camera = cameras[i];
                 
                if (!m_Renderer.Render(context, camera, mainCamera))
                {
                    Debug.LogErrorFormat("[CCustomRenderPipeline] {0} camera failed to render.", camera.name);
                    continue;
                }
            }
        }
    }
}