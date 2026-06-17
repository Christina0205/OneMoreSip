using System.Collections.Generic;
using UnityEngine;

namespace OneMoreSip
{
    /// <summary>
    /// Walking-mode player movement and action input.
    /// Controls: A = left, D = right, E = drink, Shift = hide bottle.
    /// Drunk effects modify movement via the public modifier fields, which the
    /// DrunkEffectManager toggles on/off.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public const float BaseSpeed = 4f;

        // ---- Drunk effect modifiers (set by DrunkEffectManager) ----
        public bool ReversedControls = false;     // Effect 3
        public float InputDelay = 0f;             // Effect 4 (seconds)
        public float SpeedMultiplier = 1f;        // Effect 5

        private BottleSystem _bottle;

        // Buffered input for the delay effect: (timestamp, direction).
        private struct TimedInput { public float t; public float dir; }
        private readonly List<TimedInput> _buffer = new List<TimedInput>();

        private void Awake()
        {
            _bottle = GetComponent<BottleSystem>();
        }

        private void Update()
        {
            HandleMovement();
            HandleActions();
        }

        private void HandleMovement()
        {
            // Raw horizontal from A/D.
            float raw = 0f;
            if (Input.GetKey(KeyCode.A)) raw -= 1f;
            if (Input.GetKey(KeyCode.D)) raw += 1f;
            if (ReversedControls) raw = -raw;

            // Record this frame's input, then read back the input from `InputDelay` ago.
            _buffer.Add(new TimedInput { t = Time.time, dir = raw });

            float applied = 0f;
            float cutoff = Time.time - InputDelay;
            for (int i = 0; i < _buffer.Count; i++)
            {
                if (_buffer[i].t <= cutoff) applied = _buffer[i].dir;
            }
            // Prune very old samples.
            _buffer.RemoveAll(s => s.t < Time.time - 2f);

            float speed = BaseSpeed * SpeedMultiplier;
            Vector3 pos = transform.position;
            pos.x += applied * speed * Time.deltaTime;
            pos.x = Mathf.Clamp(pos.x, 0.5f, GameManager.MapLength - 0.5f);
            transform.position = pos;
        }

        private void HandleActions()
        {
            if (Input.GetKeyDown(KeyCode.E))
                _bottle.TryDrink();

            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                _bottle.TryHide();
        }
    }
}
