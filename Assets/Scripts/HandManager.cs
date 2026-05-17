using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgottenDomain
{
    public class HandManager : MonoBehaviour
    {
        [SerializeField] private List<CardData> deckCards = new List<CardData>();
        [SerializeField] private int maxHandSize = 6, startingHandSize = 4;

        private Deck _deck;
        private List<CardData> _hand = new List<CardData>();
        private List<GameObject> _cardUIObjects = new List<GameObject>();
        private int _selectedIndex = -1;
        private bool _summonUsedThisTurn;
        private GameManager _gm;
        private Canvas _canvas;

        // New Card Info Panel
        private GameObject _infoPanel;
        private TextMeshProUGUI _infoName, _infoStats, _infoDesc;
        private Button _summonBtn;
        private int _viewingIndex = -1;

        private Sprite _panelFrameSprite;
        private Sprite _cardFrameSprite;

        public bool CanSummon => !_summonUsedThisTurn && _selectedIndex >= 0;
        public CardData SelectedCard => _selectedIndex >= 0 && _selectedIndex < _hand.Count ? _hand[_selectedIndex] : null;
        public int SelectedIndex => _selectedIndex;
        public int HandCount => _hand.Count;
        public int DeckRemaining => _deck != null ? _deck.Remaining : 0;
        public CardData GetCard(int index) => (index >= 0 && index < _hand.Count) ? _hand[index] : null;

        public void AISetSelection(int index)
        {
            if (index >= 0 && index < _hand.Count)
            {
                _selectedIndex = index;
                RefreshCardUI();
                if (_gm != null) _gm.OnCardSelected(SelectedCard);
            }
        }

        private void Awake() 
        { 
            _gm = FindAnyObjectByType<GameManager>(); 
        #if UNITY_EDITOR
            _panelFrameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/QuantumPanelFrame.png");
            _cardFrameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/CardFrame_WithSlot.png");
        #endif
            BuildCanvas(); 
            BuildInfoPanel(); 
        }

        private void Start() 
        { 
            _deck = new Deck(); 
            _deck.Initialize(deckCards); 
            DrawStartingHand(); 
        }

        private void BuildInfoPanel()
        {
            if (_canvas == null) BuildCanvas();

            _infoPanel = new GameObject("CardInfoPanel");
            _infoPanel.transform.SetParent(_canvas.transform, false);
            var rt = _infoPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(40, -100); 
            rt.sizeDelta = new Vector2(400, 600);

            var bg = _infoPanel.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.05f, 0.95f);
            var outline = _infoPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.8f, 1f, 0.3f);
            outline.effectDistance = new Vector2(2, -2);

            var layout = _infoPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 40, 40);
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true; 
            layout.childControlHeight = false;

            _infoName = AddText(_infoPanel, "Name", Vector2.zero, Vector2.zero, "", 28, FontStyles.Bold, Color.white);
            _infoStats = AddText(_infoPanel, "Stats", Vector2.zero, Vector2.zero, "", 20, FontStyles.Normal, new Color(0.4f, 0.9f, 1f));
            _infoDesc = AddText(_infoPanel, "Desc", Vector2.zero, Vector2.zero, "", 16, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f));
            _infoDesc.alignment = TextAlignmentOptions.TopLeft;

            // Summon Button
            var btnGo = new GameObject("SummonButton");
            btnGo.transform.SetParent(_infoPanel.transform, false);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.1f, 0.4f, 0.2f);
            _summonBtn = btnGo.AddComponent<Button>();
            var btnLe = btnGo.AddComponent<LayoutElement>();
            btnLe.minHeight = 70;
            var btnTxt = AddText(btnGo, "Label", Vector2.zero, Vector2.one, "SUMMON", 24, FontStyles.Bold, Color.white);
            _summonBtn.onClick.AddListener(OnSummonClick);

            // Close Button
            var closeGo = new GameObject("CloseButton");
            closeGo.transform.SetParent(_infoPanel.transform, false);
            closeGo.AddComponent<Image>().color = new Color(0.4f, 0.1f, 0.1f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeGo.AddComponent<LayoutElement>().minHeight = 45;
            AddText(closeGo, "Label", Vector2.zero, Vector2.one, "CLOSE", 18, FontStyles.Bold, Color.white);
            closeBtn.onClick.AddListener(() => _infoPanel.SetActive(false));

            _infoPanel.SetActive(false);
        }

        public void ShowInfoForMonster(Monster monster)
        {
            if (monster == null) return;
            
            _viewingIndex = -1; // Not viewing a card from hand
            _infoPanel.SetActive(true);
            _infoName.text = monster.DisplayName.ToUpper();
            _infoDesc.text = monster.SourceCard != null ? monster.SourceCard.description : "A summoned creature.";
            
            _infoStats.text = $"ATK {monster.Attack} | DEF {monster.Defense}\nHP {monster.CurrentHealth}/{monster.MaxHealth}";
            _infoStats.gameObject.SetActive(true);
            
            // Hide Summon button for units already on the field
            _infoPanel.transform.Find("SummonButton").gameObject.SetActive(false);
        }

        private void OnSummonClick()
        {
            if (_viewingIndex == -1) return;
            var card = GetCard(_viewingIndex);
            if (card == null) return;

            _selectedIndex = _viewingIndex;
            RefreshCardUI();
            if (_gm != null) _gm.OnCardSelected(card);
            _infoPanel.SetActive(false);
            GameLogManager.Instance?.Log($"Selected {card.cardName} for action. Click a valid tile.");
        }

        private void ShowInfo(int index)
        {
            var card = GetCard(index);
            if (card == null) return;
            
            _viewingIndex = index;
            _infoPanel.SetActive(true);
            _infoName.text = card.cardName.ToUpper();
            _infoDesc.text = card.description;

            var summonBtnObj = _infoPanel.transform.Find("SummonButton").gameObject;
            summonBtnObj.SetActive(true);
            
            if (card.IsMonsterCard)
            {
                _infoStats.text = $"ATK {card.attack} | DEF {card.defense}\nHP {card.health} | LV {card.level}";
                _infoStats.gameObject.SetActive(true);
                summonBtnObj.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = "SUMMON";
            }
            else
            {
                _infoStats.gameObject.SetActive(false);
                summonBtnObj.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = "ACTIVATE";
            }
        }

        private void BuildCanvas()
        {
            if (_canvas != null && _canvas.gameObject == null) _canvas = null;
            if (_canvas != null) return;

            var go = new GameObject("HandCanvas"); go.transform.SetParent(transform);
            go.layer = 5;
            _canvas = go.AddComponent<Canvas>(); 
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; 
            _canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>(); 
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; 
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();

            var container = new GameObject("CardContainer"); 
            container.transform.SetParent(go.transform, false);
            container.layer = 5;
            var rt = container.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); 
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 40); 
            rt.sizeDelta = new Vector2(1100, 280);
            var layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 15; 
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            
            var csf = container.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var btnGO = new GameObject("EndTurnButton"); 
            btnGO.transform.SetParent(go.transform, false);
            btnGO.layer = 5;
            var brt = btnGO.AddComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(1f, 0f); 
            brt.pivot = new Vector2(1f, 0f);
            brt.anchoredPosition = new Vector2(-20, 20); 
            brt.sizeDelta = new Vector2(140, 50);
            btnGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);
            var btn = btnGO.AddComponent<Button>(); 
            btn.onClick.AddListener(() => _gm?.EndTurn());
            AddText(btnGO, "Label", Vector2.zero, Vector2.one, "End Turn", 18, FontStyles.Bold, Color.white);

            var aiToggleGO = new GameObject("AIToggleButton"); 
            aiToggleGO.transform.SetParent(go.transform, false);
            aiToggleGO.layer = 5;
            var art = aiToggleGO.AddComponent<RectTransform>();
            art.anchorMin = art.anchorMax = new Vector2(1f, 0f); 
            art.pivot = new Vector2(1f, 0f);
            art.anchoredPosition = new Vector2(-20, 80); 
            art.sizeDelta = new Vector2(140, 50);
            aiToggleGO.AddComponent<Image>().color = new Color(0.25f, 0.15f, 0.35f);
            var aiBtn = aiToggleGO.AddComponent<Button>();
            var aiTxt = AddText(aiToggleGO, "Label", Vector2.zero, Vector2.one, "AI: OFF", 18, FontStyles.Bold, Color.white);
            aiBtn.onClick.AddListener(() => {
                _gm?.ToggleAutoPlay();
                aiTxt.text = (_gm != null && _gm.IsAutoPlay) ? "AI: ON" : "AI: OFF";
                aiToggleGO.GetComponent<Image>().color = (_gm != null && _gm.IsAutoPlay) ? new Color(0.4f, 0.2f, 0.6f) : new Color(0.25f, 0.15f, 0.35f);
            });
        }

        private void DrawStartingHand() 
        { 
            for (int i = 0; i < startingHandSize; i++) DrawCard(); 
        }

        public bool DrawCard()
        {
            if (_hand.Count >= maxHandSize) return false;
            var c = _deck.Draw(); if (c == null) return false;
            _hand.Add(c); RefreshCardUI(); return true;
        }

        public CardData PlayCard(int index)
        {
            if (index < 0 || index >= _hand.Count) return null;
            var c = _hand[index]; _hand.RemoveAt(index);
            DeselectCard(); RefreshCardUI(); return c;
        }

        public void MarkSummonUsed() { _summonUsedThisTurn = true; DeselectCard(); }
        public void DeselectCard() { _selectedIndex = -1; RefreshCardUI(); if (_gm) _gm.OnCardDeselected(); }
        public void ResetTurnState() { _summonUsedThisTurn = false; DrawCard(); }

        private void RefreshCardUI()
        {
            foreach (var o in _cardUIObjects) Destroy(o);
            _cardUIObjects.Clear();
            var container = _canvas.transform.Find("CardContainer"); if (container == null) return;
            for (int i = 0; i < _hand.Count; i++) _cardUIObjects.Add(BuildCardUI(_hand[i], i, container));
        }

        private GameObject BuildCardUI(CardData card, int index, Transform parent)
        {
            var go = new GameObject($"Card_{index}"); go.transform.SetParent(parent, false);
            go.layer = 5;
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 300);
            
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            le.preferredHeight = 300;

            var bg = go.AddComponent<Image>(); 
            bg.color = _selectedIndex == index ? new Color(0.25f, 0.5f, 0.8f) : new Color(0.15f, 0.13f, 0.18f);

            if (!string.IsNullOrEmpty(card.portraitPath))
            {
                var portraitGo = new GameObject("Portrait");
                portraitGo.transform.SetParent(go.transform, false);
                portraitGo.layer = 5;
                var prt = portraitGo.AddComponent<RectTransform>();
                prt.anchorMin = new Vector2(0.1f, 0.35f);
                prt.anchorMax = new Vector2(0.9f, 0.85f);
                prt.offsetMin = prt.offsetMax = Vector2.zero;
                var pimg = portraitGo.AddComponent<Image>();
                #if UNITY_EDITOR
                pimg.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(card.portraitPath);
                #endif
                pimg.preserveAspect = true;
                pimg.raycastTarget = false;
            }

            var btn = go.AddComponent<Button>(); 
            int ci = index; 
            btn.onClick.AddListener(() => ShowInfo(ci));

            var glow = new GameObject("Glow"); glow.transform.SetParent(go.transform, false);
            glow.layer = 5;
            var grt = glow.AddComponent<RectTransform>(); grt.anchorMin = Vector2.zero; grt.anchorMax = Vector2.one; grt.sizeDelta = new Vector2(10, 10);
            var gimg = glow.AddComponent<Image>(); gimg.color = new Color(0.9f, 0.9f, 0.6f, 0.4f);
            go.AddComponent<CardHoverHandler>().Setup(glow);

            AddText(go, "Name", new Vector2(0, 0.85f), new Vector2(1, 0.98f), card.cardName, 14, FontStyles.Bold, Color.white);
            AddText(go, "Type", new Vector2(0, 0.02f), new Vector2(1, 0.12f), card.cardType.ToString(), 11, FontStyles.Normal, new Color(0.4f, 0.9f, 1f));
            
            if (card.IsMonsterCard)
                AddText(go, "Stats", new Vector2(0, 0.12f), new Vector2(1, 0.32f), $"ATK {card.attack} | DEF {card.defense}\nHP {card.health}", 11, FontStyles.Normal, Color.white);
            else
                AddText(go, "Description", new Vector2(0, 0.12f), new Vector2(1, 0.32f), card.description, 9, FontStyles.Normal, Color.white);

            return go;
        }

        private void OnCardClicked(int index)
        {
            ShowInfo(index);
        }

        private TextMeshProUGUI AddText(GameObject parent, string name, Vector2 amin, Vector2 amax, string text, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            go.layer = 5;
            var rt = go.AddComponent<RectTransform>(); rt.anchorMin = amin; rt.anchorMax = amax;
            rt.offsetMin = new Vector2(8, 0); rt.offsetMax = new Vector2(-8, 0);
            var tmp = go.AddComponent<TextMeshProUGUI>(); 
            tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center; tmp.color = color;
            return tmp;
        }
    }
}
