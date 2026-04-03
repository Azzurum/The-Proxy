using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PauseScreenCaptureFeature : ScriptableRendererFeature
{
    // The actual Render Pass that executes on the GPU
    class CapturePass : ScriptableRenderPass
    {
        private RTHandle _destinationHandle;
        private bool _shouldCapture = false;

        public CapturePass()
        {
            // We grab the frame right after the 3D world and 2D sprites are drawn.
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public void RequestCapture(RTHandle destination)
        {
            _destinationHandle = destination;
            _shouldCapture = true;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!_shouldCapture || _destinationHandle == null) return;

            // Get a command buffer from the pool
            CommandBuffer cmd = CommandBufferPool.Get("PauseScreenCapture");
            
            // Unity 6 Modern Blitter API (Do not use legacy cmd.Blit)
            RTHandle cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            Blitter.BlitCameraTexture(cmd, cameraTarget, _destinationHandle);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            // Toggle off so it only captures exactly one frame
            _shouldCapture = false; 
        }
    }

    private CapturePass _capturePass;
    private static PauseScreenCaptureFeature _instance;

    public override void Create()
    {
        _capturePass = new CapturePass();
        _instance = this;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Only run this on the main game camera
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(_capturePass);
        }
    }

    // Global static helper so our PauseManager can easily request a frame
    public static void CaptureScreen(RTHandle destination)
    {
        if (_instance != null && _instance._capturePass != null)
        {
            _instance._capturePass.RequestCapture(destination);
        }
        else
        {
            Debug.LogWarning("PauseScreenCaptureFeature is missing from your URP Renderer!");
        }
    }
}