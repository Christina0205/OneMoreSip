using UnityEngine;

namespace OneMoreSip
{
    /// <summary>
    /// A green bottle sitting in the world. When the player touches it, it tries to
    /// hand itself to the BottleSystem (which enforces the anti-waste rule).
    /// </summary>
    public class BottlePickup : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var bottleSystem = other.GetComponent<BottleSystem>();
            if (bottleSystem == null) return;
            bottleSystem.TryPickup(gameObject);
        }
    }
}
