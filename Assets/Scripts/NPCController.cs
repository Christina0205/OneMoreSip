using UnityEngine;

/// <summary>
/// Self-walking NPC (teachers / security guard). No keyboard control.
///
/// Patrol pattern: walk right ~3s, idle ~2s, walk left ~3s, idle ~2s, repeat.
/// (Set 'randomizePattern' to vary the durations so the NPCs feel less robotic.)
///
/// Facing is flipped via X-scale (+1 / -1), exactly like the player.
/// Animation is driven through the "IsWalking" bool, so each NPC's Animator
/// controller just needs an Idle (default) and a Walking state with an
/// Idle<->Walking transition on IsWalking. (maleTeacher / femaleTeacher /
/// securityGuard controllers are already set up this way.)
///
/// Detection: if the player is within 'detectRange' units AND is drinking or is
/// carrying a visible (un-hidden) bottle, a red "caught" message is shown.
/// </summary>
public class NPCController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float walkTime = 3f;
    [SerializeField] private float idleTime = 2f;
    [Tooltip("Randomly vary walk/idle durations so NPCs don't all move in lockstep.")]
    [SerializeField] private bool randomizePattern = true;

    [Header("Detection")]
    [Tooltip("How close (world units) the player must be to get caught.")]
    [SerializeField] private float detectRange = 2f;
    [TextArea]
    [SerializeField] private string caughtMessage =
        "You are caught by a staff member!\nYour parents are on the way!";

    private Animator _animator;
    private PlayerController _player;
    private float _baseScaleX = 1f;
    private bool _hasIsWalking;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    private enum Phase { WalkRight, IdleA, WalkLeft, IdleB }
    private Phase _phase;
    private float _timer;
    private float _lastAlert = -10f;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _baseScaleX = Mathf.Abs(transform.localScale.x);
        if (_baseScaleX == 0f) _baseScaleX = 1f;

        if (_animator != null)
            foreach (var p in _animator.parameters)
                if (p.name == "IsWalking") { _hasIsWalking = true; break; }

        _player = FindFirstObjectByType<PlayerController>();
        StartPhase(Phase.WalkRight);
    }

    private void Update()
    {
        TickPattern();
        Detect();
    }

    // -------------------------------------------------------------------
    // Patrol pattern
    // -------------------------------------------------------------------
    private void StartPhase(Phase p)
    {
        _phase = p;
        bool walking = (p == Phase.WalkRight || p == Phase.WalkLeft);
        float baseT = walking ? walkTime : idleTime;
        _timer = randomizePattern ? baseT * Random.Range(0.7f, 1.3f) : baseT;
    }

    private void TickPattern()
    {
        _timer -= Time.deltaTime;

        float dir = 0f;
        if (_phase == Phase.WalkRight) dir = 1f;
        else if (_phase == Phase.WalkLeft) dir = -1f;
        bool moving = dir != 0f;

        // Move + face the walk direction.
        transform.position += Vector3.right * (dir * moveSpeed * Time.deltaTime);
        if (moving)
        {
            Vector3 s = transform.localScale;
            s.x = (dir > 0f) ? _baseScaleX : -_baseScaleX;
            transform.localScale = s;
        }

        // Drive Idle/Walking animation.
        if (_hasIsWalking && _animator != null) _animator.SetBool(IsWalkingHash, moving);

        // Advance the pattern: right -> idle -> left -> idle -> right ...
        if (_timer <= 0f)
        {
            Phase next = _phase == Phase.WalkRight ? Phase.IdleA
                       : _phase == Phase.IdleA     ? Phase.WalkLeft
                       : _phase == Phase.WalkLeft  ? Phase.IdleB
                       :                             Phase.WalkRight;
            StartPhase(next);
        }
    }

    // -------------------------------------------------------------------
    // Catch detection
    // -------------------------------------------------------------------
    private void Detect()
    {
        if (_player == null) return;
        if (Vector2.Distance(transform.position, _player.transform.position) > detectRange) return;

        // Only detect if the NPC is FACING the player. Sprites face right at +scale.x,
        // left at -scale.x. So a right-facing NPC only sees players to its right, etc.
        bool facingRight = transform.localScale.x >= 0f;
        bool playerOnRight = _player.transform.position.x >= transform.position.x;
        bool facingPlayer = facingRight == playerOnRight;
        if (!facingPlayer) return;   // back turned -> no detection, even if drinking/holding

        bool caught = _player.IsDrinking || _player.BottleExposed;
        if (caught && Time.time - _lastAlert > 2f)   // don't spam the message
        {
            _lastAlert = Time.time;
            NpcAlert.Show(caughtMessage);
        }
    }

    // Optional: visualise the detection radius in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
