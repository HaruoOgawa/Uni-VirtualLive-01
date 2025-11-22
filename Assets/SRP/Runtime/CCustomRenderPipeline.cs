using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace srp
{
    public class CCustomRenderPipeline : RenderPipeline
    {
        CCustomRenderer m_Renderer = new CCustomRenderer();

        public CCustomRenderPipeline(bool PerObjLight)
        {
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            // 現在のモードを見てメインカメラを事前に決定しておく
            Camera mainCamera = null;
            if (Application.isFocused)
            {
                // GameViewにフォーカスしている
                mainCamera = Camera.main;
            }
            else
            {
                // SceneViewにフォーカスしている
                mainCamera = SceneView.lastActiveSceneView.camera;
            }

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