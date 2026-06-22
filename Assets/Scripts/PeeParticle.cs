using UnityEngine;

/// <summary>
/// One pee droplet. Falls under gravity. Scores when it hits a target; if it expires
/// or drops below the floor line without hitting a target, it counts as a miss (-3).
/// Each droplet scores exactly once (then is destroyed), so no per-frame inflation.
/// </summary>
public class PeeParticle : MonoBehaviour
{
    private PeeMinigame _game;
    private float _lifeRemaining;
    private float _floorY;
    private bool _scored;

    public void Init(PeeMinigame game, float lifetime, float floorY)
    {
        _game = game;
        _lifeRemaining = lifetime;
        _floorY = floorY;
    }

    private void Update()
    {
        _lifeRemaining -= Time.deltaTime;
        if (_scored) return;

        if (_lifeRemaining <= 0f || transform.position.y < _floorY)
        {
            _scored = true;
            _game.OnMiss();              // hit "anything else" -> -3
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_scored) return;
        var tag = other.GetComponent<PeeTargetTag>();
        if (tag != null)
        {
            _scored = true;
            _game.OnHit(tag.points);     // target1/2/3 -> +1/+2/+3
            Destroy(gameObject);
        }
    }
}
