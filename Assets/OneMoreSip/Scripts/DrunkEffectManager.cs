using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OneMoreSip
{
    /// <summary>
    /// Once Drunk Level reaches 30, picks a random drunk effect every 10 seconds.
    /// Each effect lasts 4 seconds, only one is active at a time, and everything
    /// returns to normal afterwards.
    /// </summary>
    public class DrunkEffectManager : MonoBehaviour
    {
        public const float DrunkThreshold = 30f;
        public const float EffectDuration = 4f;
        public const float CycleInterval = 6f;

        public string CurrentEffectName { get; private set; } = "None";

        private PlayerController _player;
        private BottleSystem _bottle;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _bottle = GetComponent<BottleSystem>();
            StartCoroutine(EffectLoop());
        }

        private IEnumerator EffectLoop()
        {
            while (true)
            {
                if (GameManager.Instance.DrunkLevel >= DrunkThreshold)
                {
                    int effect = Random.Range(1, 6); // 1..5
                    yield return StartCoroutine(RunEffect(effect));
                    // Remainder of the 10s cycle spent sober.
                    yield return new WaitForSeconds(CycleInterval - EffectDuration);
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        private IEnumerator RunEffect(int effect)
        {
            switch (effect)
            {
                case 1: CurrentEffectName = "Screen Shake"; ScreenShake(); break;
                case 2: CurrentEffectName = "Double Vision"; DoubleVision(); break;
                case 3: CurrentEffectName = "Reversed Controls"; _player.ReversedControls = true; break;
                case 4: CurrentEffectName = "Input Delay"; _player.InputDelay = 0.5f; break;
                case 5: CurrentEffectName = "Slow Actions"; ApplySlow(); break;
            }

            yield return new WaitForSeconds(EffectDuration);

            // Reset everything to normal.
            _player.ReversedControls = false;
            _player.InputDelay = 0f;
            _player.SpeedMultiplier = 1f;
            _bottle.DrinkDurationMultiplier = 1f;
            _bottle.HideDurationMultiplier = 1f;
            GameManager.Instance.Shake.StopShake();
            CurrentEffectName = "None";
        }

        // ---- Effect 1 ----
        private void ScreenShake()
        {
            // Small, comfortable shake for the full effect duration.
            GameManager.Instance.Shake.Shake(EffectDuration, 0.10f);
        }

        // ---- Effect 2 ----
        private void DoubleVision()
        {
            // Duplicate every NPC (and any target) as a fading ghost.
            var targets = new List<SpriteRenderer>();
            foreach (var npc in FindObjectsOfType<NPCDetection>())
                targets.Add(npc.GetComponent<SpriteRenderer>());

            foreach (var sr in targets)
            {
                if (sr == null) continue;
                var ghost = new GameObject("Ghost");
                ghost.transform.position = sr.transform.position + new Vector3(0.9f, 0.4f, 0f);
                ghost.transform.localScale = sr.transform.lossyScale;
                var gsr = ghost.AddComponent<SpriteRenderer>();
                gsr.sprite = sr.sprite;
                gsr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.45f);
                gsr.sortingOrder = sr.sortingOrder + 1;
                ghost.AddComponent<DoubleVisionGhost>().Init(sr.transform, EffectDuration);
            }
        }

        // ---- Effect 5 ----
        private void ApplySlow()
        {
            // Only player actions are affected - never Time.timeScale.
            _player.SpeedMultiplier = 0.5f;
            _bottle.DrinkDurationMultiplier = 2f;
            _bottle.HideDurationMultiplier = 2f;
        }
    }
}
