using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule; // CRUCIAL: The new Unity 6 API

public class PauseScreenCaptureFeature : ScriptableRendererFeature
{
    class CapturePass : ScriptableRenderPass
    {
        private RTHandle _destinationHandle;
        private bool _shouldCapture = false;

        public CapturePass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public void RequestCapture(RTHandle destination)
        {
            _destinationHandle = destination;
            _shouldCapture = true;
        }

        // Data container for the Render Graph
        private class PassData
        {
            public TextureHandle sourceTexture;
        }

        // =================================================================
        // UNITY 6 RENDER GRAPH API (Replaces the obsolete Execute method)
        // =================================================================
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!_shouldCapture || _destinationHandle == null) return;

            // 1. Get the camera's current screen texture natively
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle cameraColor = resourceData.activeColorTexture;

            // 2. Import your Pause Menu's texture into the Render Graph
            TextureHandle destinationHandle = renderGraph.ImportTexture(_destinationHandle);

            // 3. Create a Raster Render Pass
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("PauseScreenCapture", out var passData))
            {
                passData.sourceTexture = cameraColor;
                
                // Read from the camera, Write to our pause menu texture
                builder.UseTexture(passData.sourceTexture);
                builder.SetRenderAttachment(destinationHandle, 0);

                // 4. Execute the copy!
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
                });
            }

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
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(_capturePass);
        }
    }

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