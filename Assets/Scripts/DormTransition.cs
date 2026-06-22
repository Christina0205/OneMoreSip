using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Put this on the DormDoor in the walking scene. When the player reaches it:
///   - Drunk Level >= minDrunk  -> load the bathroom scene (pee minigame).
///   - otherwise                -> show "You are not that drunk and still stressed."
///
/// Uses distance (not a physics collider) so the player needs no Collider2D.
/// The reached Drunk Level is carried into the next scene via GameSession.
/// </summary>
public class DormTransition : MonoBehaviour
{
    [SerializeField] private int minDrunk = 30;
    [SerializeField] private float triggerRange = 1.5f;
    [Tooltip("Exact name of the bathroom scene (must be added to Build Settings).")]
    [SerializeField] private string bathroomSceneName = "Bathroom";

    private PlayerController _player;
    private bool _done;

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (_done || _player == null) return;
        if (Vector2.Distance(transform.position, _player.transform.position) > triggerRange) return;

        _done = true;
        GameSession.DrunkLevel = _player.DrunkLevel;   // carry into the next scene

        if (_player.DrunkLevel >= minDrunk)
            SceneManager.LoadScene(bathroomSceneName);
        else
            EndScreen.Show("You are not that drunk and still stressed.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRange);
    }
}
