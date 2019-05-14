using System.Collections;
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
    private float targetMS = 14.5f;
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
    #endregion
    #endregion

    #region Methods
    #region Public
    /// <summary>
    /// Freezes scaling for a length of time. 
    /// Use during e.g. a heavy load-operation to prevent that operation 
    /// from changing the Resolution
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
        XRSettings.renderViewportScale = 0.5f; // 0.5f * 2.0f = 1.0f
        if (PlayerPrefs.HasKey("DRS"))
            XRSettings.renderViewportScale = PlayerPrefs.GetFloat("DRS");
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
            PlayerPrefs.SetFloat("DRS", XRSettings.renderViewportScale);
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
        currTimer -= Time.deltaTime;
        if (currTimer < 0)
        {
            currTimer = updateTimer;
            float timing = GPUms;
            if (timing < 0)
                return; // Invalid Timing
            if (timing <= (targetMS - 1f) && XRSettings.renderViewportScale < MAX_SCALE)
                ChangeResolution(ScalingDirection.Increase);
            else if (timing >= (targetMS + 1f) && XRSettings.renderViewportScale > MIN_SCALE)
                ChangeResolution(ScalingDirection.Decrease);
        }
        text1.text = (XRSettings.renderViewportScale * XRSettings.eyeTextureResolutionScale).ToString();
        text2.text = GPUms.ToString();
    }
    #endregion

    #region Private
#if UNITY_ANDROID
    /// <summary>
    /// Awaits Init of RenderTiming
    /// </summary>
    private IEnumerator AwaitInit()
    {
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
        float curr = XRSettings.renderViewportScale;
        curr += (dir == ScalingDirection.Increase ? SCALE_STEP : -SCALE_STEP);
        curr = Mathf.Min(Mathf.Max(curr, MIN_SCALE), MAX_SCALE);
        // Final scale for rendering = RenderViewPortScale * EyeTextureResolutionScale
        XRSettings.renderViewportScale = curr;
    }
    #endregion
    #endregion
}