using UnityEngine;

namespace OneMoreSip
{
    /// <summary>
    /// The blue dorm entrance. Touching it ends the walk: below 40 Drunk Level the
    /// player skips the pee minigame and gets the "still anxious" ending; at 40+
    /// the pee minigame loads.
    /// </summary>
    public class DormTrigger : MonoBehaviour
    {
        private bool _triggered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered) return;
            if (other.GetComponent<PlayerController>() == null) return;

            _triggered = true;
            GameManager.Instance.ArriveAtDorm();
        }
    }
}
