using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Core "One More Sip" mechanic — handles the sip action, fill level, and risk/reward logic.
/// </summary>
public class SipMechanic : MonoBehaviour
{
    [Header("Sip Settings")]
    [SerializeField] private float maxFillLevel = 100f;
    [SerializeField] private float sipAmount = 10f;
    [SerializeField] [Range(0f, 1f)] private float dangerThreshold = 0.8f;

    [Header("Events")]
    public UnityEvent onSip;
    public UnityEvent onSpill;           // triggered when sip pushes over the edge
    public UnityEvent<float> onFillChanged; // passes normalised fill (0–1)

    public float CurrentFill { get; private set; }
    public float NormalisedFill => CurrentFill / maxFillLevel;
    public bool InDangerZone => NormalisedFill >= dangerThreshold;

    private void Start()
    {
        CurrentFill = maxFillLevel * 0.3f; // start partially filled
        onFillChanged?.Invoke(NormalisedFill);
    }

    /// <summary>Call this when the player takes a sip.</summary>
    public void TakeSip()
    {
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing)
            return;

        float previous = CurrentFill;
        CurrentFill = Mathf.Min(CurrentFill + sipAmount, maxFillLevel + sipAmount);

        onSip?.Invoke();
        onFillChanged?.Invoke(NormalisedFill);

        if (CurrentFill >= maxFillLevel)
        {
            Spill();
        }
        else
        {
            ScoreManager.Instance?.AddScore(InDangerZone ? 2 : 1);
        }
    }

    private void Spill()
    {
        CurrentFill = maxFillLevel;
        onSpill?.Invoke();
        GameManager.Instance?.EndGame();
    }

    /// <summary>Refill the cup (e.g. at level start or after a power-up).</summary>
    public void Refill(float amount)
    {
        CurrentFill = Mathf.Max(0f, CurrentFill - amount);
        onFillChanged?.Invoke(NormalisedFill);
    }
}
