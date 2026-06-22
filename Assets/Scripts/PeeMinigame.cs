using System.Collections;
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

    [Header("Droplets")]
    [SerializeField] private float fireRate = 20f;        // original prototype value
    [SerializeField] private float particleSpeed = 14f;   // original prototype value
    [SerializeField] private float gravityScale = 0.7f;   // original prototype value
    [SerializeField] private float particleLifetime = 3f;
    [SerializeField] private float dropletSize = 0.1f;    // kept smaller per your earlier request

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
    private int _remaining, _score, _goalLow, _goalHigh;
    private bool _firing, _spent, _ended;
    private float _fireAcc, _endTimer;
    private static Sprite _dropletSprite;

    private Text _info, _center;

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
        HideWalking(walkingPlayer);
        ResolveReferences();
        SetupTargets();
        FrameCamera();
        BuildUI();

        _remaining = Mathf.Max(1, drunk * 25);     // total droplets (requirement 6)
        GenerateGoalRange(_remaining);             // random reachable goal (requirement 5)
        _angle = Mathf.Clamp(startAngle, aimMinAngle, aimMaxAngle);

        StartCoroutine(Run());
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
            // target1/2/3 -> +1/+2/+3 ; floor / Wall / anything else -> -3
            int pts = -3;
            if (child.name.ToLower().Contains("target"))
                foreach (char c in child.name)
                    if (char.IsDigit(c)) { pts = c - '0'; break; }

            var col = child.GetComponent<Collider2D>();
            if (col == null)
            {
                var box = child.gameObject.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    box.size = sr.sprite.bounds.size;
                    box.offset = sr.sprite.bounds.center;
                }
                col = box;
            }
            else col.isTrigger = true;

            var tag = child.GetComponent<PeeTargetTag>() ?? child.gameObject.AddComponent<PeeTargetTag>();
            tag.points = pts;
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
        cam.transform.position = new Vector3(b.center.x, b.center.y, -10f);
    }

    private void GenerateGoalRange(int budget)
    {
        _goalLow = Mathf.RoundToInt(budget * Random.Range(0.45f, 0.6f));
        _goalHigh = _goalLow + Mathf.RoundToInt(budget * Random.Range(0.3f, 0.5f));
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
                _spent = true;
                _firing = false;
                _endTimer = particleLifetime + 0.3f;
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
        _angle = Mathf.Clamp(_angle + d * aimSpeed * Time.deltaTime, aimMinAngle, aimMaxAngle);
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

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.linearVelocity = dir * particleSpeed;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        go.AddComponent<PeeParticle>().Init(this, particleLifetime, _floorY);
    }

    public void OnHit(int points) { _score += points; }
    public void OnMiss() { _score -= 3; }

    private void End()
    {
        _ended = true;
        bool inRange = _score >= _goalLow && _score <= _goalHigh;
        string verdict = inRange ? "You hit the goal range!" : "Outside the goal range.";
        EndScreen.Show(
            $"Score: {_score}\nGoal range: {_goalLow} - {_goalHigh}\n{verdict}\n\nHave a good night!");
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

        _info = MakeText(canvasGo.transform, 22, TextAnchor.UpperLeft, Color.white,
                         new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(520f, 160f));
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
        _info.text = $"Pee left: {_remaining}\nScore: {_score}\nGoal: {_goalLow} - {_goalHigh}\nAngle: {Mathf.RoundToInt(_angle)}°";
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
