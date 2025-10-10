using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TiltShiftPass : ScriptableRenderPass
{
    const string k_RenderTag = "TiltShiftPass";

    TiltShiftSettings settings;
    Material mat;

    // input handle assigned from AddRenderPasses
    RTHandle source;

    // downsampled RTHandles used for blur
    RTHandle tmp0; // small RT
    RTHandle tmp1; // small RT

    RenderTextureDescriptor m_Descriptor;

    public TiltShiftPass(TiltShiftSettings settings)
    {
        this.settings = settings;
        this.renderPassEvent = settings.renderPassEvent;
        this.mat = settings.tiltShiftMaterial;
    }

    public void Setup(in RTHandle sourceHandle)
    {
        source = sourceHandle;
    }

    // Configure is called before Execute — allocate RTHandles here
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        int ds = Mathf.Max(1, settings.downsample);
        int w = Mathf.Max(1, cameraTextureDescriptor.width / (2 * ds));
        int h = Mathf.Max(1, cameraTextureDescriptor.height / (2 * ds));

        // Clone descriptor, but adjust resolution and disable depth buffer
        var desc = cameraTextureDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;
        desc.width = w;
        desc.height = h;

        // Allocate temporary RTHandles
        tmp0 = RTHandles.Alloc(desc, name: "_Tilt_tmp0", filterMode: FilterMode.Bilinear);
        tmp1 = RTHandles.Alloc(desc, name: "_Tilt_tmp1", filterMode: FilterMode.Bilinear);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (mat == null) return;

        // Try to get the camera color target handle inside Execute (safe scope)
        RTHandle camColorTarget = null;
        // renderingData.cameraData.renderer is available here; obtain its cameraColorTargetHandle
#if UNITY_2022_1_OR_NEWER
        // In modern URP this is available:
        camColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
#else
    // Older URP: fall back to cameraColorTarget RenderTargetIdentifier (if you used that earlier)
    // camColorTarget = ??? (you can handle older versions separately)
#endif

        if (camColorTarget == null)
        {
            // nothing to do — camera target not ready
            return;
        }

        var cmd = CommandBufferPool.Get(k_RenderTag);

        // Set params
        mat.SetFloat("_FocusPos", settings.focusPos);
        mat.SetFloat("_FocusRange", settings.focusRange);
        mat.SetFloat("_BlurAmount", settings.blurAmount);
        mat.SetInt("_Samples", Mathf.Clamp(settings.samples, 1, 32));
        float rad = settings.tiltAngle * Mathf.Deg2Rad;
        Vector2 tilt = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        mat.SetVector("_TiltDir", new Vector4(tilt.x, tilt.y, 0, 0));

        // 1) Downsample camera color -> tmp0
        Blitter.BlitCameraTexture(cmd, camColorTarget, tmp0);

        // 2) Horizontal blur: tmp0 -> tmp1
        mat.SetVector("_BlurDir", new Vector4(1f, 0f, 0f, 0f));
        Blitter.BlitTexture(cmd, tmp0, tmp1, mat, 0);

        // 3) Vertical blur: tmp1 -> tmp0
        mat.SetVector("_BlurDir", new Vector4(0f, 1f, 0f, 0f));
        Blitter.BlitTexture(cmd, tmp1, tmp0, mat, 0);

        // 4) Composite: tell shader which blurred RT to use, then composite into camera target
        cmd.SetGlobalTexture("_BlurTex", tmp0.nameID);
        Blitter.BlitCameraTexture(cmd, camColorTarget, camColorTarget, mat, 1);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    // Release RTHandles
    public override void FrameCleanup(CommandBuffer cmd)
    {
        if (tmp0 != null) { RTHandles.Release(tmp0); tmp0 = null; }
        if (tmp1 != null) { RTHandles.Release(tmp1); tmp1 = null; }
    }
}
