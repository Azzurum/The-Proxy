using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule; 

/// <summary>
/// A Universal Render Pipeline feature that safely captures the active camera output into a texture via the Render Graph API.
/// </summary>
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

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!_shouldCapture || _destinationHandle == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle cameraColor = resourceData.activeColorTexture;

            TextureHandle destinationHandle = renderGraph.ImportTexture(_destinationHandle);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("PauseScreenCapture", out var passData))
            {
                passData.sourceTexture = cameraColor;
                
                builder.UseTexture(passData.sourceTexture);
                builder.SetRenderAttachment(destinationHandle, 0);

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

    /// <summary>
    /// Queues a request to capture the current frame into the provided Render Texture destination.
    /// </summary>
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