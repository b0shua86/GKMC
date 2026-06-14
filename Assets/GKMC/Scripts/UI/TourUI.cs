using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GKMC
{
    /// <summary>On-screen HUD: current track + theme, the audio/YouTube status, controls, and the
    /// pop-up reader for career easter eggs.</summary>
    public class TourUI : MonoBehaviour
    {
        Text _trackText, _blurbText, _audioText, _promptText, _panelText, _bannerText;
        RawImage _panelBg;
        GameObject _panel;
        float _bannerTimer;

        public static TourUI Create(Transform parent)
        {
            var go = new GameObject("TourUI");
            go.transform.SetParent(parent, false);
            var ui = go.AddComponent<TourUI>();
            ui.BuildCanvas();
            return ui;
        }

        void BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var root = canvasGo.transform;

            // Top-left info card.
            var card = Panel(root, "InfoCard", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -30f), new Vector2(620f, 250f), new Color(0f, 0f, 0f, 0.45f));
            _trackText = MakeLabel(card, "Track", 34, TextAnchor.UpperLeft, Color.white,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(16f, -14f), new Vector2(-32f, 90f));
            _blurbText = MakeLabel(card, "Blurb", 20, TextAnchor.UpperLeft, new Color(0.9f, 0.9f, 0.92f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(16f, -108f), new Vector2(-32f, 120f));

            // Audio / YouTube status (under the card).
            _audioText = MakeLabel(root, "Audio", 18, TextAnchor.UpperLeft, new Color(0.7f, 0.95f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -290f), new Vector2(640f, 60f));

            // Center "now entering" banner.
            _bannerText = MakeLabel(root, "Banner", 52, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 160f));
            _bannerText.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
            SetAlpha(_bannerText, 0f);

            // Bottom-center easter-egg prompt.
            _promptText = MakeLabel(root, "Prompt", 24, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.5f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(900f, 40f));
            _promptText.gameObject.SetActive(false);

            // Bottom controls.
            var controls = MakeLabel(root, "Controls", 16, TextAnchor.LowerCenter, new Color(0.8f, 0.8f, 0.82f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(1400f, 30f));
            controls.text = "WASD move   ·   Mouse look   ·   Shift sprint   ·   Space jump   ·   E read easter egg   ·   Esc free cursor";

            // Easter-egg reader panel (hidden until opened).
            _panel = new GameObject("EggPanel");
            _panel.transform.SetParent(root, false);
            _panelBg = _panel.AddComponent<RawImage>();
            _panelBg.texture = Texture2D.whiteTexture;
            _panelBg.color = new Color(0.02f, 0.02f, 0.04f, 0.92f);
            var prt = _panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f); prt.sizeDelta = new Vector2(900f, 420f); prt.anchoredPosition = Vector2.zero;
            _panelText = MakeLabel(_panel.transform, "EggText", 24, TextAnchor.UpperLeft, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-60f, -60f));
            _panel.SetActive(false);
        }

        // -------------------------------------------------------------------

        public void ShowWorld(TrackInfo ti, bool audioLoaded)
        {
            SetNowPlaying(ti, audioLoaded);
            _bannerText.text = $"{ti.number:00}.  {ti.title}";
            _bannerTimer = 3.2f;
        }

        public void SetNowPlaying(TrackInfo ti, bool audioLoaded)
        {
            string feat = string.IsNullOrEmpty(ti.feature) ? "" : "  " + ti.feature;
            _trackText.text = $"<b>{ti.number:00}.  {ti.title}</b>{feat}\n<color=#FFD24B>[ {ti.theme} ]</color>";
            _trackText.supportRichText = true;
            _blurbText.text = ti.worldDesc;

            if (audioLoaded)
                _audioText.text = "♪ Now playing from the album";
            else
                _audioText.text = "♪ No audio yet — open on YouTube:\n" + ti.youtubeUrl +
                                  "\n(or drop StreamingAssets/Audio/" + ti.audioFile + ")";
        }

        public void SetEasterEggPrompt(EasterEgg egg)
        {
            if (egg == null) { _promptText.gameObject.SetActive(false); return; }
            _promptText.gameObject.SetActive(true);
            _promptText.text = $"[E]  {egg.title}  —  {egg.sub}";
        }

        public void ToggleEasterEggPanel(EasterEgg egg)
        {
            if (_panel.activeSelf) { _panel.SetActive(false); return; }
            _panelText.text = $"<size=34><b>{egg.title}</b></size>\n<color=#FFD24B>{egg.sub}</color>\n\n{egg.desc}\n\n<size=18><i>[E] to close</i></size>";
            _panelText.supportRichText = true;
            _panel.SetActive(true);
        }

        void Update()
        {
            if (_bannerTimer > 0f)
            {
                _bannerTimer -= Time.deltaTime;
                SetAlpha(_bannerText, Mathf.Clamp01(_bannerTimer));
            }
        }

        // -------- UGUI builders --------

        static RawImage Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<RawImage>();
            img.texture = Texture2D.whiteTexture;
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.sizeDelta = size; rt.anchoredPosition = anchoredPos;
            return img;
        }

        static Text MakeLabel(Transform parent, string name, int size, TextAnchor anchor, Color color,
            Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = GKMCUtil.UIFont;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
            return t;
        }

        static void SetAlpha(Text t, float a)
        {
            var c = t.color; c.a = a; t.color = c;
            t.gameObject.SetActive(a > 0.001f);
        }
    }
}
