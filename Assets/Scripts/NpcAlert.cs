using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared on-screen red "caught" message. Any NPC can call NpcAlert.Show(...).
/// Lazily creates a single overlay canvas + text, so multiple NPCs don't each
/// spawn their own UI. The message auto-hides after a short time.
/// </summary>
public class NpcAlert : MonoBehaviour
{
    private static NpcAlert _instance;
    private Text _text;
    private float _hideAt;

    public static void Show(string message, float duration = 2.5f)
    {
        EnsureInstance();
        _instance._text.text = message;
        _instance._text.enabled = true;
        _instance._hideAt = Time.time + duration;
    }

    private static void EnsureInstance()
    {
        if (_instance != null) return;
        var go = new GameObject("NpcAlert");
        _instance = go.AddComponent<NpcAlert>();
        _instance.Build();
    }

    private void Build()
    {
        var canvasGo = new GameObject("NpcAlertCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // draw above the player HUD
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);
        _text = textGo.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                     ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        _text.fontSize = 32;
        _text.fontStyle = FontStyle.Bold;
        _text.alignment = TextAnchor.MiddleCenter;  // both lines centered
        _text.lineSpacing = 1.1f;
        _text.color = Color.red;
        _text.horizontalOverflow = HorizontalWrapMode.Overflow;
        _text.verticalOverflow = VerticalWrapMode.Overflow;

        var rt = _text.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); // center of screen
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1200f, 200f);

        _text.enabled = false;
    }

    private void Update()
    {
        if (_text != null && _text.enabled && Time.time >= _hideAt)
            _text.enabled = false;
    }
}
