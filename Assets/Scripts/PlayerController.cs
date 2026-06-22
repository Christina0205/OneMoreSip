using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Player controller for OneMoreSip (walking section).
///
/// Movement: A = left, D = right. Facing is flipped via X-scale (+1 / -1).
///
/// Bottle / drinking / hiding state machine (animation states are played by name,
/// so the Player animator controller must contain states with these exact names):
///   - No bottle:        "idle" / "RightWalking"
///   - Holding bottle:   "bottleIdle" / "bottleWalking"
///   - Drinking (E):     "drinkingIdle" (standing) / "drinking" (walking)
///   - Hiding (Shift):   "hidingIdle" (standing) / "hidingWalking" (walking)
///
/// Rules:
///   - Walking over a ground bottle while EMPTY-HANDED picks it up (bottle amount = 20)
///     and the ground bottle disappears.
///   - Each E press: bottle amount -5, drunk level +5 (a top-left text hint shows).
///   - You cannot pick up a new bottle until the current one reaches 0; at 0 you drop
///     it and return to the normal "idle" / "RightWalking" states.
///
/// Uses the new Input System (Keyboard.current).
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Horizontal move speed in units per second.")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Bottle pickup")]
    [Tooltip("Parent that holds the ground bottles. Auto-found by name '+++++Bottles+++++' if left empty.")]
    [SerializeField] private Transform bottlesParent;
    [Tooltip("How close (world units) the player must be to a ground bottle to pick it up.")]
    [SerializeField] private float pickupRange = 1.5f;

    [Header("Drinking")]
    [Tooltip("How long one drink takes (seconds). Drunk/Bottle only change when it FINISHES.")]
    [SerializeField] private float drinkDuration = 3f;

    [Header("References")]
    [Tooltip("Animator on the player. Auto-grabbed if left empty.")]
    [SerializeField] private Animator animator;
    [Tooltip("Optional - kept so existing scene references stay valid. Not required.")]
    [SerializeField] private SipMechanic sipMechanic;

    [Header("Dorm / bathroom transition")]
    [Tooltip("Name of the dorm door object in this scene.")]
    [SerializeField] private string dormDoorName = "DormDoor";
    [SerializeField] private int dormMinDrunk = 30;
    [SerializeField] private float dormRange = 2.5f;

    private Transform _dormDoor;
    private bool _enteredDorm;

    // ---- Tunable constants ----
    private const int BottleMax = 20;
    private const int DrinkStep = 5;
    private const int DrunkMax = 100;

    // ---- Runtime state ----
    public int DrunkLevel { get; private set; } = 0;
    public int BottleAmount { get; private set; } = 0;
    private bool Holding => BottleAmount > 0;

    // ---- Public state for NPC detection ----
    public bool IsDrinking => _isDrinking;
    public bool IsHolding => BottleAmount > 0;
    /// <summary>True when carrying a bottle that is NOT hidden (i.e. visible to staff).</summary>
    public bool BottleExposed => IsHolding && !_hiding;

    private bool _hiding = false;

    // ---- Drunk-effect modifiers (set by DrunkEffectManager) ----
    [HideInInspector] public bool ReversedControls = false;       // Effect 3
    [HideInInspector] public float InputDelay = 0f;              // Effect 4 (seconds)
    [HideInInspector] public float SpeedMultiplier = 1f;         // Effect 5
    [HideInInspector] public float DrinkDurationMultiplier = 1f; // Effect 5
    public string CurrentDrunkEffect = "None";                   // shown in HUD

    private struct TimedInput { public float t; public float dir; }
    private readonly List<TimedInput> _inputBuffer = new List<TimedInput>();

    private float _baseScaleX = 1f;
    private bool _isDrinking = false;     // a drink is in progress
    private float _drinkRemaining = 0f;   // seconds left until it completes
    private string _currentAnim = "";

    // If the controller still has the legacy "IsWalking" bool + idle<->RightWalking
    // transitions, we keep the bool in sync so those transitions agree with Play().
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private bool _hasIsWalking = false;
    private readonly List<Transform> _bottles = new List<Transform>();

    // ---- Top-left HUD ----
    private Text _hudText;

    private void Awake()
    {
        _baseScaleX = Mathf.Abs(transform.localScale.x);
        if (_baseScaleX == 0f) _baseScaleX = 1f;

        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Detect whether the controller exposes the legacy "IsWalking" bool.
        if (animator != null)
            foreach (var p in animator.parameters)
                if (p.name == "IsWalking") { _hasIsWalking = true; break; }

        CacheBottles();
        BuildHud();

        var door = GameObject.Find(dormDoorName);
        if (door != null) _dormDoor = door.transform;
        else Debug.LogWarning($"[OneMoreSip] Dorm door '{dormDoorName}' not found in the scene.");

        // Drives the drunk effects (auto-added so there's nothing to wire up).
        if (GetComponent<DrunkEffectManager>() == null)
            gameObject.AddComponent<DrunkEffectManager>();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // --- read raw input ---
        float raw = 0f;
        if (kb.aKey.isPressed) raw = -1f;
        if (kb.dKey.isPressed) raw = 1f;          // D wins if both held
        if (ReversedControls) raw = -raw;         // Effect 3: A/D inverted

        // Effect 4: input delay - store input now, act on the input from `InputDelay` ago.
        _inputBuffer.Add(new TimedInput { t = Time.time, dir = raw });
        float dir = raw;
        if (InputDelay > 0f)
        {
            dir = 0f;
            float cutoff = Time.time - InputDelay;
            for (int i = 0; i < _inputBuffer.Count; i++)
                if (_inputBuffer[i].t <= cutoff) dir = _inputBuffer[i].dir;
        }
        _inputBuffer.RemoveAll(s => s.t < Time.time - 2f);

        bool moving = dir != 0f;
        bool hiding = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        _hiding = hiding;   // exposed for NPC detection

        // --- move + face (Effect 5 scales speed) ---
        transform.position += Vector3.right * (dir * moveSpeed * SpeedMultiplier * Time.deltaTime);
        if (moving)
        {
            Vector3 s = transform.localScale;
            s.x = (dir > 0f) ? _baseScaleX : -_baseScaleX;   // requirement 5: x = +1 / -1
            transform.localScale = s;
        }

        // --- pickup (only when empty-handed) ---
        if (!Holding) TryPickup();

        // --- drinking (E) : a 3-second action. Drunk Level / Bottle amount only change
        //     when the action FINISHES. Pressing Shift mid-drink cancels it (no change). ---
        if (_isDrinking)
        {
            if (hiding)
            {
                _isDrinking = false;                 // Shift interrupts -> no +5 / -5
            }
            else
            {
                _drinkRemaining -= Time.deltaTime;
                if (_drinkRemaining <= 0f)
                    CompleteDrink();                 // only now apply the effect
            }
        }
        else if (kb.eKey.wasPressedThisFrame && Holding && !hiding)
        {
            _isDrinking = true;                      // begin a fresh drink
            _drinkRemaining = drinkDuration * DrinkDurationMultiplier; // Effect 5 doubles it
        }

        // --- pick + play the right animation state ---
        UpdateAnimation(moving, hiding);

        // --- refresh HUD ---
        UpdateHud();

        // --- reached the dorm door? ---
        CheckDorm();
    }

    private void CheckDorm()
    {
        if (_enteredDorm || _dormDoor == null) return;
        if (Vector2.Distance(transform.position, _dormDoor.position) > dormRange) return;

        _enteredDorm = true;
        Debug.Log($"[OneMoreSip] Reached dorm. Drunk Level = {DrunkLevel}.");

        if (DrunkLevel >= dormMinDrunk)
        {
            // Same-scene switch: spawn the bathroom controller and hand it this player.
            var go = new GameObject("BathroomManager");
            go.AddComponent<PeeMinigame>().Begin(this, DrunkLevel);
        }
        else
        {
            EndScreen.Show("You are not that drunk and still stressed.");
        }
    }

    // -------------------------------------------------------------------
    // Bottle pickup
    // -------------------------------------------------------------------
    private void CacheBottles()
    {
        if (bottlesParent == null)
        {
            var go = GameObject.Find("+++++Bottles+++++");
            if (go != null) bottlesParent = go.transform;
        }
        _bottles.Clear();
        if (bottlesParent != null)
            foreach (Transform child in bottlesParent)
                _bottles.Add(child);
    }

    private void TryPickup()
    {
        foreach (var bottle in _bottles)
        {
            if (bottle == null || !bottle.gameObject.activeSelf) continue;
            if (Vector2.Distance(transform.position, bottle.position) <= pickupRange)
            {
                bottle.gameObject.SetActive(false); // the touched ground bottle disappears
                BottleAmount = BottleMax;           // now holding a full bottle (20)
                return;
            }
        }
    }

    // -------------------------------------------------------------------
    // Drinking
    // -------------------------------------------------------------------
    /// <summary>Called only when a full drink action finishes (not when cancelled).</summary>
    private void CompleteDrink()
    {
        BottleAmount = Mathf.Max(0, BottleAmount - DrinkStep);    // -5 bottle
        DrunkLevel = Mathf.Min(DrunkMax, DrunkLevel + DrinkStep); // +5 drunk
        _isDrinking = false;
        // If the bottle just hit 0, Holding becomes false and we fall back to
        // the normal idle / RightWalking states automatically.
    }

    // -------------------------------------------------------------------
    // Animation state selection (played by name; priority top-to-bottom)
    // -------------------------------------------------------------------
    private void UpdateAnimation(bool moving, bool hiding)
    {
        // Keep the legacy bool in sync so idle<->RightWalking transitions (if the
        // controller still has them) don't override our Play() calls.
        if (_hasIsWalking && animator != null)
            animator.SetBool(IsWalkingHash, moving && !hiding && !_isDrinking && !Holding);

        string state;
        if (hiding)
            state = moving ? "hidingWalking" : "hidingIdle";
        else if (_isDrinking)
            state = moving ? "drinking" : "drinkingIdle";
        else if (Holding)
            state = moving ? "bottleWalking" : "bottleIdle";
        else
            state = moving ? "RightWalking" : "idle";

        if (state != _currentAnim)
        {
            _currentAnim = state;
            if (animator != null) animator.Play(state, 0, 0f);
        }
    }

    // -------------------------------------------------------------------
    // Top-left HUD text (built at runtime - nothing to assign in Unity)
    // -------------------------------------------------------------------
    private void BuildHud()
    {
        var canvasGo = new GameObject("PlayerHUD_Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        var textGo = new GameObject("HUDText");
        textGo.transform.SetParent(canvasGo.transform, false);
        _hudText = textGo.AddComponent<Text>();
        _hudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        _hudText.fontSize = 22;
        _hudText.alignment = TextAnchor.UpperLeft;
        _hudText.color = Color.white;
        _hudText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _hudText.verticalOverflow = VerticalWrapMode.Overflow;

        var rt = _hudText.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f); // top-left
        rt.anchoredPosition = new Vector2(16f, -16f);
        rt.sizeDelta = new Vector2(600f, 120f);
    }

    private void UpdateHud()
    {
        if (_hudText == null) return;

        string s = $"Drunk Level: {DrunkLevel}";
        if (Holding) s += $"\nBottle: {BottleAmount} / {BottleMax}";
        if (_isDrinking) s += $"\nDrinking... {_drinkRemaining:0.0}s  (+5 Drunk on finish)";
        if (CurrentDrunkEffect != "None") s += $"\nDrunk Effect: {CurrentDrunkEffect}";
        _hudText.text = s;
    }
}
