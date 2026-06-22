using UnityEngine;

/// <summary>
/// A transparent duplicate spawned by the Double Vision effect. It starts offset
/// from the original sprite, drifts back toward it while fading out, then destroys
/// itself - simulating the eyes losing then regaining focus.
/// </summary>
public class DoubleVisionGhost : MonoBehaviour
{
    private Transform _original;
    private SpriteRenderer _srcSr;
    private SpriteRenderer _sr;
    private float _life;
    private float _age;
    private Vector3 _offset;
    private Color _baseColor;

    public static void Spawn(SpriteRenderer src, float life, Vector2 offset)
    {
        var go = new GameObject("DoubleVisionGhost");
        go.AddComponent<DoubleVisionGhost>().Init(src, life, offset);
    }

    private void Init(SpriteRenderer src, float life, Vector2 offset)
    {
        _srcSr = src;
        _original = src.transform;
        _life = life;
        _offset = offset;

        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sprite = src.sprite;
        _sr.sortingLayerID = src.sortingLayerID;
        _sr.sortingOrder = src.sortingOrder + 1;
        _sr.flipX = src.flipX;
        _sr.flipY = src.flipY;

        transform.position = _original.position + (Vector3)offset;
        transform.localScale = _original.lossyScale;
        transform.rotation = _original.rotation;

        _baseColor = src.color;
        SetAlpha(0.45f);
    }

    private void Update()
    {
        _age += Time.deltaTime;
        float k = Mathf.Clamp01(_age / _life);

        Vector3 anchor = _original != null ? _original.position : transform.position - (Vector3)_offset * (1f - k);
        transform.position = anchor + (Vector3)_offset * (1f - k); // drift back to the original
        if (_original != null)
        {
            transform.localScale = _original.lossyScale;
            if (_srcSr != null) _sr.flipX = _srcSr.flipX;
        }

        SetAlpha(Mathf.Lerp(0.45f, 0f, k));        // fade away

        if (_age >= _life) Destroy(gameObject);
    }

    private void SetAlpha(float a)
    {
        _sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, a);
    }
}
