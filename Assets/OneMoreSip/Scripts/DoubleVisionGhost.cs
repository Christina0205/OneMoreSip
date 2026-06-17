using UnityEngine;

namespace OneMoreSip
{
    /// <summary>
    /// A transparent duplicate spawned by the Double Vision effect. It starts offset
    /// from the original, drifts back toward it, fades out, then destroys itself -
    /// simulating the eyes regaining focus.
    /// </summary>
    public class DoubleVisionGhost : MonoBehaviour
    {
        private Transform _original;
        private SpriteRenderer _sr;
        private float _life;
        private float _age;
        private Vector3 _startOffset;

        public void Init(Transform original, float life)
        {
            _original = original;
            _life = life;
            _sr = GetComponent<SpriteRenderer>();
            _startOffset = transform.position - (original != null ? original.position : transform.position);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _life);

            // Drift the offset back to zero (toward the original object).
            Vector3 anchor = _original != null ? _original.position : transform.position - _startOffset;
            transform.position = anchor + _startOffset * (1f - t);

            // Fade out.
            if (_sr != null)
            {
                Color c = _sr.color;
                c.a = Mathf.Lerp(0.45f, 0f, t);
                _sr.color = c;
            }

            if (_age >= _life) Destroy(gameObject);
        }
    }
}
