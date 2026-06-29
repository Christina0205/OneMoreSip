using System.Collections;
using UnityEngine;

/// <summary>
/// One pee droplet. Falls under gravity. When it hits a target it scores once, then
/// sticks in place as a fading "stain" for a few seconds before disappearing.
/// If it expires / drops below the floor without hitting a target, it's a miss (-3).
/// Ghost (double-vision) droplets are non-scoring and just disappear on contact.
/// </summary>
public class PeeParticle : MonoBehaviour
{
    private PeeMinigame _game;
    private float _lifeRemaining;
    private float _floorY;
    private bool _scored;
    private bool _scoring = true;
    private float _stainSeconds = 2f;

    public void Init(PeeMinigame game, float lifetime, float floorY,
                     bool scoring = true, float stainSeconds = 2f)
    {
        _game = game;
        _lifeRemaining = lifetime;
        _floorY = floorY;
        _scoring = scoring;
        _stainSeconds = stainSeconds;
    }

    private void Update()
    {
        if (_scored) return;
        _lifeRemaining -= Time.deltaTime;

        if (_lifeRemaining <= 0f || transform.position.y < _floorY)
        {
            _scored = true;             // missed everything: just disappear (no penalty now)
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_scored) return;
        var tag = other.GetComponent<PeeTargetTag>();
        if (tag == null) return;

        _scored = true;
        if (_scoring)
        {
            _game.OnDropletHit(tag.points);  // tag.points is the target index (1/2/3) or 0
            StartCoroutine(Stain());         // linger as a urine stain, then fade
        }
        else
        {
            Destroy(gameObject);             // ghost twins just vanish
        }
    }

    private IEnumerator Stain()
    {
        // Freeze where it landed and stop colliding.
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; }
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        yield return new WaitForSeconds(_stainSeconds);

        // Fade out over 1 second.
        var sr = GetComponent<SpriteRenderer>();
        const float fade = 1f;
        float t = 0f;
        Color start = sr != null ? sr.color : Color.white;
        while (t < fade)
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                Color c = start;
                c.a = Mathf.Lerp(start.a, 0f, t / fade);
                sr.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
