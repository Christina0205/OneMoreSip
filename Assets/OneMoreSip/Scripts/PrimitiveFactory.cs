using UnityEngine;
using UnityEngine.UI;

namespace OneMoreSip
{
    /// <summary>
    /// Generates all placeholder primitive sprites / GameObjects at runtime.
    /// No art assets are required - everything is a tinted square or circle.
    /// </summary>
    public static class PrimitiveFactory
    {
        private static Sprite _square;
        private static Sprite _circle;
        private static Font _font;

        /// <summary>A 1x1 white square sprite (pixelsPerUnit = 1, so 1 sprite unit = 1 world unit).</summary>
        public static Sprite Square
        {
            get
            {
                if (_square == null)
                {
                    var tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    _square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }
                return _square;
            }
        }

        /// <summary>A soft white circle sprite (used for pee particles).</summary>
        public static Sprite Circle
        {
            get
            {
                if (_circle == null)
                {
                    const int size = 32;
                    var tex = new Texture2D(size, size);
                    float r = size / 2f;
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = x - r + 0.5f;
                            float dy = y - r + 0.5f;
                            bool inside = (dx * dx + dy * dy) <= (r - 0.5f) * (r - 0.5f);
                            tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                        }
                    }
                    tex.Apply();
                    _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                }
                return _circle;
            }
        }

        /// <summary>Built-in dynamic font for legacy UI Text (no asset import needed).</summary>
        public static Font UIFont
        {
            get
            {
                if (_font == null)
                {
                    // Unity 2022+ ships "LegacyRuntime.ttf"; older versions use "Arial.ttf".
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 16);
                }
                return _font;
            }
        }

        /// <summary>
        /// Creates a colored box GameObject with a SpriteRenderer scaled to <paramref name="size"/>.
        /// </summary>
        public static GameObject CreateBox(string name, Vector2 pos, Vector2 size, Color color,
                                           Transform parent = null, int sortingOrder = 0)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Square;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        /// <summary>Creates a colored circle GameObject (used for pee particles).</summary>
        public static GameObject CreateCircle(string name, Vector2 pos, float diameter, Color color,
                                              int sortingOrder = 0)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Circle;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }
    }
}
