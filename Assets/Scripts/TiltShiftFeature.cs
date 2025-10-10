using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


[System.Serializable]
public class TiltShiftSettings
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    public Material tiltShiftMaterial = null; // assign material using Hidden/URP/TiltShift shader
    [Range(0f, 1f)] public float focusPos = 0.5f;
    [Range(0.01f, 1f)] public float focusRange = 0.15f;
    [Range(0f, 10f)] public float blurAmount = 2.5f;
    [Range(1, 16)] public int samples = 8;
    [Range(-90f, 90f)] public float tiltAngle = 0f;
    public int downsample = 2; // 1 = half res, 2 = quarter res, etc. (downsample factor)
}


public class TiltShiftFeature : ScriptableRendererFeature
{
    public TiltShiftSettings settings = new TiltShiftSettings();


    TiltShiftPass pass;


    public override void Create()
    {
        pass = new TiltShiftPass(settings);
        pass.renderPassEvent = settings.renderPassEvent;
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.tiltShiftMaterial == null) return;

        // DON'T fetch renderer.cameraColorTargetHandle here — it's not safe yet.
        // Just enqueue the pass; the pass will fetch the camera target during Execute().
        renderer.EnqueuePass(pass);
    }

}