using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Reusable full-screen end / message overlay: dark background, centered text, and a
/// Restart button (or press R) that reloads the current scene.
/// Call EndScreen.Show("..."). Works with the new Input System.
/// </summary>
public class EndScreen : MonoBehaviour
{
    private static EndScreen _instance;
    private Text _text;

    public static void Show(string message) => Show(message, Color.white);

    public static void Show(string message, Color color)
    {
        EnsureInstance();
        _instance._text.text = message;
        _instance._text.color = color;
        _instance.gameObject.SetActive(true);
        Time.timeScale = 0f;   // freeze the game behind the screen
    }

    private static void EnsureInstance()
    {
        if (_instance != null) return;
        var go = new GameObject("EndScreen");
        _instance = go.AddComponent<EndScreen>();
        _instance.Build();
    }

    private void Build()
    {
        var canvasGo = new GameObject("EndScreenCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        _text = MakeText(canvasGo.transform, 34, new Vector2(0.5f, 0.5f),
                         new Vector2(0f, 50f), new Vector2(1400f, 500f), Color.white);

        // Restart hint (keyboard, since the project uses the new Input System).
        var hint = MakeText(canvasGo.transform, 28, new Vector2(0.5f, 0.5f),
                            new Vector2(0f, -170f), new Vector2(800f, 80f), new Color(0.6f, 0.85f, 1f));
        hint.text = "Press R to Restart";
    }

    private void Update()
    {
        // Keyboard fallback so Restart always works even if a click is missed.
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Restart();
    }

    private void Restart()
    {
        Debug.Log("[OneMoreSip] Restart -> reloading scene.");
        Time.timeScale = 1f;
        GameSession.DrunkLevel = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private Text MakeText(Transform parent, int size, Vector2 anchorPivot, Vector2 pos,
                          Vector2 sizeDelta, Color color)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchorPivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        return t;
    }
}
