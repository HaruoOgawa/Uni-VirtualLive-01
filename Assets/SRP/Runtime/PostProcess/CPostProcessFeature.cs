using UnityEngine;
using UnityEngine.Rendering;
using srp.render;

namespace srp.postprocess
{
    public abstract class CPostProcessFeature : ScriptableObject
    {
        public abstract void Release();
        public abstract bool Create(int ScreenWidth, int ScreenHeight);
        public abstract bool Draw(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera,
            CRenderTarget readRT, CRenderTarget writeRT, CSceneController sceneController);
    }
}