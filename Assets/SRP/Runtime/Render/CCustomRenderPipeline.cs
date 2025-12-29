using srp.postprocess;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using srp.data;

#if UNITY_EDITOR
using Unity.VisualScripting;
#endif

namespace srp.render
{
    public class CCustomRenderPipeline : RenderPipeline
    {
        CCustomRenderer m_Renderer = null;

        public CCustomRenderPipeline(List<CPostProcessFeature> processFeatures)
        {
            m_Renderer = new CCustomRenderer(processFeatures);
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

                    // Unityネイティブ側に資材が自動破棄されて無効な形式になっているので資材をすべて作り直す
                    var ProcessFeatures = m_Renderer.GetProcessFeatures();
                    m_Renderer = new CCustomRenderer(ProcessFeatures);

                    return;
                }
            }
        }
    }
}