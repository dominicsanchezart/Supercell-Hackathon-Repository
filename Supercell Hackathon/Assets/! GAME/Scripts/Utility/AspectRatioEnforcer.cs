using UnityEngine;

/// <summary>
/// Forces every camera to render at a 16:9 aspect ratio by adjusting
/// viewport rects. Adds letterbox (black bars top/bottom) or pillarbox
/// (black bars left/right) as needed.
///
/// Attach to any always-active GameObject (e.g. Main Camera or a
/// DontDestroyOnLoad manager). The component updates every frame so it
/// handles window resizes, orientation changes, and new cameras that
/// appear at runtime.
/// </summary>
public class AspectRatioEnforcer : MonoBehaviour
{
    [Tooltip("Target aspect ratio (width / height). Default is 16:9.")]
    [SerializeField] private float targetAspect = 16f / 9f;

    [Tooltip("Colour used to fill the letterbox / pillarbox bars.")]
    [SerializeField] private Color barColor = Color.black;

    private Camera _bgCamera;          // renders the bars
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private void Awake()
    {
        CreateBackgroundCamera();
        Apply();
    }

    private void Update()
    {
        // Only recalculate when the screen size actually changes.
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            Apply();
    }

    /// <summary>
    /// Recalculate and apply the viewport rect to every active camera.
    /// </summary>
    private void Apply()
    {
        _lastScreenWidth  = Screen.width;
        _lastScreenHeight = Screen.height;

        float currentAspect = (float)Screen.width / Screen.height;
        float scaleHeight   = currentAspect / targetAspect;

        Rect viewportRect;

        if (scaleHeight < 1f)
        {
            // Screen is taller than 16:9 → letterbox (bars top & bottom)
            viewportRect = new Rect(0f, (1f - scaleHeight) / 2f, 1f, scaleHeight);
        }
        else
        {
            // Screen is wider than 16:9 → pillarbox (bars left & right)
            float scaleWidth = 1f / scaleHeight;
            viewportRect = new Rect((1f - scaleWidth) / 2f, 0f, scaleWidth, 1f);
        }

        // Apply to every camera except our background bar camera.
        foreach (Camera cam in Camera.allCameras)
        {
            if (cam == _bgCamera) continue;
            cam.rect = viewportRect;
        }
    }

    /// <summary>
    /// Creates a low-depth camera that clears to <see cref="barColor"/>
    /// so the letterbox / pillarbox bars are filled.
    /// </summary>
    private void CreateBackgroundCamera()
    {
        // Reuse if already present (e.g. scene reload).
        if (_bgCamera != null) return;

        var go = new GameObject("_AspectRatio_BG_Camera");
        go.transform.SetParent(transform);
        _bgCamera                  = go.AddComponent<Camera>();
        _bgCamera.depth            = -100;
        _bgCamera.cullingMask      = 0;           // renders nothing
        _bgCamera.clearFlags       = CameraClearFlags.SolidColor;
        _bgCamera.backgroundColor  = barColor;
        _bgCamera.orthographic     = true;
        _bgCamera.rect             = new Rect(0f, 0f, 1f, 1f); // full screen
    }
}
