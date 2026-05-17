using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem.UI;

namespace ForgottenDomain
{
    public class MainMenuManager : MonoBehaviour
    {
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private TMP_FontAsset _font;
        private Sprite _logoSprite;
        private Sprite _buttonFrameSprite;
        private Sprite _bgSprite;
        private Sprite _panelFrameSprite;
        private Texture _starsTexture;
        private Texture _nebulaTexture;
        
        private RawImage _starsRaw;
        private RawImage _nebulaRaw;
        private RectTransform _centerPanelRT;
        private Outline _centerPanelOutline;

        private void Awake()
        {
            EnsureEventSystem();
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            _logoSprite = AssetDatabaseLoad<Sprite>("Assets/Sprites/ForgottenDomainLogo_V2.png");
            _buttonFrameSprite = AssetDatabaseLoad<Sprite>("Assets/Sprites/SciFiButtonFrame.png");
            _bgSprite = AssetDatabaseLoad<Sprite>("Assets/Sprites/QuantumBackground.png");
            _panelFrameSprite = AssetDatabaseLoad<Sprite>("Assets/Sprites/QuantumPanelFrame.png");
            _starsTexture = AssetDatabaseLoad<Texture>("Assets/Sprites/StarsTileable.png");
            _nebulaTexture = AssetDatabaseLoad<Texture>("Assets/Sprites/NebulaTileable.png");
            
            CreateMainMenu();
            StartCoroutine(FadeIn());
        }

        private void Update()
        {
            AnimateBackground();
            AnimatePanel();
        }

        private void AnimateBackground()
        {
            if (_starsRaw != null)
            {
                Rect r = _starsRaw.uvRect;
                r.x += Time.unscaledDeltaTime * 0.005f;
                r.y += Time.unscaledDeltaTime * 0.002f;
                _starsRaw.uvRect = r;
            }
            if (_nebulaRaw != null)
            {
                Rect r = _nebulaRaw.uvRect;
                r.x += Time.unscaledDeltaTime * 0.015f;
                r.y -= Time.unscaledDeltaTime * 0.005f;
                _nebulaRaw.uvRect = r;
            }
        }

        private void AnimatePanel()
        {
            if (_centerPanelRT != null)
            {
                // Floating effect
                float floatY = Mathf.Sin(Time.unscaledTime * 0.8f) * 10f;
                _centerPanelRT.anchoredPosition = new Vector2(0, floatY);

                // Subtle scaling "breathing"
                float scale = 1.0f + Mathf.Sin(Time.unscaledTime * 0.4f) * 0.01f;
                _centerPanelRT.localScale = new Vector3(scale, scale, 1);
            }

            if (_centerPanelOutline != null)
            {
                // Pulsing glow
                float pulse = 0.2f + (Mathf.Sin(Time.unscaledTime * 1.5f) + 1f) * 0.2f;
                Color c = _centerPanelOutline.effectColor;
                c.a = pulse;
                _centerPanelOutline.effectColor = c;
            }
        }

        private T AssetDatabaseLoad<T>(string path) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return null; // In build, you'd use Addressables or Resources
#endif
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<InputSystemUIInputModule>();
            }
        }

        private void CreateMainMenu()
        {
            // ── Canvas ──
            GameObject canvasGO = new GameObject("MainMenuCanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            // ── Quantum Background ──
            BuildBackground(_canvas.transform);

            // ── Atmospheric Particles / Scanlines ──
            BuildScanlineOverlay(_canvas.transform);

            // ── Decorative Frame Accents ──
            Color quantumCyan = new Color(0f, 0.9f, 1f, 0.6f);
            Color quantumPurple = new Color(0.6f, 0.2f, 1f, 0.4f);
            
            BuildCornerAccent(_canvas.transform, new Vector2(0, 1), new Vector2(40, -40), quantumCyan, false);
            BuildCornerAccent(_canvas.transform, new Vector2(1, 1), new Vector2(-40, -40), quantumPurple, true);
            BuildCornerAccent(_canvas.transform, new Vector2(0, 0), new Vector2(40, 40), quantumPurple, false);
            BuildCornerAccent(_canvas.transform, new Vector2(1, 0), new Vector2(-40, 40), quantumCyan, true);

            // ── Central content panel ──
            var centerPanel = CreateQuantumPanel("CenterPanel", _canvas.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(850, 800));

            // ── Logo instead of Title Text ──
            if (_logoSprite != null)
                CreateLogoImage(_logoSprite, centerPanel, new Vector2(0, 240));
            else
                CreateTitleText("FORGOTTEN DOMAIN", centerPanel, new Vector2(0, 240));

            // ── Subtitle ──
            CreateSubtitleText("A SYNCINGSHIPS PROJECT", centerPanel, new Vector2(0, 140));

            // ── Divider ──
            BuildDivider(centerPanel, new Vector2(0, 100), 600);

            // ── Buttons ──
            float startY = 0f;
            float spacing = -110f;
            
            CreateQuantumButton("Initialize", centerPanel, new Vector2(0, startY), () => StartCoroutine(LaunchGame()));
            CreateQuantumButton("Neural Deck", centerPanel, new Vector2(0, startY + spacing));
            CreateQuantumButton("Manifest", centerPanel, new Vector2(0, startY + spacing * 2));
            CreateQuantumButton("Protocols", centerPanel, new Vector2(0, startY + spacing * 3));
            CreateQuantumButton("Disconnect", centerPanel, new Vector2(0, startY + spacing * 4), () => Application.Quit());

            // ── Footer ──
            CreateFooterText("SYNCINGSHIPS // QUANTUM ENGINE // [SECURE CONNECTION ESTABLISHED]", centerPanel, new Vector2(0, -430));
        }

        private void CreateLogoImage(Sprite sprite, GameObject parent, Vector2 position)
        {
            var go = new GameObject("Logo");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(600, 300);
            
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            
            // Subtle glow outline for the logo
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.8f, 1f, 0.3f);
            shadow.effectDistance = new Vector2(4, -4);
        }

        private GameObject CreateQuantumPanel(string name, Transform parent,
            Vector2 amin, Vector2 amax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            _centerPanelRT = go.AddComponent<RectTransform>();
            _centerPanelRT.anchorMin = amin; _centerPanelRT.anchorMax = amax; _centerPanelRT.pivot = pivot;
            _centerPanelRT.anchoredPosition = pos; _centerPanelRT.sizeDelta = size;

            var bg = go.AddComponent<Image>();
            if (_panelFrameSprite != null)
            {
                bg.sprite = _panelFrameSprite;
                bg.type = Image.Type.Sliced;
                bg.color = new Color(1, 1, 1, 0.95f);
            }
            else
            {
                bg.color = new Color(0.005f, 0.005f, 0.01f, 0.85f); // Deep abyss black
            }

            _centerPanelOutline = go.AddComponent<Outline>();
            _centerPanelOutline.effectColor = new Color(0f, 0.7f, 0.8f, 0.3f);
            _centerPanelOutline.effectDistance = new Vector2(3f, -3f);

            // Add decorative Readouts
            CreateReadout("READOUT_A", go.transform, new Vector2(-380, 360), "QUANTUM_STABILITY: 98.4%");
            CreateReadout("READOUT_B", go.transform, new Vector2(380, 360), "NEURAL_SYNC: ACTIVE");

            return go;
        }

        private void CreateReadout(string name, Transform parent, Vector2 pos, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(250, 30);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) tmp.font = _font;
            tmp.text = text;
            tmp.fontSize = 10;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0f, 0.9f, 1f, 0.4f);
            tmp.characterSpacing = 2f;
        }

        private void CreateQuantumButton(string label, GameObject parent, Vector2 position,
            UnityEngine.Events.UnityAction onClick = null)
        {
            var btnGO = new GameObject(label + "Button");
            btnGO.transform.SetParent(parent.transform, false);
            var rt = btnGO.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(500, 80);

            var img = btnGO.AddComponent<Image>();
            if (_buttonFrameSprite != null)
            {
                img.sprite = _buttonFrameSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = new Color(1f, 1f, 1f, 0.9f);

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = new Color(1, 1, 1, 0.6f);
            colors.highlightedColor = new Color(1, 1, 1, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 1f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick ?? (() => Debug.Log(label + " initialized")));

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(btnGO.transform, false);
            var trt = textGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            if (_font != null) tmp.font = _font;
            tmp.text = label.ToUpper();
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.4f, 0.95f, 1f);
            tmp.characterSpacing = 5f;
            tmp.raycastTarget = false;
        }

        // Keep existing helper methods but adjust colors slightly
        private void BuildScanlineOverlay(Transform parent)
        {
            var scanGo = new GameObject("ScanlineOverlay");
            scanGo.transform.SetParent(parent, false);
            var srt = scanGo.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.sizeDelta = Vector2.zero;

            var simg = scanGo.AddComponent<Image>();
            simg.color = new Color(0f, 0.8f, 1f, 0.03f); // Tinted cyan
            simg.raycastTarget = false;

            int h = 1080, w = 4;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < h; y++)
            {
                float alpha = (y % 4 == 0) ? 0.08f : 0f; // Sharper scanlines
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
            tex.Apply();
            simg.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
            simg.type = Image.Type.Tiled;
        }

        private void BuildBackground(Transform parent)
        {
            // Base Static Background
            var bg = new GameObject("BackgroundBase");
            bg.transform.SetParent(parent, false);
            var rt = bg.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            bg.transform.SetAsFirstSibling();

            var img = bg.AddComponent<Image>();
            if (_bgSprite != null)
            {
                img.sprite = _bgSprite;
                img.color = new Color(0.3f, 0.3f, 0.35f, 1f); 
            }
            else
            {
                img.color = new Color(0.002f, 0.005f, 0.01f, 1f);
            }
            img.raycastTarget = false;

            // Nebula Parallax Layer
            if (_nebulaTexture != null)
            {
                var nebGO = new GameObject("NebulaLayer");
                nebGO.transform.SetParent(parent, false);
                var nrt = nebGO.AddComponent<RectTransform>();
                nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one;
                nrt.sizeDelta = Vector2.zero;
                nebGO.transform.SetSiblingIndex(1);

                _nebulaRaw = nebGO.AddComponent<RawImage>();
                _nebulaRaw.texture = _nebulaTexture;
                _nebulaRaw.color = new Color(1, 1, 1, 0.4f);
                _nebulaRaw.raycastTarget = false;
            }

            // Stars Parallax Layer
            if (_starsTexture != null)
            {
                var starsGO = new GameObject("StarsLayer");
                starsGO.transform.SetParent(parent, false);
                var srt = starsGO.AddComponent<RectTransform>();
                srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
                srt.sizeDelta = Vector2.zero;
                starsGO.transform.SetSiblingIndex(2);

                _starsRaw = starsGO.AddComponent<RawImage>();
                _starsRaw.texture = _starsTexture;
                _starsRaw.color = new Color(1, 1, 1, 0.7f);
                _starsRaw.raycastTarget = false;
            }
        }

        private void BuildCornerAccent(Transform parent, Vector2 anchor, Vector2 pos, Color color, bool flipX)
        {
            var go = new GameObject("CornerAccent");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(150, 2);
            
            // Apply scale flips
            Vector3 scale = Vector3.one;
            if (flipX) scale.x = -1;
            if (anchor.y > 0.5f) scale.y = -1; // Flip vertically for top corners
            rt.localScale = scale;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            var vGo = new GameObject("VLeg");
            vGo.transform.SetParent(go.transform, false);
            var vrt = vGo.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.zero;
            vrt.pivot = Vector2.zero;
            vrt.anchoredPosition = Vector2.zero;
            vrt.sizeDelta = new Vector2(2, 75);
            var vimg = vGo.AddComponent<Image>();
            vimg.color = color;
            vimg.raycastTarget = false;
        }

        private void BuildDivider(GameObject parent, Vector2 position, float width)
        {
            var go = new GameObject("Divider");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(width, 1);

            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 1f, 0.95f, 0.4f);
            img.raycastTarget = false;
        }

        private void CreateTitleText(string text, GameObject parent, Vector2 position)
        {
            var go = new GameObject("TitleFallback");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(700, 120);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) tmp.font = _font;
            tmp.text = text;
            tmp.fontSize = 84;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.4f, 0.9f, 1f);
            tmp.characterSpacing = 10f;
        }

        private void CreateSubtitleText(string text, GameObject parent, Vector2 position)
        {
            var go = new GameObject("Subtitle");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(600, 40);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) tmp.font = _font;
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0f, 0.7f, 1f, 0.8f);
            tmp.characterSpacing = 8f;
        }

        private void CreateFooterText(string text, GameObject parent, Vector2 position)
        {
            var go = new GameObject("Footer");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(800, 30);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) tmp.font = _font;
            tmp.text = text;
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.2f, 0.5f, 0.6f, 0.6f);
            tmp.characterSpacing = 2f;
        }

        private IEnumerator FadeIn()
        {
            float duration = 1.2f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                
                // Add tech flicker
                if (elapsed < 0.5f && Random.value > 0.9f)
                    _canvasGroup.alpha = 0f;
                else
                    _canvasGroup.alpha = alpha;
                    
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }

        private IEnumerator LaunchGame()
        {
            float duration = 0.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = 0f;
            SceneManager.LoadScene("Battlefield");
        }
    }
}
