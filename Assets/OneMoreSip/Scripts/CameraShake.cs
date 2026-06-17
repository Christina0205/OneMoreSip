using UnityEngine;

namespace OneMoreSip
{
    /// <summary>
    /// Custom (Cinemachine-free) camera shake. A base position is supplied each frame
    /// by the camera-follow logic; this component layers a small noise offset on top.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        private Vector3 _base;
        private float _timer;
        private float _amplitude;
        private float _seedX, _seedY;

        private void Awake()
        {
            _base = transform.position;
            _seedX = Random.value * 100f;
            _seedY = Random.value * 100f;
        }

        /// <summary>Where the camera should sit before shake is applied.</summary>
        public void SetBasePosition(Vector3 p) => _base = new Vector3(p.x, p.y, _base.z != 0 ? _base.z : p.z);

        public void Shake(float duration, float amplitude = 0.12f)
        {
            _timer = Mathf.Max(_timer, duration);
            _amplitude = amplitude;
        }

        public void StopShake() => _timer = 0f;

        private void LateUpdate()
        {
            Vector3 offset = Vector3.zero;
            if (_timer > 0f)
            {
                _timer -= Time.deltaTime;
                float t = Time.time * 18f; // shake frequency
                // Perlin noise mapped to [-1,1] for smooth, non-jarring movement.
                float nx = (Mathf.PerlinNoise(_seedX, t) - 0.5f) * 2f;
                float ny = (Mathf.PerlinNoise(_seedY, t) - 0.5f) * 2f;
                offset = new Vector3(nx, ny, 0f) * _amplitude;
            }
            transform.position = _base + offset;
        }
    }
}
