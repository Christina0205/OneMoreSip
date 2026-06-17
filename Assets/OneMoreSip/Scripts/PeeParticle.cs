using UnityEngine;

namespace OneMoreSip
{
    /// <summary>
    /// A single yellow pee droplet. Has a Rigidbody2D + Collider2D, lives 3 seconds,
    /// and disappears on hitting a target, hitting the floor, or expiring.
    /// </summary>
    public class PeeParticle : MonoBehaviour
    {
        public const float Lifetime = 3f;

        private void Start()
        {
            Destroy(gameObject, Lifetime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var target = other.GetComponent<PeeTarget>();
            if (target != null)
            {
                target.RegisterHit();   // cooldown handled inside the target
                Destroy(gameObject);    // particle always disappears on contact
            }
        }
    }
}
