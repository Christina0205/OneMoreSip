using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// In-scene bathroom pee minigame. NOT a separate Unity scene - it runs inside the
/// walking scene. PlayerController spawns this and calls Begin() when the player
/// reaches the dorm with Drunk Level >= 30.
///
/// Begin() hides the walking world, moves the camera to the bathroom background, then:
///   - 3-2-1 countdown, then auto-releases gravity pee droplets from PeePoint
///   - player only aims with W (up) / S (down)
///   - target1 +1, target2 +2, target3 +3; anything else (miss) -3
///   - total droplets = Drunk Level x 25
///   - shows a random reachable goal range top-left
///   - at the end: score, in-range or not, and "Have a good night!"
///
/// You place these objects in the scene (anywhere off to the side is fine):
///   PlayerPee  (with a child empty named PeePoint at the nozzle)
///   "bathroom Targets"  (3 children: target1 / target2 / target3)
///   bathroom_0  (the background sprite)
/// </summary>
public class PeeMinigame : MonoBehaviour
{
    [Header("Scene object names (spaces/caps are ignored)")]
    [SerializeField] private string peePointName = "PeePoint";
    [SerializeField] private string targetsParentName = "bathroomTargets";
    [SerializeField] private string backgroundName = "bathroom_0";

    [Header("Aiming")]
    [SerializeField] private float aimMinAngle = -40f;
    [SerializeField] private float aimMaxAngle = 60f;
    [SerializeField] private float aimSpeed = 60f;
    [SerializeField] private float startAngle = 15f;
    [Tooltip("Tick this if the targets are to the LEFT of the player.")]
    [SerializeField] private bool aimFaceLeft = false;

    [Header("Player")]
    [SerializeField] private string playerName = "PlayerPee";

    [Header("Droplets")]
    [SerializeField] private float fireRate = 20f;
    [Tooltip("Pee power randomly drifts between these (changes how far it reaches).")]
    [SerializeField] private float powerMin = 10f;
    [SerializeField] private float powerMax = 20f;
    [SerializeField] private float powerChangeSpeed = 0.4f; // how fast the power wanders
    [SerializeField] private float gravityScale = 0.7f;
    [SerializeField] private float particleLifetime = 3f;
    [SerializeField] private float dropletSize = 0.1f;
    [Tooltip("How long a droplet lingers as a stain after hitting a target.")]
    [SerializeField] private float stainSeconds = 2f;

    [Header("Drunk effects (bathroom)")]
    [SerializeField] private float effectDuration = 4f;
    [SerializeField] private float shakeAmplitude = 0.15f;

    [Header("Rhythm scoring")]
    [Tooltip("A new target is highlighted every this many droplets.")]
    [SerializeField] private int windowSize = 25;

    [Header("Misc")]
    [SerializeField] private float countdownSeconds = 3f;

    [Header("Testing")]
    [Tooltip("Tick this and drop PeeMinigame on an empty object to test the bathroom directly (no walk needed).")]
    [SerializeField] private bool autoStartForTesting = false;
    [SerializeField] private int debugDrunk = 50;

    private bool _begun;

    private Transform _peePoint;
    private float _floorY = -100f;
    private float _angle;
    private int _remaining;
    private bool _firing, _spent, _ended;
    private float _fireAcc, _endTimer;
    private static Sprite _dropletSprite;

    // ---- Rhythm scoring (highlight one target per window) ----
    private int _score, _miss, _combo;
    private int _highlightIndex;          // 1..3, the lit target
    private int _dropletsThisWindow;
    private bool _windowHit;              // did we hit the lit target this window?
    private readonly SpriteRenderer[] _targets = new SpriteRenderer[4]; // [1..3]
    private readonly Color[] _baseColors = new Color[4];

    private Text _info, _center;

    private float _powerSeed;

    // drunk effects
    private int _drunk;
    private Vector3 _camBase;
    private float _shakeTimer;
    private float _aimMult = 1f;
    private bool _doubleVisionActive;
    private string _effectName = "None";
    private SpriteRenderer _playerRenderer;
    private readonly List<SpriteRenderer> _targetRenderers = new List<SpriteRenderer>();

    private void Start()
    {
        // Standalone test path: lets you run the bathroom without walking to the dorm.
        if (autoStartForTesting && !_begun) Begin(null, debugDrunk);
    }

    /// <summary>Switch from the walking scene into the bathroom and start the minigame.</summary>
    public void Begin(PlayerController walkingPlayer, int drunk)
    {
        if (_begun) return;
        _begun = true;
        AudioManager.Instance?.StopMusic();   // corridor music off in the bathroom
        HideWalking(walkingPlayer);
        ResolveReferences();
        SetupTargets();
        FrameCamera();
        BuildUI();

        _drunk = drunk;
        _remaining = Mathf.Max(1, drunk * 25);     // total droplets
        _angle = Mathf.Clamp(startAngle, aimMinAngle, aimMaxAngle);
        _powerSeed = Random.value * 100f;

        SetupPlayerMovement();

        StartCoroutine(Run());
    }

    private void SetupPlayerMovement()
    {
        // Player no longer moves; we only need its renderer for the Double Vision effect.
        var p = FindLoose(playerName);
        if (p != null) _playerRenderer = p.GetComponentInChildren<SpriteRenderer>();
    }

    // -------------------------------------------------------------------
    // Switch worlds
    // -------------------------------------------------------------------
    private void HideWalking(PlayerController walkingPlayer)
    {
        var hud = GameObject.Find("PlayerHUD_Canvas");
        if (hud != null) hud.SetActive(false);

        foreach (var npc in FindObjectsByType<NPCController>(FindObjectsSortMode.None))
            npc.gameObject.SetActive(false);

        var bottles = GameObject.Find("+++++Bottles+++++");
        if (bottles != null) bottles.SetActive(false);

        // Stop the camera-follow so we can reframe on the bathroom.
        var cf = FindFirstObjectByType<CameraFollow>();
        if (cf != null) cf.enabled = false;

        if (walkingPlayer != null) walkingPlayer.gameObject.SetActive(false);
    }

    private void ResolveReferences()
    {
        var point = FindLoose(peePointName);
        if (point != null) _peePoint = point.transform;
        else Debug.LogWarning($"PeeMinigame: '{peePointName}' not found - droplets spawn at origin.");

        var bgGo = FindLoose(backgroundName);
        if (bgGo != null)
        {
            var sr = bgGo.GetComponent<SpriteRenderer>();
            if (sr != null) _floorY = sr.bounds.min.y;
        }
    }

    // Finds a GameObject by name, ignoring spaces and capitalization (and inactive ones).
    private static GameObject FindLoose(string wanted)
    {
        var exact = GameObject.Find(wanted);
        if (exact != null) return exact;
        string key = Norm(wanted);
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (Norm(t.name) == key) return t.gameObject;
        return null;
    }

    private static string Norm(string s) => s.Replace(" ", "").ToLowerInvariant();

    private void SetupTargets()
    {
        var parent = FindLoose(targetsParentName);
        if (parent == null)
        {
            Debug.LogWarning($"PeeMinigame: '{targetsParentName}' not found - nothing to score on.");
            return;
        }

        foreach (Transform child in parent.transform)
        {
            // target1/2/3 -> index 1/2/3 ; floor / Wall / anything else -> 0
            int idx = 0;
            if (child.name.ToLower().Contains("target"))
                foreach (char c in child.name)
                    if (char.IsDigit(c)) { idx = c - '0'; break; }

            var col = child.GetComponent<Collider2D>();
            if (col == null)
            {
                var box = child.gameObject.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                var sr2 = child.GetComponent<SpriteRenderer>();
                if (sr2 != null && sr2.sprite != null)
                {
                    box.size = sr2.sprite.bounds.size;
                    box.offset = sr2.sprite.bounds.center;
                }
                col = box;
            }
            else col.isTrigger = true;

            var tag = child.GetComponent<PeeTargetTag>() ?? child.gameObject.AddComponent<PeeTargetTag>();
            tag.points = idx;   // now an index (1/2/3) or 0 for non-targets

            var rend = child.GetComponent<SpriteRenderer>();
            if (rend != null && idx >= 1 && idx <= 3)
            {
                _targetRenderers.Add(rend);     // Double Vision: real targets only
                _targets[idx] = rend;           // for highlighting
                _baseColors[idx] = rend.color;
            }
        }
    }

    private void FrameCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.rotation = Quaternion.identity; // clear any drunk-sway tilt

        var bgGo = FindLoose(backgroundName);
        if (bgGo == null) return;
        var sr = bgGo.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        cam.orthographic = true;
        Bounds b = sr.bounds;
        cam.orthographicSize = b.extents.y;
        _camBase = new Vector3(b.center.x, b.center.y, -10f);
        cam.transform.position = _camBase;
    }

    // -------------------------------------------------------------------
    // Flow
    // -------------------------------------------------------------------
    private IEnumerator Run()
    {
        int n = Mathf.CeilToInt(countdownSeconds);
        for (int i = n; i >= 1; i--)
        {
            _center.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        _center.text = "";
        _firing = true;
        StartWindow();                       // light up the first target
        AudioManager.Instance?.StartPee();   // looping pee sound
        StartCoroutine(EffectLoop());        // drunk effects begin after the countdown
    }

    // -------------------------------------------------------------------
    // Rhythm scoring: highlight one target per window of 'windowSize' droplets.
    // -------------------------------------------------------------------
    private void StartWindow()
    {
        _dropletsThisWindow = 0;
        _windowHit = false;
        SetHighlight(Random.Range(1, 4));    // 1..3
    }

    private void ResolveWindow()
    {
        if (_windowHit) _combo++;            // hit this window -> combo grows
        else { _miss++; _combo = 0; }        // missed the window
    }

    private void SetHighlight(int index)
    {
        _highlightIndex = index;
        for (int i = 1; i <= 3; i++)
        {
            if (_targets[i] == null) continue;
            Color b = _baseColors[i];
            _targets[i].color = (i == index)
                ? b                                              // lit: full colour
                : new Color(b.r * 0.4f, b.g * 0.4f, b.b * 0.4f, b.a); // dimmed
        }
    }

    /// <summary>Called by a droplet when it lands on a target (index 1/2/3; 0 = floor/wall).</summary>
    public void OnDropletHit(int index)
    {
        if (index < 1 || index > 3) return;             // floor / wall: nothing
        AudioManager.Instance?.NoteHit(index - 1);      // do / re / mi
        if (index == _highlightIndex && !_windowHit)
        {
            _windowHit = true;
            _score++;                                   // scored this window
        }
    }

    private void Update()
    {
        HandleAim();

        if (_firing && !_spent)
        {
            _fireAcc += fireRate * Time.deltaTime;
            while (_fireAcc >= 1f && _remaining > 0)
            {
                _fireAcc -= 1f;
                FireOne();
                _remaining--;
            }
            if (_remaining <= 0)
            {
                if (_dropletsThisWindow > 0) ResolveWindow();  // count the final partial window
                _spent = true;
                _firing = false;
                _endTimer = particleLifetime + 0.3f;
                AudioManager.Instance?.StopPee();
            }
        }

        if (_spent && !_ended)
        {
            _endTimer -= Time.deltaTime;
            if (_endTimer <= 0f) End();
        }

        UpdateInfo();
    }

    private void HandleAim()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        float d = 0f;
        if (kb.wKey.isPressed) d += 1f;
        if (kb.sKey.isPressed) d -= 1f;
        _angle = Mathf.Clamp(_angle + d * aimSpeed * _aimMult * Time.deltaTime, aimMinAngle, aimMaxAngle);
    }

    private void FireOne()
    {
        Vector3 origin = _peePoint != null ? _peePoint.position : transform.position;
        float rad = _angle * Mathf.Deg2Rad;
        float sign = aimFaceLeft ? -1f : 1f;
        Vector2 dir = new Vector2(sign * Mathf.Cos(rad), Mathf.Sin(rad));

        var go = new GameObject("PeeDroplet");
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * dropletSize;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetDropletSprite();
        sr.color = new Color(0.95f, 0.9f, 0.25f);
        sr.sortingOrder = 50;

        Vector2 vel = dir * CurrentPower();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.linearVelocity = vel;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        go.AddComponent<PeeParticle>().Init(this, particleLifetime, _floorY, true, stainSeconds);

        // While Double Vision is active, spawn a translucent non-scoring twin droplet.
        if (_doubleVisionActive) SpawnGhostDroplet(origin, vel);

        // Advance the highlight window.
        _dropletsThisWindow++;
        if (_dropletsThisWindow >= windowSize) { ResolveWindow(); StartWindow(); }
    }

    private void SpawnGhostDroplet(Vector3 origin, Vector2 velocity)
    {
        var go = new GameObject("PeeDropletGhost");
        go.transform.position = origin + (Vector3)(RandomOffset() * 0.3f);
        go.transform.localScale = Vector3.one * dropletSize;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetDropletSprite();
        sr.color = new Color(0.95f, 0.9f, 0.25f, 0.4f);
        sr.sortingOrder = 49;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.linearVelocity = velocity;

        // Same collision behaviour as a real droplet (disappears on any target),
        // but non-scoring so the twin doesn't change the score.
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;
        go.AddComponent<PeeParticle>().Init(this, particleLifetime, _floorY, false);
    }

    private void End()
    {
        _ended = true;
        // calculation = (score + miss score) / score, where miss score = -miss.
        float calc = _score > 0 ? (float)(_score - _miss) / _score : 0f;
        string verdict = calc >= 0.8f ? "Perfect Melody!"
                       : calc >= 0.4f ? "You Made it!"
                       :                "Such a Mess!!!";
        if (calc >= 0.4f) AudioManager.Instance?.PlayWin();
        else AudioManager.Instance?.PlayGameOver();
        EndScreen.Show($"Score: {_score}\nMiss: {_miss}\n{verdict}\n\nHave a good night!");
    }

    // -------------------------------------------------------------------
    // Randomly-drifting pee power (makes the player move to reach targets)
    // -------------------------------------------------------------------
    private float CurrentPower()
    {
        float n = Mathf.PerlinNoise(_powerSeed, Time.time * powerChangeSpeed);
        return Mathf.Lerp(powerMin, powerMax, n);
    }

    // -------------------------------------------------------------------
    // Drunk effects (Camera Shake / Slow / Double Vision)
    //   Drunk 35-64: every 6s, 1 or 2 effects ;  Drunk 65+: every 5s, 2 effects
    // -------------------------------------------------------------------
    private IEnumerator EffectLoop()
    {
        while (!_ended)
        {
            int tier = _drunk >= 65 ? 3 : _drunk >= 35 ? 2 : 0;
            if (tier == 0) { yield return new WaitForSeconds(0.5f); continue; }

            float interval = tier == 3 ? 5f : 6f;
            int count = tier == 3 ? 2 : Random.Range(1, 3);

            yield return StartCoroutine(RunEffects(count));
            yield return new WaitForSeconds(Mathf.Max(0f, interval - effectDuration));
        }
    }

    private IEnumerator RunEffects(int count)
    {
        var pool = new List<int> { 0, 1, 2 };   // 0 shake, 1 slow, 2 double vision
        var names = new List<string>();
        count = Mathf.Clamp(count, 1, pool.Count);
        for (int i = 0; i < count; i++)
        {
            int k = Random.Range(0, pool.Count);
            ApplyEffect(pool[k], names);
            pool.RemoveAt(k);
        }
        _effectName = string.Join(" + ", names);

        yield return new WaitForSeconds(effectDuration);

        ResetEffects();
        _effectName = "None";
    }

    private void ApplyEffect(int e, List<string> names)
    {
        switch (e)
        {
            case 0: names.Add("Camera Shake"); _shakeTimer = effectDuration; break;
            case 1: names.Add("Slow"); _aimMult = 0.5f; break;
            case 2: names.Add("Double Vision"); SpawnDoubleVision(); _doubleVisionActive = true; break;
        }
    }

    private void ResetEffects()
    {
        _shakeTimer = 0f;
        _aimMult = 1f;
        _doubleVisionActive = false;
        var cam = Camera.main;
        if (cam != null) cam.transform.position = _camBase;
    }

    // Double Vision affects: target1/2/3, PlayerPee, and the pee droplets.
    private void SpawnDoubleVision()
    {
        // Targets (only the real targets - floor/Wall excluded).
        foreach (var sr in _targetRenderers)
            if (sr != null) DoubleVisionGhost.Spawn(sr, effectDuration, RandomOffset());

        // The player.
        if (_playerRenderer != null)
            DoubleVisionGhost.Spawn(_playerRenderer, effectDuration, RandomOffset());

        // Pee droplets currently in the air.
        foreach (var p in FindObjectsByType<PeeParticle>(FindObjectsSortMode.None))
        {
            var sr = p.GetComponent<SpriteRenderer>();
            if (sr != null) DoubleVisionGhost.Spawn(sr, effectDuration, RandomOffset());
        }
    }

    private static Vector2 RandomOffset()
    {
        float sgn = Random.value < 0.5f ? -1f : 1f;
        return new Vector2(Random.Range(0.4f, 0.8f) * sgn, Random.Range(0.2f, 0.5f));
    }

    private void LateUpdate()
    {
        if (_shakeTimer <= 0f) return;
        _shakeTimer -= Time.deltaTime;
        var cam = Camera.main;
        if (cam == null) return;
        float t = Time.time * 18f;
        Vector3 off = new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, 0f)
                      * (2f * shakeAmplitude);
        cam.transform.position = _camBase + off;
        if (_shakeTimer <= 0f) cam.transform.position = _camBase;
    }

    // -------------------------------------------------------------------
    // UI
    // -------------------------------------------------------------------
    private void BuildUI()
    {
        var canvasGo = new GameObject("PeeUI");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Top-left: controls.
        var controls = MakeText(canvasGo.transform, 22, TextAnchor.UpperLeft, Color.white,
                                new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(520f, 80f));
        controls.text = "W/S: Change Angle";

        // Top-right: stats.
        _info = MakeText(canvasGo.transform, 22, TextAnchor.UpperRight, Color.white,
                         new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(520f, 200f));
        _center = MakeText(canvasGo.transform, 90, TextAnchor.MiddleCenter, Color.white,
                           new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 300f));
        _center.text = "";
    }

    private Text MakeText(Transform parent, int size, TextAnchor anchor, Color color,
                          Vector2 anchorPivot, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = size;
        t.alignment = anchor;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchorPivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    private void UpdateInfo()
    {
        if (_info == null) return;
        string s = $"Score: {_score}\nMiss: {_miss}";
        if (_combo >= 3) s += $"\nCombo x{_combo}";
        s += $"\nAngle: {Mathf.RoundToInt(_angle)}°";
        _info.text = s;
    }

    private static Sprite GetDropletSprite()
    {
        if (_dropletSprite != null) return _dropletSprite;
        const int s = 32;
        var tex = new Texture2D(s, s);
        float r = s / 2f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x - r + 0.5f, dy = y - r + 0.5f;
                bool inside = dx * dx + dy * dy <= (r - 0.5f) * (r - 0.5f);
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        tex.Apply();
        _dropletSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        return _dropletSprite;
    }
}
