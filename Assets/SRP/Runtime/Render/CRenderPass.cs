using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace srp.render
{
    public class CRenderPass : ARenderPass
    {
        public CRenderPass(string PassName) : base(PassName)
        {
        }

        public override bool Begin(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera,
            bool reflect, bool clearColor = true, bool clearDepth = true)
        {
            // カメラのビュープロジェクション行列を設定する
            context.SetupCameraProperties(camera);

            if (m_RenderTarget != null)
            {
                if (!m_RenderTarget.IsValid()) return false;

                commandBuffer.SetRenderTarget(m_RenderTarget.GetColorRTIdentifiers().ToArray(), m_RenderTarget.GetDepthRTIdentifier());
                //commandBuffer.BeginRenderPass(m_Width, m_Height, 1, );
            }

            // フレームバッファ(フレームテクスチャ)の初期化コマンドを発行
            commandBuffer.ClearRenderTarget(clearDepth, clearColor, UnityEngine.Color.clear);

            // プロファイラ(例えばFrame Debugger)への記録開始
            commandBuffer.BeginSample(BufferName);

            // プロファイラ開始コマンドをコンテキストに登録
            ExecuteBuffer(context, commandBuffer, camera.name);

            // 反射描画の時は裏面表示されてしまうのでカリングの扱いを反転させる
            if (reflect) GL.invertCulling = true;

            return true;
        }

        public override bool End(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera, bool reflect)
        {
            // レンダーパス終了
            if (m_RenderTarget != null)
            {
                if (!m_RenderTarget.IsValid()) return false;
                //commandBuffer.EndRenderPass();
            }

            // これまでの描画コマンドをコンテキストに登録
            ExecuteBuffer(context, commandBuffer, m_PassName);

            // プロファイラの記録終了
            commandBuffer.EndSample(BufferName);

            // プロファイラ終了コマンドをコンテキストに登録
            ExecuteBuffer(context, commandBuffer, camera.name);

            // コンテキストに積み上げられたコマンドを全て実行する
            context.Submit();

            // 全描画が終了したので元に戻す
            // コマンドのサブミット後に解除しないと全てなかったことになってしまうのでSubmit関数の後に実行している
            if (reflect) GL.invertCulling = false;

            return true;
        }
    }
}
