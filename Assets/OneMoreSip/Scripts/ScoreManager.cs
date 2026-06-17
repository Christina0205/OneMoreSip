using UnityEngine;

namespace OneMoreSip
{
    public enum PeeTargetType { Painting, Urinal, Trash, Floor, Miss }

    /// <summary>
    /// Central scoring for the pee minigame. Applies point values and tracks floor hits.
    /// Painting +50, Urinal +20, Trash +10. Anything else (Floor or a missed Wall) is -10.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void RegisterHit(PeeTargetType type)
        {
            var gm = GameManager.Instance;
            switch (type)
            {
                case PeeTargetType.Painting: gm.PeeScore += 50; break;
                case PeeTargetType.Urinal:   gm.PeeScore += 20; break;
                case PeeTargetType.Trash:    gm.PeeScore += 10; break;
                case PeeTargetType.Floor:
                    gm.PeeScore -= 10;
                    gm.FloorHits += 1;
                    break;
                case PeeTargetType.Miss:   // hit a wall / anywhere that isn't a target
                    gm.PeeScore -= 10;
                    break;
            }
        }
    }
}
