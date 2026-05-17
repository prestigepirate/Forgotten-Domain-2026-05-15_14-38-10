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

        private void Awake()
        {
            _gm = FindAnyObjectByType<GameManager>();
            _hm = FindAnyObjectByType<HandManager>();
            _am = FindAnyObjectByType<AIManager>();
            BuildUI();
            BuildGravePanel();
        }

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

            // === Brutalist Top Bar ===
            var topBar = CreatePanel("TopBar", go.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(500, 90));
            _turnText = AddText(topBar, "TurnText", Vector2.zero, Vector2.one, "TURN 1", 26, FontStyles.Bold, new Color(0.9f, 0.9f, 0.95f));
            _timerText = AddText(topBar, "TimerText", Vector2.zero, Vector2.one, "30s", 32, FontStyles.Bold, new Color(0f, 0.95f, 0.85f));
            _turnText.alignment = TextAlignmentOptions.Top;
            _timerText.alignment = TextAlignmentOptions.Bottom;

            // Player Stats (Bottom Left) - Brutalist
            var pPanel = CreatePanel("PlayerStats", go.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20, 20), new Vector2(280, 140));
            AddText(pPanel, "Header", new Vector2(0, 0.75f), new Vector2(1, 1), "YOU", 18, FontStyles.Bold, new Color(0.4f, 0.8f, 1f));
            _playerLP = AddText(pPanel, "LP", new Vector2(0, 0.45f), new Vector2(1, 0.75f), "LP: 10000", 24, FontStyles.Normal, Color.white);
            _playerDeck = AddText(pPanel, "Deck", new Vector2(0, 0.2f), new Vector2(0.5f, 0.45f), "Deck: 40", 16, FontStyles.Normal, new Color(0.7f, 0.7f, 0.75f));
            _playerGrave = AddText(pPanel, "Grave", new Vector2(0.5f, 0.2f), new Vector2(1, 0.45f), "Grave: 0", 16, FontStyles.Normal, new Color(0.7f, 0.7f, 0.75f));
            var pGraveBtn = _playerGrave.gameObject.AddComponent<Button>();
            pGraveBtn.onClick.AddListener(() => ShowGrave(Team.Player));

            // Opponent Stats (Top Right)
            var oPanel = CreatePanel("OpponentStats", go.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20, -20), new Vector2(280, 140));
            AddText(oPanel, "Header", new Vector2(0, 0.75f), new Vector2(1, 1), "OPPONENT", 18, FontStyles.Bold, new Color(1f, 0.35f, 0.3f));
            _opponentLP = AddText(oPanel, "LP", new Vector2(0, 0.45f), new Vector2(1, 0.75f), "LP: 10000", 24, FontStyles.Normal, Color.white);
            _opponentDeck = AddText(oPanel, "Deck", new Vector2(0, 0.2f), new Vector2(0.5f, 0.45f), "Deck: 40", 16, FontStyles.Normal, new Color(0.7f, 0.7f, 0.75f));
            _opponentGrave = AddText(oPanel, "Grave", new Vector2(0.5f, 0.2f), new Vector2(1, 0.45f), "Grave: 0", 16, FontStyles.Normal, new Color(0.7f, 0.7f, 0.75f));
            var oGraveBtn = _opponentGrave.gameObject.AddComponent<Button>();
            oGraveBtn.onClick.AddListener(() => ShowGrave(Team.Opponent));
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 amin, Vector2 amax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.03f, 0.03f, 0.05f, 0.92f); // Deep brutalist dark

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.75f, 0.65f, 0.4f);
            outline.effectDistance = new Vector2(2, -2);

            return go;
        }

        private TextMeshProUGUI AddText(GameObject parent, string name, Vector2 amin, Vector2 amax, string text, float size, FontStyles style, Color color)
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
            tmp.enableWordWrapping = false;
            return tmp;
        }

        private void BuildGravePanel()
        {
            _gravePanel = new GameObject("GravePanel");
            _gravePanel.transform.SetParent(_canvas.transform, false);
            var rt = _gravePanel.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600, 800);
            
            var img = _gravePanel.AddComponent<Image>();
            img.color = new Color(0.02f, 0.02f, 0.04f, 0.97f);
            
            var title = AddText(_gravePanel, "Title", new Vector2(0, 0.9f), new Vector2(1, 1), "GRAVEYARD", 28, FontStyles.Bold, new Color(0f, 0.9f, 0.8f));
            
            // Scroll area remains mostly the same but with brutalist colors
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
            viewport.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
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
            closeBtn.AddComponent<Image>().color = new Color(0.25f, 0.08f, 0.08f);
            var btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(() => _gravePanel.SetActive(false));
            AddText(closeBtn, "Label", Vector2.zero, Vector2.one, "CLOSE", 18, FontStyles.Bold, Color.white);
            
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
                itemImg.color = new Color(0.1f, 0.12f, 0.18f);
                var le = item.AddComponent<LayoutElement>();
                le.minHeight = 45;
                AddText(item, "Name", Vector2.zero, Vector2.one, $"{card.cardName} ({card.cardType})", 16, FontStyles.Normal, new Color(0.85f, 0.9f, 0.95f));
            }
        }

        private void Update()
        {
            if (_gm == null) return;

            _turnText.text = $"TURN {_gm.TurnNumber} ({(_gm.CurrentTurn == TurnState.PlayerTurn ? "PLAYER" : "OPPONENT")})";
            _timerText.text = $"{Mathf.CeilToInt(_gm.TurnTimer)}s";
            
            if (_gm.PlayerTower != null) _playerLP.text = $"LP: {_gm.PlayerTower.lifePoints}";
            if (_gm.OpponentTower != null) _opponentLP.text = $"LP: {_gm.OpponentTower.lifePoints}";

            if (_hm != null) _playerDeck.text = $"Deck: {_hm.DeckRemaining}";
            if (_am != null) _opponentDeck.text = $"Deck: {_am.DeckRemaining}";

            _playerGrave.text = $"Grave: {_gm.PlayerGraveCount}";
            _opponentGrave.text = $"Grave: {_gm.OpponentGraveCount}";

            _timerText.color = _gm.TurnTimer < 10f ? Color.red : new Color(0f, 0.95f, 0.85f);
        }
    }
}
