using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class DynamicResolutionManager : MonoBehaviour
{
    public Text text1, text2;

    #region InnerClasses
    /// <summary>
    /// Direction for Scaling of RenderViewport
    /// </summary>
    private enum ScalingDirection
    {
        Increase,
        Decrease
    }
    #endregion

    #region Variables
    #region Constants
    /// <summary>
    /// Maximum Scale for RenderViewPort (1)
    /// </summary>
    private const float MAX_SCALE = 1.0f;
    /// <summary>
    /// Minimum (used) Scale for RenderViewPort (0.2)
    /// </summary>
    private const float MIN_SCALE = 0.2f;
    /// <summary>
    /// Step-Size when changing Scale
    /// </summary>
    private const float SCALE_STEP = 0.05f;
    /// <summary>
    /// Number of Samples for averaging
    /// </summary>
    private const int NUM_SAMPLES = 60;
    #endregion

    #region Editor
    /// <summary>
    /// Time (in seconds) between scaling operations
    /// </summary>
    [SerializeField]
    [Tooltip("Time (in seconds) between scaling operations")]
    private float updateTimer = 5f;
    /// <summary>
    /// TargetTime (in milliseconds) to aim for
    /// </summary>
    [SerializeField]
    [Tooltip("TargetTime (in milliseconds) to aim for")]
    private float targetMS = 13.5f;
    #endregion

    #region Private
    /// <summary>
    /// Current timer (until next scaling operation)
    /// </summary>
    private float currTimer = 10f;
    /// <summary>
    /// Whether Init has completed
    /// </summary>
    private bool initComplete;
    /// <summary>
    /// GPU timing (in milliseconds)
    /// </summary>
    private float GPUms
    {
        get
        {
#if UNITY_ANDROID
            return RenderTiming.instance.deltaTime * 1000;
#elif UNITY_IOS
            // TODO: Use average over some time
            FrameTiming[] timings = new FrameTiming[1];
            if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                return timings[0].gpuFrameTime;
            else return -1; // Invalid timing
#else
            return -1;
#endif
        }
    }
    /// <summary>
    /// RenderScale
    /// </summary>
    private float scale
    {
        get
        {
#if UNITY_ANDROID
            return XRSettings.renderViewportScale;
#elif UNITY_IOS
            return XRSettings.eyeTextureResolutionScale;
#else
            return -1; // Not supported
#endif
        }
        set
        {
#if UNITY_ANDROID
            XRSettings.renderViewportScale = value;
#elif UNITY_IOS
            XRSettings.eyeTextureResolutionScale = value;
#else
            // Not supported
#endif
        }
    }

    private float logScale
    {
        get
        {
            return XRSettings.renderViewportScale * XRSettings.eyeTextureResolutionScale;
        }
    }

    private float maxScale
    {
        get
        {
#if UNITY_ANDROID
            return MAX_SCALE;
#elif UNITY_IOS
            return MAX_SCALE * 2f;
#else
            return -1;
#endif
        }
    }
    private float minScale
    {
        get
        {
#if UNITY_ANDROID
            return MIN_SCALE;
#elif UNITY_IOS
            return MIN_SCALE * 2f;
#else
            return -1;
#endif
        }
    }
    /// <summary>
    /// Samples for GPU-timing
    /// </summary>
    private readonly Queue<float> msQueue = new Queue<float>(NUM_SAMPLES);
#endregion
#endregion

#region Methods
#region Public
    /// <summary>
    /// Freezes scaling for a length of time.
    /// <para>
    /// Use during e.g. a heavy load-operation to prevent that operation 
    /// from changing the Resolution
    /// </para>
    /// </summary>
    /// <param name="seconds">Time to freeze for (in seconds)</param>
    public void Freeze(float seconds)
    {
        if (currTimer <= 0)
            currTimer = updateTimer;
        else if (currTimer > updateTimer)
            currTimer = updateTimer;
        currTimer += seconds;
    }
    /// <summary>
    /// Sets timer to 0, to run scaling next frame
    /// </summary>
    /// <param name="nextFrame">Next Frame or current frame</param>
    public void Check(bool nextFrame)
    {
        currTimer = nextFrame ? 0.014f : 0f;
    }
#endregion

#region Unity
    /// <summary>
    /// Called when Object is created. Starts Initialization
    /// </summary>
    private void Awake()
    {
        XRSettings.eyeTextureResolutionScale = 2.0f; // To allow for upscaling
#if UNITY_ANDROID
        XRSettings.renderViewportScale = 0.5f; // 0.5f * 2.0f = 1.0f
#else
        XRSettings.renderViewportScale = 1f;
#endif
        if (PlayerPrefs.HasKey("DRS"))
            scale = PlayerPrefs.GetFloat("DRS");
#if UNITY_ANDROID
        StartCoroutine(AwaitInit());
#elif UNITY_IOS
        initComplete = true;
#else
        Debug.LogError("Not Supported");
#endif

    }
    /// <summary>
    /// Called when Object is Destroyed. Saves current scaling-value to PlayerPrefs
    /// </summary>
    private void OnDestroy()
    {
        if (initComplete) // Supported & Working
            PlayerPrefs.SetFloat("DRS", scale);
    }
    /// <summary>
    /// Runs every frame. Checks timer and applies new scaling
    /// </summary>
    private void Update()
    {
        if (!initComplete)
            return;
#if UNITY_IOS
        FrameTimingManager.CaptureFrameTimings();
#endif
        float time = GPUms;
        if (!float.IsNaN(time))
            msQueue.Enqueue(time);
        while (msQueue.Count > NUM_SAMPLES)
            msQueue.Dequeue();
        currTimer -= Time.deltaTime;
        if (currTimer < 0)
        {
            currTimer = updateTimer;
            if (msQueue.Count.Equals(0))
                return; // No timings
            //float timing = msQueue.Average();
            float timing = 14.6f;
            if (timing <= 0 || float.IsNaN(timing))
                return; // Invalid Timing
            if (timing <= (targetMS - 1f) && scale < MAX_SCALE)
                ChangeResolution(ScalingDirection.Increase);
            else if (timing >= (targetMS + 1f) && scale > MIN_SCALE)
                ChangeResolution(ScalingDirection.Decrease);
            Debug.Log("Average: " + timing + "   number of samples: " + msQueue.Count + "  New Scale: " + logScale);
            text1.text = logScale.ToString();
        }
        text2.text = msQueue.Average().ToString(); 
    }
#endregion

#region Private
#if UNITY_ANDROID
    /// <summary>
    /// Awaits Init of RenderTiming
    /// </summary>
    private IEnumerator AwaitInit()
    {
        yield return null;
        yield return new WaitUntil(() => !RenderTiming.instance.enabled || RenderTiming.instance.isInitialized);
        if (!RenderTiming.instance.isSupported)
        {
            Debug.LogWarning("RenderTiming not supported");
            Destroy(this); // Not Supported;
        }
        initComplete = true;
    }
#endif
    /// <summary>
    /// Updates Resolution, by changing RenderViewPortScale
    /// </summary>
    /// <param name="dir">Direction to move in</param>
    private void ChangeResolution(ScalingDirection dir)
    {
        float curr = scale;
        curr += (dir == ScalingDirection.Increase ? SCALE_STEP : -SCALE_STEP);
        curr = Mathf.Clamp(curr, MIN_SCALE, MAX_SCALE);
        // Final scale for rendering = RenderViewPortScale * EyeTextureResolutionScale
        scale = curr;
    }
#endregion
#endregion
}