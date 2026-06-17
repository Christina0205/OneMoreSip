using UnityEngine;

namespace OneMoreSip
{
    /// <summary>
    /// A scoring zone in the pee minigame (Painting / Urinal / Trash / Floor).
    /// Because particles arrive continuously, each target can only score once per
    /// 0.5 seconds to prevent score inflation.
    /// </summary>
    public class PeeTarget : MonoBehaviour
    {
        public const float ScoreCooldown = 0.5f;

        public PeeTargetType Type { get; private set; }
        private float _lastScoreTime = -10f;

        public void Init(PeeTargetType type) => Type = type;

        /// <summary>Called by a pee particle on contact. Returns true if it actually scored.</summary>
        public bool RegisterHit()
        {
            if (Time.time - _lastScoreTime < ScoreCooldown) return false;
            _lastScoreTime = Time.time;
            if (ScoreManager.Instance != null) ScoreManager.Instance.RegisterHit(Type);
            return true;
        }
    }
}
