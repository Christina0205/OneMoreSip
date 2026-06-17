using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OneMoreSip
{
    /// <summary>
    /// Builds and updates all runtime UI (legacy uGUI, no TextMeshPro needed):
    /// walking HUD, pee HUD, NPC messages and the ending screen with a restart button.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private Canvas _canvas;

        // Walking HUD
        private GameObject _walkPanel;
        private Text _wDrunk, _wBottle, _wEffect, _wHolding, _wNpc;
        private float _npcMsgUntil;

        // Pee HUD
        private GameObject _peePanel;
        private Text _pDrunk, _pScore, _pAngle, _pAmount, _pFloor;

        // Ending
        private GameObject _endPanel;
        private Text _endText;

        private void Awake()
        {
            BuildCanvas();
            BuildWalkingHud();
            BuildPeeHud();
            BuildEndingScreen();
            HideAll();
        }

        // -------------------------------------------------------------------
        // Canvas / EventSystem
        // -------------------------------------------------------------------
        private void BuildCanvas()
        {
            var go = new GameObject("Canvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            go.AddComponent<GraphicRaycaster>();

            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        // -------------------------------------------------------------------
        // HUD builders
        // -------------------------------------------------------------------
        private void BuildWalkingHud()
        {
            _walkPanel = NewPanel("WalkHUD");
            _wDrunk   = Label(_walkPanel.transform, new Vector2(20, -20), 600, 28, 22, TextAnchor.UpperLeft, Color.white);
            _wBottle  = Label(_walkPanel.transform, new Vector2(20, -52), 600, 28, 22, TextAnchor.UpperLeft, Color.white);
            _wEffect  = Label(_walkPanel.transform, new Vector2(20, -84), 600, 28, 22, TextAnchor.UpperLeft, new Color(1f, 0.85f, 0.4f));
            _wHolding = Label(_walkPanel.transform, new Vector2(20, -116), 600, 28, 22, TextAnchor.UpperLeft, Color.white);

            _wNpc = Label(_walkPanel.transform, new Vector2(0, -24), 1100, 36, 26, TextAnchor.UpperCenter, new Color(1f, 0.35f, 0.3f));
            Anchor(_wNpc.rectTransform, new Vector2(0.5f, 1f));

            var hint = Label(_walkPanel.transform, new Vector2(0, 14), 1200, 26, 18, TextAnchor.LowerCenter, new Color(0.8f, 0.8f, 0.85f));
            Anchor(hint.rectTransform, new Vector2(0.5f, 0f));
            hint.text = "A/D move   E drink   Shift hide bottle   Reach the blue dorm on the right";
        }

        private void BuildPeeHud()
        {
            _peePanel = NewPanel("PeeHUD");
            _pDrunk  = Label(_peePanel.transform, new Vector2(20, -20), 600, 28, 22, TextAnchor.UpperLeft, Color.white);
            _pScore  = Label(_peePanel.transform, new Vector2(20, -52), 600, 28, 22, TextAnchor.UpperLeft, Color.white);
            _pAngle  = Label(_peePanel.transform, new Vector2(20, -84), 600, 28, 22, TextAnchor.UpperLeft, new Color(0.6f, 0.9f, 1f));
            _pAmount = Label(_peePanel.transform, new Vector2(20, -116), 600, 28, 22, TextAnchor.UpperLeft, Color.white);
            _pFloor  = Label(_peePanel.transform, new Vector2(20, -148), 600, 28, 22, TextAnchor.UpperLeft, Color.white);

            var hint = Label(_peePanel.transform, new Vector2(0, 14), 1200, 26, 18, TextAnchor.LowerCenter, new Color(0.8f, 0.8f, 0.85f));
            Anchor(hint.rectTransform, new Vector2(0.5f, 0f));
            hint.text = "W/S aim   Hold Space to pee   Red painting +50  Cyan urinal +20  Yellow trash +10  Floor -10";
        }

        private void BuildEndingScreen()
        {
            _endPanel = NewPanel("EndingHUD");
            var bg = _endPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.82f);
            Stretch(_endPanel.GetComponent<RectTransform>());

            _endText = Label(_endPanel.transform, new Vector2(0, 60), 1000, 320, 30, TextAnchor.MiddleCenter, Color.white);
            Anchor(_endText.rectTransform, new Vector2(0.5f, 0.5f));

            // Restart button
            var btnGo = new GameObject("RestartButton");
            btnGo.transform.SetParent(_endPanel.transform, false);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.25f, 0.55f, 0.95f);
            var rt = btnGo.GetComponent<RectTransform>();
            Anchor(rt, new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(240, 70);
            rt.anchoredPosition = new Vector2(0, -150);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(OnRestartClicked);

            var btnText = Label(btnGo.transform, Vector2.zero, 240, 70, 26, TextAnchor.MiddleCenter, Color.white);
            Anchor(btnText.rectTransform, new Vector2(0.5f, 0.5f));
            btnText.rectTransform.anchoredPosition = Vector2.zero;
            btnText.text = "RESTART";
        }

        private void OnRestartClicked()
        {
            GameManager.Instance.Restart();
        }

        // -------------------------------------------------------------------
        // Mode switching
        // -------------------------------------------------------------------
        public void SwitchToWalking()
        {
            HideAll();
            _walkPanel.SetActive(true);
            _wNpc.text = "";
        }

        public void SwitchToPee()
        {
            HideAll();
            _peePanel.SetActive(true);
        }

        public void ShowEnding(bool madeItDrunk)
        {
            HideAll();
            _endPanel.SetActive(true);
            var gm = GameManager.Instance;

            if (madeItDrunk)
            {
                _endText.text =
                    "You made it back to the dorm.\n\n" +
                    $"Final Drunk Level: {Mathf.RoundToInt(gm.DrunkLevel)}\n" +
                    $"Final Pee Score: {gm.PeeScore}\n" +
                    $"Floor Hits: {gm.FloorHits}";
            }
            else
            {
                _endText.text =
                    "You are not that drunk and still anxious.\nYou have problems going to sleep.\n\n" +
                    $"Final Drunk Level: {Mathf.RoundToInt(gm.DrunkLevel)}";
            }
        }

        public void ShowNpcMessage(string msg, float duration)
        {
            if (_wNpc == null) return;
            _wNpc.text = msg;
            _npcMsgUntil = Time.time + duration;
        }

        private void HideAll()
        {
            _walkPanel.SetActive(false);
            _peePanel.SetActive(false);
            _endPanel.SetActive(false);
        }

        // -------------------------------------------------------------------
        // Live updates
        // -------------------------------------------------------------------
        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.Mode == GameMode.Walking)
            {
                int drunk = Mathf.RoundToInt(gm.DrunkLevel);
                _wDrunk.text = $"Drunk Level: {drunk} / 100";
                if (gm.Bottle != null)
                {
                    _wBottle.text = gm.Bottle.Holding
                        ? $"Bottle Progress: {Mathf.CeilToInt(gm.Bottle.BottleProgress)} / 20" +
                          (gm.Bottle.IsDrinking ? "  (drinking...)" : "")
                        : "Bottle Progress: -";
                    _wHolding.text = "Holding Bottle: " + (gm.Bottle.Holding ? "Yes" : "No") +
                                     (gm.Bottle.IsHidden ? "  (hidden)" : "");
                }
                _wEffect.text = "Drunk Effect: " +
                    (gm.Effects != null ? gm.Effects.CurrentEffectName : "None");

                if (Time.time > _npcMsgUntil) _wNpc.text = "";
            }
            else if (gm.Mode == GameMode.Pee && gm.Pee != null)
            {
                _pDrunk.text  = $"Drunk Level: {Mathf.RoundToInt(gm.DrunkLevel)} / 100";
                _pScore.text  = $"Pee Score: {gm.PeeScore}";
                _pAngle.text  = $"Angle: {Mathf.RoundToInt(gm.Pee.AngleDeg)}°";
                _pAmount.text = $"Pee Amount: {gm.Pee.PeeAmount}";
                _pFloor.text  = $"Floor Hits: {gm.FloorHits}";
            }
        }

        // -------------------------------------------------------------------
        // Small UI helpers
        // -------------------------------------------------------------------
        private GameObject NewPanel(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            Stretch(rt);
            return go;
        }

        private Text Label(Transform parent, Vector2 anchoredPos, float w, float h, int fontSize,
                           TextAnchor align, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = PrimitiveFactory.UIFont;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            var rt = t.rectTransform;
            Anchor(rt, new Vector2(0f, 1f)); // default top-left
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = anchoredPos;
            return t;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
