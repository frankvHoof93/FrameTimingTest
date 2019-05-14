using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR;
using UnityEngine.XR;

public class FrameTimingDisp : MonoBehaviour
{
    private enum ScalingDirection
    {
        Increase,
        Decrease
    }

    private const float MAX_SCALE = 1.4f;
    private const float MIN_SCALE = 0.55f;
    private const float SCALE_STEP = 0.05f;

    private Text text;

    void Awake()
    {
        text = GetComponent<Text>();
        XRSettings.eyeTextureResolutionScale = MAX_SCALE; // To allow for upscaling
    }
    void Update()
    {
        FrameTimingManager.CaptureFrameTimings();
        FrameTiming[] timings = new FrameTiming[1];
        Debug.Log("Found " + FrameTimingManager.GetLatestTimings(1, timings) + " frames");
        Debug.Log("Frame 0 (GPU): " + timings[0].gpuFrameTime);
        Debug.Log("Frame 0 (CPU): " + timings[0].cpuFrameTime);
        Debug.Log("Frame 0 (CPU (comp)): " + timings[0].cpuTimeFrameComplete);
        Debug.Log("CPUFreq: " + FrameTimingManager.GetCpuTimerFrequency());
        Debug.Log("CPUFreq: " + FrameTimingManager.GetGpuTimerFrequency());
        Debug.Log("Vsync/Sec: " + FrameTimingManager.GetVSyncsPerSecond());
        Debug.Log("RefreshRate: " + XRDevice.refreshRate);
        Debug.Log("GPU Last Frame: " + XRStats.gpuTimeLastFrame);
        text.text = timings[0].gpuFrameTime.ToString();
    }


    private void ChangeResolution(ScalingDirection dir)
    {
        float curr = XRSettings.renderViewportScale;
        curr += (dir == ScalingDirection.Increase ? SCALE_STEP : -SCALE_STEP);
        curr = Mathf.Min(Mathf.Max(curr, MIN_SCALE), MAX_SCALE);
        XRSettings.renderViewportScale = curr;
    }
}
