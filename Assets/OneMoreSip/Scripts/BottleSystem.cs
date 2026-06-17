using System.Collections;
using UnityEngine;

namespace OneMoreSip
{
    /// <summary>
    /// Holds the currently-carried bottle and runs the drinking + hiding logic.
    /// Lives on the Player. Drinking takes 3 seconds; each completed drink adds
    /// +2 Drunk Level and removes 2 from Bottle Progress (each bottle = 20).
    /// </summary>
    public class BottleSystem : MonoBehaviour
    {
        public const float MaxBottle = 20f;
        public const float BaseDrinkDuration = 3f;
        public const float BaseHideDuration = 5f;
        public const float HideCooldown = 1f;

        // ---- State (read by UI / NPC) ----
        public bool Holding { get; private set; }
        public float BottleProgress { get; private set; }
        public bool IsDrinking { get; private set; }
        public bool IsHidden { get; private set; }

        /// <summary>True when the bottle can be seen by NPCs.</summary>
        public bool VisibleBottle => Holding && !IsHidden;

        // ---- Drunk-effect multipliers (set by DrunkEffectManager, Effect 5) ----
        public float DrinkDurationMultiplier = 1f;
        public float HideDurationMultiplier = 1f;

        private bool _hideOnCooldown = false;
        private GameObject _heldVisual;

        // -------------------------------------------------------------------
        // Pickup (called by BottlePickup when the player touches a bottle)
        // -------------------------------------------------------------------
        public bool TryPickup(GameObject worldBottle)
        {
            if (Holding)
            {
                // Anti-waste: cannot grab a new bottle while one is unfinished.
                GameManager.Instance.UI.ShowNpcMessage(
                    "You must finish your current bottle first. Don't waste it.", 2.5f);
                return false;
            }

            Holding = true;
            BottleProgress = MaxBottle;
            Destroy(worldBottle);
            CreateHeldVisual();
            UpdateHeldVisual();
            return true;
        }

        // -------------------------------------------------------------------
        // Drinking
        // -------------------------------------------------------------------
        public void TryDrink()
        {
            if (!Holding || IsDrinking) return;   // cannot drink twice at once
            StartCoroutine(DrinkRoutine());
        }

        private IEnumerator DrinkRoutine()
        {
            IsDrinking = true;
            float duration = BaseDrinkDuration * DrinkDurationMultiplier;
            yield return new WaitForSeconds(duration);

            // Apply the drink once the animation completes (+5 drunk, -5 bottle per sip).
            GameManager.Instance.DrunkLevel = Mathf.Min(100f, GameManager.Instance.DrunkLevel + 5f);
            BottleProgress -= 5f;

            if (BottleProgress <= 0f)
            {
                BottleProgress = 0f;
                DropBottle();
            }
            IsDrinking = false;
        }

        private void DropBottle()
        {
            Holding = false;
            IsHidden = false;
            if (_heldVisual != null) Destroy(_heldVisual);
        }

        // -------------------------------------------------------------------
        // Hiding
        // -------------------------------------------------------------------
        public void TryHide()
        {
            if (!Holding || IsHidden || _hideOnCooldown) return;
            StartCoroutine(HideRoutine());
        }

        private IEnumerator HideRoutine()
        {
            IsHidden = true;
            UpdateHeldVisual();

            float duration = BaseHideDuration * HideDurationMultiplier;
            yield return new WaitForSeconds(duration);

            IsHidden = false;
            UpdateHeldVisual();

            // 1 second cooldown before it can be hidden again.
            _hideOnCooldown = true;
            yield return new WaitForSeconds(HideCooldown);
            _hideOnCooldown = false;
        }

        // -------------------------------------------------------------------
        // Held bottle visual (small green square above the player)
        // -------------------------------------------------------------------
        private void CreateHeldVisual()
        {
            _heldVisual = new GameObject("HeldBottle");
            _heldVisual.transform.SetParent(transform);
            _heldVisual.transform.localPosition = new Vector3(0.35f, 0.4f, 0f);
            _heldVisual.transform.localScale = new Vector3(0.35f, 0.6f, 1f);
            var sr = _heldVisual.AddComponent<SpriteRenderer>();
            sr.sprite = PrimitiveFactory.Square;
            sr.color = new Color(0.20f, 0.75f, 0.25f);
            sr.sortingOrder = 3;
        }

        private void UpdateHeldVisual()
        {
            if (_heldVisual == null) return;
            // When hidden, fade the bottle out so the player can tell it's concealed.
            var sr = _heldVisual.GetComponent<SpriteRenderer>();
            sr.color = IsHidden
                ? new Color(0.20f, 0.75f, 0.25f, 0.15f)
                : new Color(0.20f, 0.75f, 0.25f, 1f);
        }
    }
}
