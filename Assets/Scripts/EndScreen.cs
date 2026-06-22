using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Reusable full-screen end / message overlay: dark background, centered white text,
/// and a Restart button that reloads the current scene.
/// Call EndScreen.Show("...") from anywhere.
/// </summary>
public class EndScreen : MonoBehaviour
{
    private static EndScreen _instance;
    private Text _text;

    public static void Show(string message)
    {
        EnsureInstance();
        _instance._text.text = message;
        _instance.gameObject.SetActive(true);
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
        // Make sure clicks work even if the scene has no EventSystem.
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvasGo = new GameObject("EndScreenCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Dark full-screen background.
        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        // Centered message.
        _text = MakeText(canvasGo.transform, 34, new Vector2(0.5f, 0.5f),
                         new Vector2(0f, 60f), new Vector2(1400f, 500f), Color.white);

        // Restart button.
        var btnGo = new GameObject("RestartButton");
        btnGo.transform.SetParent(canvasGo.transform, false);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.55f, 0.95f);
        var rt = btnImg.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(260f, 80f);
        rt.anchoredPosition = new Vector2(0f, -180f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(Restart);

        var label = MakeText(btnGo.transform, 28, new Vector2(0.5f, 0.5f),
                             Vector2.zero, new Vector2(260f, 80f), Color.white);
        label.text = "RESTART";
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
