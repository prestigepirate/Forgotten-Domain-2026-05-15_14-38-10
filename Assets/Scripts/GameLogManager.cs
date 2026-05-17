using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace ForgottenDomain
{
    public class GameLogManager : MonoBehaviour
    {
        public static GameLogManager Instance { get; private set; }

        private GameObject _logPanel;
        private ScrollRect _scrollRect;
        private RectTransform _content;
        private List<string> _logs = new List<string>();
        
        [SerializeField] private int maxLogs = 50;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            BuildUI();
        }

        private void BuildUI()
        {
            var go = new GameObject("GameLogCanvas");
            go.transform.SetParent(transform);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();

            _logPanel = new GameObject("LogPanel");
            _logPanel.transform.SetParent(go.transform, false);
            var rt = _logPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-20, 0); rt.sizeDelta = new Vector2(400, 600);
            
            var bg = _logPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            var title = new GameObject("Title");
            title.transform.SetParent(_logPanel.transform, false);
            var trt = title.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1); trt.anchoredPosition = new Vector2(0, -5); trt.sizeDelta = new Vector2(0, 30);
            var tt = title.AddComponent<TextMeshProUGUI>();
            tt.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            tt.text = "BATTLE LOG"; tt.fontSize = 18; tt.fontStyle = FontStyles.Bold;
            tt.alignment = TextAlignmentOptions.Center; tt.color = new Color(1f, 0.8f, 0.2f);

            // Scroll View
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(_logPanel.transform, false);
            var srt = scrollView.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(10, 10); srt.offsetMax = new Vector2(-10, -40);
            
            _scrollRect = scrollView.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 20f;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            var vrt = viewport.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.sizeDelta = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0,0,0,0);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            _scrollRect.viewport = vrt;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewport.transform, false);
            _content = contentGO.AddComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _content.sizeDelta = new Vector2(0, 0);
            _scrollRect.content = _content;

            var layout = contentGO.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.spacing = 8;

            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public void Log(string message)
        {
            if (_content == null || _scrollRect == null) 
            {
                Debug.Log($"[EarlyLog] {message}");
                _logs.Add(message);
                return;
            }

            _logs.Add(message);
            if (_logs.Count > maxLogs) _logs.RemoveAt(0);

            var entry = new GameObject("LogEntry");
            entry.transform.SetParent(_content, false);
            
            var txt = entry.AddComponent<TextMeshProUGUI>();
            txt.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            txt.text = $"<color=#AAAAAA>> </color>{message}";
            txt.fontSize = 16;
            txt.color = Color.white;
            txt.textWrappingMode = TextWrappingModes.Normal;
            txt.alignment = TextAlignmentOptions.TopLeft;

            var le = entry.AddComponent<LayoutElement>();
            le.minHeight = 20;

            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
            Debug.Log($"[GameLog] {message}");
        }
    }
}
