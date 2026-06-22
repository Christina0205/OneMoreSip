using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the drunk effects. The cadence and how many effects fire at once scale
/// with how drunk the player is:
///
///   Tier 1  (Drunk 10-34) : every 10s, 1 effect
///   Tier 2  (Drunk 35-64) : every  8s, 1 OR 2 effects
///   Tier 3  (Drunk 65+)   : every  6s, 2 effects
///
/// (Thresholds use >=, so the small gaps in the spec - 31-34 and 61-64 - fall into
/// the lower tier rather than doing nothing.)
///
/// Each burst lasts 4 seconds, then everything returns to normal. Auto-added by
/// PlayerController, so there is nothing to wire up in the editor.
///
///  1 Screen Shake     - gentle woozy camera sway (not an earthquake)
///  2 Double Vision    - transparent, fading duplicates of NPCs + bottles
///  3 Reversed Controls- A/D inverted
///  4 Input Delay      - 0.5s movement delay
///  5 Slow Actions     - move speed x0.5, drink duration x2 (no Time.timeScale)
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class DrunkEffectManager : MonoBehaviour
{
    [Header("Tier thresholds (Drunk Level)")]
    [SerializeField] private float tier1Min = 10f;   // Tier 1: 10-34  -> every 10s, 1 effect
    [SerializeField] private float tier2Min = 35f;   // Tier 2: 35-64  -> every  8s, 1 or 2
    [SerializeField] private float tier3Min = 65f;   // Tier 3: 65+    -> every  6s, 2 effects

    [Header("Timing")]
    [SerializeField] private float effectDuration = 4f;
    [SerializeField] private float tier1Interval = 10f;
    [SerializeField] private float tier2Interval = 8f;
    [SerializeField] private float tier3Interval = 6f;

    private PlayerController _player;
    private CameraFollow _camera;

    private void Awake() => _player = GetComponent<PlayerController>();

    private void Start()
    {
        _camera = FindFirstObjectByType<CameraFollow>();
        StartCoroutine(EffectLoop());
    }

    private IEnumerator EffectLoop()
    {
        while (true)
        {
            int tier = GetTier(_player.DrunkLevel);
            if (tier == 0)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            float interval;
            int count;
            switch (tier)
            {
                case 3: interval = tier3Interval; count = 2; break;                 // always 2
                case 2: interval = tier2Interval; count = Random.Range(1, 3); break; // 1 or 2
                default: interval = tier1Interval; count = 1; break;                 // 1
            }

            yield return StartCoroutine(RunEffects(count));
            yield return new WaitForSeconds(Mathf.Max(0f, interval - effectDuration));
        }
    }

    private int GetTier(float drunk)
    {
        if (drunk >= tier3Min) return 3;
        if (drunk >= tier2Min) return 2;
        if (drunk >= tier1Min) return 1;
        return 0;
    }

    // -------------------------------------------------------------------
    // Run N distinct effects together for one burst.
    // -------------------------------------------------------------------
    private IEnumerator RunEffects(int count)
    {
        var chosen = PickDistinct(count);
        var names = new List<string>();
        foreach (int e in chosen) ApplyEffect(e, names);
        _player.CurrentDrunkEffect = string.Join(" + ", names);

        yield return new WaitForSeconds(effectDuration);

        ResetAll();
        _player.CurrentDrunkEffect = "None";
    }

    private List<int> PickDistinct(int count)
    {
        var pool = new List<int> { 1, 2, 3, 4, 5 };
        var result = new List<int>();
        count = Mathf.Clamp(count, 1, pool.Count);
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return result;
    }

    private void ApplyEffect(int effect, List<string> names)
    {
        switch (effect)
        {
            case 1:
                names.Add("Screen Shake");
                if (_camera != null) _camera.DrunkSway(effectDuration);
                break;
            case 2:
                names.Add("Double Vision");
                SpawnDoubleVision();
                break;
            case 3:
                names.Add("Reversed Controls");
                _player.ReversedControls = true;
                break;
            case 4:
                names.Add("Input Delay");
                _player.InputDelay = 0.5f;
                break;
            case 5:
                names.Add("Slow Actions");
                _player.SpeedMultiplier = 0.5f;
                _player.DrinkDurationMultiplier = 2f;
                break;
        }
    }

    private void ResetAll()
    {
        _player.ReversedControls = false;
        _player.InputDelay = 0f;
        _player.SpeedMultiplier = 1f;
        _player.DrinkDurationMultiplier = 1f;
        if (_camera != null) _camera.StopSway();
    }

    // Effect 2: spawn fading duplicates of every NPC and every active ground bottle.
    private void SpawnDoubleVision()
    {
        var renderers = new List<SpriteRenderer>();

        foreach (var npc in FindObjectsByType<NPCController>(FindObjectsSortMode.None))
        {
            var sr = npc.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) renderers.Add(sr);
        }

        var bottlesParent = GameObject.Find("+++++Bottles+++++");
        if (bottlesParent != null)
            foreach (Transform child in bottlesParent.transform)
                if (child.gameObject.activeSelf)
                {
                    var sr = child.GetComponent<SpriteRenderer>();
                    if (sr != null) renderers.Add(sr);
                }

        foreach (var sr in renderers)
        {
            float sign = Random.value < 0.5f ? -1f : 1f;
            Vector2 offset = new Vector2(Random.Range(0.5f, 0.9f) * sign, Random.Range(0.2f, 0.5f));
            DoubleVisionGhost.Spawn(sr, effectDuration, offset);
        }
    }
}
