using UnityEngine;

/// <summary>
/// 2D side-view camera that follows the player horizontally, but is clamped to the
/// background's left/right edges.
///
/// Because the camera's left edge can never go past the background's left edge:
///  - At the start (player at the far-left of the background) the camera is pinned
///    left, so the player appears at the left of the screen.
///  - As the player walks right they move toward screen-center; the moment they reach
///    center the clamp releases and the camera starts following, keeping them centered.
///  - Near the end the camera pins to the background's right edge the same way.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("The player (or whatever the camera should follow).")]
    [SerializeField] private Transform target;

    [Tooltip("SpriteRenderer of the background image. Its left/right edges become the camera limits.")]
    [SerializeField] private SpriteRenderer background;

    [Header("Options")]
    [Tooltip("0 = snap instantly. Larger = smoother / laggier follow.")]
    [SerializeField] private float smoothTime = 0.12f;

    [Tooltip("If true the camera also follows the player vertically; otherwise Y stays fixed.")]
    [SerializeField] private bool followY = false;

    private Camera _cam;
    private float _fixedY;
    private Vector3 _velocity;

    // ---- Drunk "woozy" sway (Effect 1) ----
    private float _swayTimer;
    private float _swayAmp;
    private float _swayAngle;

    /// <summary>
    /// Start a gentle, dizzy sway (low-frequency drift + slight tilt) for the camera.
    /// This is intentionally NOT an earthquake shake.
    /// </summary>
    public void DrunkSway(float duration, float amplitude = 0.18f, float tiltDegrees = 1.5f)
    {
        _swayTimer = Mathf.Max(_swayTimer, duration);
        _swayAmp = amplitude;
        _swayAngle = tiltDegrees;
    }

    public void StopSway()
    {
        _swayTimer = 0f;
        transform.rotation = Quaternion.identity;
    }

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _fixedY = transform.position.y; // remember the starting height for side-view
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Camera half-size in world units (depends on orthographic size + screen aspect).
        float halfHeight = _cam.orthographicSize;
        float halfWidth = halfHeight * _cam.aspect;

        // Desired X = follow the player...
        float desiredX = target.position.x;

        // ...but clamp so the view never shows past the background's left/right edges.
        if (background != null)
        {
            Bounds b = background.bounds;
            float minX = b.min.x + halfWidth;   // left limit  (camera center)
            float maxX = b.max.x - halfWidth;   // right limit (camera center)

            if (minX > maxX)
                desiredX = (b.min.x + b.max.x) * 0.5f; // background narrower than view: center it
            else
                desiredX = Mathf.Clamp(desiredX, minX, maxX);
        }

        float desiredY = followY ? target.position.y : _fixedY;
        Vector3 desired = new Vector3(desiredX, desiredY, transform.position.z);

        // Woozy sway on top of the follow position (smooth, low-frequency = dizzy, not jarring).
        if (_swayTimer > 0f)
        {
            _swayTimer -= Time.deltaTime;
            float t = Time.time;
            desired.x += Mathf.Sin(t * 2.3f) * _swayAmp;
            desired.y += Mathf.Sin(t * 1.7f + 1.3f) * _swayAmp * 0.6f;
            float ang = Mathf.Sin(t * 1.9f) * _swayAngle;     // slight head-tilt wobble
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
            if (_swayTimer <= 0f) transform.rotation = Quaternion.identity;
        }

        // Smooth follow (set smoothTime to 0 in the Inspector for an instant snap).
        transform.position = (smoothTime > 0f)
            ? Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime)
            : desired;
    }
}
