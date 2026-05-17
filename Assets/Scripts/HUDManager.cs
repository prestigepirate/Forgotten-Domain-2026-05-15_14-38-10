using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ForgottenDomain
{
    public class HUDManager : MonoBehaviour
    {
        private Canvas _canvas;
        private TextMeshProUGUI _turnText, _timerText;
        private TextMeshProUGUI _playerLP, _playerDeck, _playerGrave;
        private TextMeshProUGUI _opponentLP, _opponentDeck, _opponentGrave;
        private GameObject _gravePanel;
        private Transform _graveContent;

        private GameManager _gm;
        private HandManager _hm;
        private AIManager _am;

        // Segmented energy bar segments (for visual updates)
        private Image[] _playerSegments;
        private Image[] _opponentSegments;

        private void Awake()
        {
            _gm = FindAnyObjectByType<GameManager>();
            _hm = FindAnyObjectByType<HandManager>();
            _am = FindAnyObjectByType<AIManager>();
            BuildUI();
            BuildGravePanel();
        }

        // ═══════════════════════════════════════════
        //  HOLOGRAPHIC TERMINAL UI
        // ═══════════════════════════════════════════

        private void BuildUI()
        {
            var go = new GameObject("HUDCanvas");
            go.transform.SetParent(transform);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();

            // ── Scanline overlay (full screen holographic noise) ──
            BuildScanlineOverlay(go.transform);

            // ── Top-center TURN header ──
            BuildTurnHeader(go.transform);

            // ── Player LP panel (top-left) ──
            BuildLPPanel(go.transform, Team.Player);

            // ── Opponent LP panel (top-right) ──
            BuildLPPanel(go.transform, Team.Opponent);

            // ── Corner frame accents (decorative L-brackets) ──
            BuildCornerAccent(go.transform, new Vector2(0, 1), new Vector2(24, -24), new Color(0f, 0.85f, 0.75f, 0.5f), false);
            BuildCornerAccent(go.transform, new Vector2(1, 1), new Vector2(-24, -24), new Color(1f, 0.25f, 0.2f, 0.5f), true);
        }

        private void BuildScanlineOverlay(Transform parent)
        {
            var scanGo = new GameObject("ScanlineOverlay");
            scanGo.transform.SetParent(parent, false);
            var srt = scanGo.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.sizeDelta = Vector2.zero;
            var simg = scanGo.AddComponent<Image>();
            simg.color = new Color(0f, 0f, 0f, 0.03f);
            simg.raycastTarget = false;

            // Create scanline texture procedurally
            int h = 1080, w = 4;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < h; y++)
            {
                float alpha = (y % 3 == 0) ? 0.12f : 0f;
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
            tex.Apply();
            simg.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
            simg.type = Image.Type.Tiled;
        }

        private void BuildTurnHeader(Transform parent)
        {
            var panel = CreateBrutalistPanel("TurnHeader", parent,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -18), new Vector2(420, 88));

            _turnText = AddGlowText(panel, "TurnText",
                new Vector2(0, 0.52f), new Vector2(1, 1),
                "TURN 1", 28, FontStyles.Bold,
                new Color(0.85f, 0.92f, 1f), new Color(0.3f, 0.65f, 0.8f, 0.5f));
            _turnText.alignment = TextAlignmentOptions.Center;

            _timerText = AddGlowText(panel, "TimerText",
                new Vector2(0, 0), new Vector2(1, 0.48f),
                "30s", 34, FontStyles.Bold,
                new Color(0f, 0.95f, 0.82f), new Color(0f, 0.7f, 0.6f, 0.5f));
            _timerText.alignment = TextAlignmentOptions.Center;
        }

        private void BuildLPPanel(Transform parent, Team team)
        {
            bool isPlayer = team == Team.Player;
            string panelName = isPlayer ? "PlayerPanel" : "OpponentPanel";
            Color accentColor = isPlayer
                ? new Color(0f, 0.85f, 0.78f)   // cyan
                : new Color(1f, 0.28f, 0.18f);   // red
            Color glowColor = isPlayer
                ? new Color(0f, 0.6f, 0.55f, 0.45f)
                : new Color(0.8f, 0.15f, 0.1f, 0.45f);
            string label = isPlayer ? "PLAYER STATION" : "ENEMY STATION";
            Color labelColor = isPlayer
                ? new Color(0.35f, 0.75f, 0.9f)
                : new Color(1f, 0.4f, 0.3f);

            Vector2 anchor = isPlayer ? new Vector2(0, 1) : new Vector2(1, 1);
            Vector2 pos = isPlayer ? new Vector2(28, -28) : new Vector2(-28, -28);

            // Outer frame
            var panel = CreateBrutalistPanel(panelName, parent,
                anchor, anchor, anchor, pos, new Vector2(300, 155));

            // Inner accent border
            var inner = new GameObject("InnerBorder");
            inner.transform.SetParent(panel.transform, false);
            var irt = inner.AddComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(4, 4); irt.offsetMax = new Vector2(-4, -4);
            var iimg = inner.AddComponent<Image>();
            iimg.color = new Color(accentColor.r * 0.15f, accentColor.g * 0.15f, accentColor.b * 0.15f, 0.6f);
            iimg.raycastTarget = false;

            // Label
            AddGlowText(panel, "Label",
                new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.98f),
                label, 14, FontStyles.Bold,
                labelColor, glowColor);

            // LP value (large)
            var lpText = AddGlowText(panel, "LP",
                new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.82f),
                "10000", 40, FontStyles.Bold,
                Color.white, accentColor * 0.6f);

            if (isPlayer) _playerLP = lpText;
            else _opponentLP = lpText;

            // "LP" prefix label
            var lpLabel = CreateText(panel, "LPLabel",
                new Vector2(0.08f, 0.48f), new Vector2(0.25f, 0.82f),
                "LP", 16, FontStyles.Bold, accentColor * 0.7f);
            lpLabel.alignment = TextAlignmentOptions.Left;
            lpLabel.raycastTarget = false;

            // Reposition LP value to right of LP label
            var lpRt = lpText.GetComponent<RectTransform>();
            lpRt.anchorMin = new Vector2(0.22f, 0.48f);
            lpRt.anchorMax = new Vector2(0.92f, 0.82f);
            lpText.alignment = TextAlignmentOptions.Right;

            // Segmented energy bar
            BuildEnergyBar(panel, accentColor, isPlayer);

            // Deck & Grave row
            var deckText = AddGlowText(panel, "Deck",
                new Vector2(0.08f, 0.05f), new Vector2(0.48f, 0.35f),
                "DECK 40", 14, FontStyles.Normal,
                new Color(0.65f, 0.7f, 0.75f), Color.clear);

            var graveText = AddGlowText(panel, "Grave",
                new Vector2(0.52f, 0.05f), new Vector2(0.92f, 0.35f),
                "GRAVE 0", 14, FontStyles.Normal,
                new Color(0.65f, 0.7f, 0.75f), Color.clear);

            if (isPlayer) { _playerDeck = deckText; _playerGrave = graveText; }
            else { _opponentDeck = deckText; _opponentGrave = graveText; }

            // Clickable grave
            var gBT = graveText.gameObject.AddComponent<Button>();
            gBT.onClick.AddListener(() => ShowGrave(team));
            var gColors = gBT.colors;
            gColors.highlightedColor = accentColor * 0.5f;
            gBT.colors = gColors;
        }

        private void BuildEnergyBar(GameObject parent, Color accentColor, bool isPlayer)
        {
            var barGo = new GameObject("EnergyBar");
            barGo.transform.SetParent(parent.transform, false);
            var brt = barGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.08f, 0.35f);
            brt.anchorMax = new Vector2(0.92f, 0.45f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;

            var layout = barGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 3;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            int segCount = 20;
            var segments = new Image[segCount];
            for (int i = 0; i < segCount; i++)
            {
                var seg = new GameObject($"Seg_{i}");
                seg.transform.SetParent(barGo.transform, false);
                var le = seg.AddComponent<LayoutElement>();
                le.flexibleWidth = 1;
                le.minWidth = 8;
                var img = seg.AddComponent<Image>();
                img.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.35f);
                img.raycastTarget = false;
                segments[i] = img;
            }

            if (isPlayer) _playerSegments = segments;
            else _opponentSegments = segments;
        }

        private void BuildCornerAccent(Transform parent, Vector2 anchor, Vector2 pos, Color color, bool flipX)
        {
            var go = new GameObject("CornerAccent");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(80, 4);
            if (flipX) rt.localScale = new Vector3(-1, 1, 1);

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            // Vertical leg
            var vGo = new GameObject("VLeg");
            vGo.transform.SetParent(go.transform, false);
            var vrt = vGo.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.zero;
            vrt.pivot = Vector2.zero;
            vrt.anchoredPosition = Vector2.zero;
            vrt.sizeDelta = new Vector2(4, 40);
            var vimg = vGo.AddComponent<Image>();
            vimg.color = color;
            vimg.raycastTarget = false;
        }

        // ═══════════════════════════════════════════
        //  BRUTALIST PANEL BUILDERS
        // ═══════════════════════════════════════════

        private GameObject CreateBrutalistPanel(string name, Transform parent,
            Vector2 amin, Vector2 amax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;

            // Deep background
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.04f, 0.94f);

            // Technically we could add a bevel with a second Image + smaller inset,
            // but that's complex. The Outline component adds the border.
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.6f, 0.55f, 0.25f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            return go;
        }

        private TextMeshProUGUI CreateText(GameObject parent, string name,
            Vector2 amin, Vector2 amax, string text, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        private TextMeshProUGUI AddGlowText(GameObject parent, string name,
            Vector2 amin, Vector2 amax, string text, float size, FontStyles style,
            Color color, Color glowColor)
        {
            var tmp = CreateText(parent, name, amin, amax, text, size, style, color);

            if (glowColor.a > 0.01f)
            {
                var outline = tmp.gameObject.AddComponent<Outline>();
                outline.effectColor = glowColor;
                outline.effectDistance = new Vector2(2, -2);
            }

            return tmp;
        }

        // ═══════════════════════════════════════════
        //  GRAVEYARD PANEL (kept functional, restyled)
        // ═══════════════════════════════════════════

        private void BuildGravePanel()
        {
            _gravePanel = new GameObject("GravePanel");
            _gravePanel.transform.SetParent(_canvas.transform, false);
            var rt = _gravePanel.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600, 800);

            var img = _gravePanel.AddComponent<Image>();
            img.color = new Color(0.02f, 0.02f, 0.05f, 0.97f);

            var outline = _gravePanel.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.7f, 0.65f, 0.3f);
            outline.effectDistance = new Vector2(2, -2);

            AddGlowText(_gravePanel, "Title",
                new Vector2(0, 0.9f), new Vector2(1, 1),
                "GRAVEYARD", 26, FontStyles.Bold,
                new Color(0f, 0.9f, 0.8f), new Color(0f, 0.6f, 0.55f, 0.4f));

            var scrollGo = new GameObject("ScrollArea");
            scrollGo.transform.SetParent(_gravePanel.transform, false);
            var srt = scrollGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.05f, 0.15f); srt.anchorMax = new Vector2(0.95f, 0.85f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vrt = viewport.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one; vrt.sizeDelta = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 0.8f);
            viewport.AddComponent<Mask>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            _graveContent = content.transform;
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.sizeDelta = new Vector2(0, 0);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 5;
            layout.childControlHeight = true; layout.childControlWidth = true;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vrt;
            scrollRect.content = crt;
            scrollRect.horizontal = false;

            var closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(_gravePanel.transform, false);
            var cbrt = closeBtn.AddComponent<RectTransform>();
            cbrt.anchorMin = new Vector2(0.5f, 0); cbrt.anchorMax = new Vector2(0.5f, 0);
            cbrt.anchoredPosition = new Vector2(0, 40); cbrt.sizeDelta = new Vector2(140, 45);
            closeBtn.AddComponent<Image>().color = new Color(0.2f, 0.06f, 0.06f);
            var btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(() => _gravePanel.SetActive(false));
            CreateText(closeBtn, "Label", Vector2.zero, Vector2.one,
                "CLOSE", 18, FontStyles.Bold, Color.white);

            _gravePanel.SetActive(false);
        }

        public void ShowGrave(Team team)
        {
            _gravePanel.SetActive(true);
            foreach (Transform child in _graveContent) Destroy(child.gameObject);

            var list = _gm.GetGraveyard(team);
            foreach (var card in list)
            {
                var item = new GameObject("CardItem");
                item.transform.SetParent(_graveContent, false);
                var itemImg = item.AddComponent<Image>();
                itemImg.color = new Color(0.08f, 0.1f, 0.16f);
                var le = item.AddComponent<LayoutElement>();
                le.minHeight = 45;
                CreateText(item, "Name", Vector2.zero, Vector2.one,
                    $"{card.cardName} ({card.cardType})", 15, FontStyles.Normal,
                    new Color(0.8f, 0.85f, 0.9f));
            }
        }

        // ═══════════════════════════════════════════
        //  UPDATE — wires unchanged, just reads values
        // ═══════════════════════════════════════════

        private void Update()
        {
            if (_gm == null) return;

            _turnText.text = $"TURN {_gm.TurnNumber} ({( _gm.CurrentTurn == TurnState.PlayerTurn ? "PLAYER" : "OPPONENT")})";
            _timerText.text = $"{Mathf.CeilToInt(_gm.TurnTimer)}s";

            if (_gm.PlayerTower != null) _playerLP.text = $"{_gm.PlayerTower.lifePoints}";
            if (_gm.OpponentTower != null) _opponentLP.text = $"{_gm.OpponentTower.lifePoints}";

            if (_hm != null) _playerDeck.text = $"DECK {_hm.DeckRemaining}";
            if (_am != null) _opponentDeck.text = $"DECK {_am.DeckRemaining}";

            _playerGrave.text = $"GRAVE {_gm.PlayerGraveCount}";
            _opponentGrave.text = $"GRAVE {_gm.OpponentGraveCount}";

            // Timer warning
            _timerText.color = _gm.TurnTimer < 10f
                ? new Color(1f, 0.3f, 0.2f)
                : new Color(0f, 0.95f, 0.82f);

            // Energy bars
            UpdateEnergyBars();
        }

        private void UpdateEnergyBars()
        {
            if (_gm.PlayerTower == null || _gm.OpponentTower == null) return;

            float playerPct = Mathf.Clamp01((float)_gm.PlayerTower.lifePoints / 10000f);
            float opponentPct = Mathf.Clamp01((float)_gm.OpponentTower.lifePoints / 10000f);

            UpdateSegmentBar(_playerSegments, playerPct,
                new Color(0f, 0.85f, 0.78f), new Color(0.3f, 0.1f, 0.1f));
            UpdateSegmentBar(_opponentSegments, opponentPct,
                new Color(1f, 0.28f, 0.18f), new Color(0.3f, 0.1f, 0.1f));
        }

        private void UpdateSegmentBar(Image[] segments, float fillPct, Color fullColor, Color dangerColor)
        {
            if (segments == null) return;
            int litSegs = Mathf.CeilToInt(fillPct * segments.Length);
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;
                if (i < litSegs)
                {
                    float t = (float)i / segments.Length;
                    segments[i].color = Color.Lerp(dangerColor, fullColor, fillPct);
                    segments[i].color = new Color(
                        segments[i].color.r,
                        segments[i].color.g,
                        segments[i].color.b,
                        0.6f + t * 0.3f);
                }
                else
                {
                    segments[i].color = new Color(fullColor.r, fullColor.g, fullColor.b, 0.12f);
                }
            }
        }
    }
}
