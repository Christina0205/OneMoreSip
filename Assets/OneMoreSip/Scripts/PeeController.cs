using System.Collections;
using UnityEngine;

namespace OneMoreSip
{
    /// <summary>
    /// Pee minigame controller. Aim with W/S, hold Space to fire a continuous stream
    /// of droplets (20/sec, machine-gun feel). Pee Amount = Drunk Level x 10, and each
    /// droplet consumes 1. When the amount runs out, the minigame ends.
    /// </summary>
    public class PeeController : MonoBehaviour
    {
        public const float MinAngle = -30f;
        public const float MaxAngle = 60f;
        public const float AimSpeed = 60f;       // degrees per second
        public const float FireRate = 20f;       // particles per second
        public const float ParticleSpeed = 14f;
        public const float ParticleGravity = 0.7f;

        public float AngleDeg { get; private set; } = 15f;
        public int PeeAmount { get; private set; }

        private float _fireAccumulator;
        private bool _ending;

        private void Awake()
        {
            // Score manager for this minigame.
            gameObject.AddComponent<ScoreManager>();

            // Pee Amount derived from how drunk the player is.
            PeeAmount = Mathf.RoundToInt(GameManager.Instance.DrunkLevel * 10f);
            if (PeeAmount <= 0) PeeAmount = 1;
        }

        private void Update()
        {
            HandleAim();
            HandleFire();
        }

        private void HandleAim()
        {
            float delta = 0f;
            if (Input.GetKey(KeyCode.W)) delta += 1f;
            if (Input.GetKey(KeyCode.S)) delta -= 1f;
            AngleDeg = Mathf.Clamp(AngleDeg + delta * AimSpeed * Time.deltaTime, MinAngle, MaxAngle);
        }

        private void HandleFire()
        {
            if (_ending) return;

            if (Input.GetKey(KeyCode.Space) && PeeAmount > 0)
            {
                // Accumulate fractional particles so the stream is smooth and rate-accurate.
                _fireAccumulator += FireRate * Time.deltaTime;
                while (_fireAccumulator >= 1f && PeeAmount > 0)
                {
                    _fireAccumulator -= 1f;
                    FireOne();
                    PeeAmount--;
                }
            }
            else
            {
                _fireAccumulator = 0f;
            }

            if (PeeAmount <= 0 && !_ending)
                StartCoroutine(EndAfterDelay());
        }

        private void FireOne()
        {
            float rad = AngleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            var p = PrimitiveFactory.CreateCircle("Pee", transform.position, 0.18f,
                new Color(0.95f, 0.9f, 0.2f), 4);

            var rb = p.AddComponent<Rigidbody2D>();
            rb.gravityScale = ParticleGravity;
            rb.linearVelocity = dir * ParticleSpeed;

            var col = p.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f; // sprite is unit circle scaled by 0.18

            p.AddComponent<PeeParticle>();
        }

        private IEnumerator EndAfterDelay()
        {
            _ending = true;
            // Let droplets already in the air land before showing the ending.
            yield return new WaitForSeconds(1.5f);
            GameManager.Instance.EndPeeMode();
        }
    }
}
