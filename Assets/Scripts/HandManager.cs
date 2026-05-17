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
        private GameObject _inspectPanel;
        private TextMeshProUGUI _inspectNameText, _inspectDescText, _inspectStatsText;
        private Transform _inspectButtonsContainer;
        private int _inspectingIndex = -1;

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

        private void Awake() { _gm = FindAnyObjectByType<GameManager>(); BuildCanvas(); BuildInspectPanel(); }
        private void Start() { _deck = new Deck(); _deck.Initialize(deckCards); DrawStartingHand(); }

        private void BuildInspectPanel()
        {
            if (_canvas == null) BuildCanvas();

            if (_inspectPanel != null) Destroy(_inspectPanel);

            _inspectPanel = new GameObject("InspectPanel");
            _inspectPanel.transform.SetParent(_canvas.transform, false);
            var rt = _inspectPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(20, 0); rt.sizeDelta = new Vector2(400, 700);
            
            var bg = _inspectPanel.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.08f, 0.08f, 0.1f, 0.98f);

            var layout = _inspectPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(25, 25, 25, 25);
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false; // Important: don't stretch children

            _inspectNameText = AddTextWithLayout(_inspectPanel, "Name", 28, FontStyles.Bold, Color.white);
            _inspectStatsText = AddTextWithLayout(_inspectPanel, "Stats", 20, FontStyles.Normal, new Color(1f, 0.8f, 0.2f));
            _inspectDescText = AddTextWithLayout(_inspectPanel, "Description", 18, FontStyles.Normal, Color.white);
            _inspectDescText.alignment = TextAlignmentOptions.TopLeft;

            var btnArea = new GameObject("Buttons");
            btnArea.transform.SetParent(_inspectPanel.transform, false);
            _inspectButtonsContainer = btnArea.transform;
            var btnLayout = btnArea.AddComponent<VerticalLayoutGroup>();
            btnLayout.spacing = 15;
            btnLayout.childControlWidth = true;
            btnLayout.childControlHeight = true;
            btnLayout.childForceExpandHeight = false;
            
            var fitter = btnArea.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(_inspectPanel.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleHeight = 1;

            var closeBtnGO = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(_inspectPanel.transform, false);
            var cimg = closeBtnGO.AddComponent<UnityEngine.UI.Image>(); cimg.color = new Color(0.4f, 0.15f, 0.15f);
            var cbtn = closeBtnGO.AddComponent<UnityEngine.UI.Button>();
            cbtn.targetGraphic = cimg;
            cbtn.onClick.AddListener(HideInspectPanel);
            var cle = closeBtnGO.AddComponent<LayoutElement>();
            cle.minHeight = 45; cle.preferredHeight = 45;
            AddText(closeBtnGO, "Label", Vector2.zero, Vector2.one, "Close", 18, FontStyles.Bold, Color.white);

            _inspectPanel.SetActive(false);
        }

        private TextMeshProUGUI AddTextWithLayout(GameObject parent, string name, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = -1; // Use TMP's preferred height
            return tmp;
        }

        private void BuildCanvas()
        {
            // If we have a canvas but it was destroyed, clean up reference
            if (_canvas != null && _canvas.gameObject == null) _canvas = null;

            if (_canvas != null) return;

            var go = new GameObject("HandCanvas"); go.transform.SetParent(transform);
            _canvas = go.AddComponent<Canvas>(); _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();

            var container = new GameObject("CardContainer"); container.transform.SetParent(go.transform, false);
            var rt = container.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 40); rt.sizeDelta = new Vector2(900, 180);
            var layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12; layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            var btnGO = new GameObject("EndTurnButton"); btnGO.transform.SetParent(go.transform, false);
            var brt = btnGO.AddComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(1f, 0f); brt.pivot = new Vector2(1f, 0f);
            brt.anchoredPosition = new Vector2(-20, 20); brt.sizeDelta = new Vector2(140, 50);
            btnGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);
            var btn = btnGO.AddComponent<Button>(); btn.onClick.AddListener(() => _gm?.EndTurn());
            var lbl = new GameObject("Label"); lbl.transform.SetParent(btnGO.transform, false);
            var lrt = lbl.AddComponent<RectTransform>(); lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
            var txt = lbl.AddComponent<TextMeshProUGUI>(); txt.text = "End Turn"; txt.fontSize = 18; txt.alignment = TextAlignmentOptions.Center; txt.color = Color.white;

            var aiToggleGO = new GameObject("AIToggleButton"); aiToggleGO.transform.SetParent(go.transform, false);
            var art = aiToggleGO.AddComponent<RectTransform>();
            art.anchorMin = art.anchorMax = new Vector2(1f, 0f); art.pivot = new Vector2(1f, 0f);
            art.anchoredPosition = new Vector2(-20, 80); art.sizeDelta = new Vector2(140, 50);
            aiToggleGO.AddComponent<Image>().color = new Color(0.25f, 0.15f, 0.35f);
            var aiBtn = aiToggleGO.AddComponent<Button>();
            var aiLbl = new GameObject("Label"); aiLbl.transform.SetParent(aiToggleGO.transform, false);
            var ailrt = aiLbl.AddComponent<RectTransform>(); ailrt.anchorMin = Vector2.zero; ailrt.anchorMax = Vector2.one; ailrt.sizeDelta = Vector2.zero;
            var aiTxt = aiLbl.AddComponent<TextMeshProUGUI>(); aiTxt.text = "AI: OFF"; aiTxt.fontSize = 18; aiTxt.alignment = TextAlignmentOptions.Center; aiTxt.color = Color.white;
            aiBtn.onClick.AddListener(() => {
                _gm?.ToggleAutoPlay();
                aiTxt.text = (_gm != null && _gm.IsAutoPlay) ? "AI: ON" : "AI: OFF";
                aiToggleGO.GetComponent<Image>().color = (_gm != null && _gm.IsAutoPlay) ? new Color(0.4f, 0.2f, 0.6f) : new Color(0.25f, 0.15f, 0.35f);
            });
        }

        private void DrawStartingHand() 
        { 
            // Debug: Ensure the animated monster card is in the starting hand if available
            int foundIdx = -1;
            for (int i = 0; i < deckCards.Count; i++)
            {
                if (deckCards[i] != null && !string.IsNullOrEmpty(deckCards[i].modelPath))
                {
                    foundIdx = i;
                    break;
                }
            }

            if (foundIdx != -1)
            {
                // Force it to the top of the draw pile for now (simple way to ensure it's drawn)
                // or just manually add it to hand if we can.
                // Let's just draw 4 normally, but if one of them isn't the animated one, 
                // swap the first one drawn with it.
            }

            for (int i = 0; i < startingHandSize; i++) DrawCard(); 

            // Check if we got an animated card, if not, force the first one to be it
            bool hasAnimated = _hand.Exists(c => !string.IsNullOrEmpty(c.modelPath));
            if (!hasAnimated && foundIdx != -1)
            {
                _hand[0] = deckCards[foundIdx];
                RefreshCardUI();
            }
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
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(180, 160);
            var bg = go.AddComponent<Image>(); bg.color = _selectedIndex == index ? new Color(0.25f, 0.5f, 0.8f) : new Color(0.15f, 0.13f, 0.18f);
            var btn = go.AddComponent<Button>(); int ci = index; btn.onClick.AddListener(() => OnCardClicked(ci));

            var glow = new GameObject("Glow"); glow.transform.SetParent(go.transform, false);
            var grt = glow.AddComponent<RectTransform>(); grt.anchorMin = Vector2.zero; grt.anchorMax = Vector2.one; grt.sizeDelta = new Vector2(10, 10);
            var gimg = glow.AddComponent<Image>(); gimg.color = new Color(0.9f, 0.9f, 0.6f, 0.4f);
            go.AddComponent<CardHoverHandler>().Setup(glow);

            AddText(go, "Name", new Vector2(0, 0.65f), new Vector2(1, 0.95f), card.cardName, 14, FontStyles.Bold, Color.white);
            AddText(go, "Type", new Vector2(0, 0.45f), new Vector2(1, 0.62f), card.cardType.ToString(), 11, FontStyles.Normal, new Color(0.6f, 0.35f, 0.15f));
            
            if (card.IsMonsterCard)
            {
                AddText(go, "Stats", new Vector2(0, 0.1f), new Vector2(1, 0.42f), $"ATK {card.attack}  DEF {card.defense}\nHP {card.health}  Lv.{card.level}", 11, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f));
            }
            else
            {
                AddText(go, "Description", new Vector2(0, 0.05f), new Vector2(1, 0.42f), card.description, 9, FontStyles.Normal, Color.white);
            }
            return go;
        }

        private TextMeshProUGUI AddText(GameObject parent, string name, Vector2 amin, Vector2 amax, string text, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>(); rt.anchorMin = amin; rt.anchorMax = amax;
            rt.offsetMin = new Vector2(8, 0); rt.offsetMax = new Vector2(-8, 0);
            var tmp = go.AddComponent<TextMeshProUGUI>(); tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center; tmp.color = color;
            return tmp;
        }

        private void OnCardClicked(int index)
        {
            var card = GetCard(index);
            if (card == null) return;
            ShowInspectPanel(card, index);
        }

        public void ShowInspectPanel(CardData card, int index)
        {
            if (_inspectPanel == null || _inspectButtonsContainer == null)
            {
                BuildInspectPanel();
                if (_inspectPanel == null)
                {
                    Debug.LogError("[HandManager] Failed to build Inspect Panel!");
                    return;
                }
            }

            _inspectingIndex = index;
            _inspectPanel.SetActive(true);
            
            Debug.Log($"[HandManager] Inspecting Card: {card.cardName} | Type: {card.cardType}");

            if (_inspectNameText != null) _inspectNameText.text = card.cardName;
            if (_inspectDescText != null) _inspectDescText.text = card.description;

            if (card.IsMonsterCard)
            {
                if (_inspectStatsText != null)
                {
                    _inspectStatsText.gameObject.SetActive(true);
                    _inspectStatsText.text = $"ATK {card.attack} | DEF {card.defense}\nHP {card.health} | LV {card.level}";
                }
            }
            else
            {
                if (_inspectStatsText != null) _inspectStatsText.gameObject.SetActive(false);
            }

            // Clear existing buttons
            if (_inspectButtonsContainer != null)
            {
                foreach (Transform child in _inspectButtonsContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            if (card.IsMonsterCard)
            {
                Debug.Log("[HandManager] Creating Summon button.");
                CreateInspectButton("Summon", () => { 
                    _selectedIndex = index; 
                    RefreshCardUI(); 
                    if (_gm != null) _gm.OnCardSelected(card); 
                    HideInspectPanel(); 
                });
            }
            else if (card.IsMagicCard)
            {
                Debug.Log("[HandManager] Creating Cast button.");
                CreateInspectButton("Cast", () => { 
                    _selectedIndex = index; 
                    RefreshCardUI(); 
                    if (_gm != null) _gm.OnCardSelected(card); 
                    HideInspectPanel(); 
                });
            }
            else if (card.IsTrapCard)
            {
                Debug.Log("[HandManager] Creating Condemned button.");
                CreateInspectButton("Condemned", () => { 
                    _selectedIndex = index; 
                    RefreshCardUI(); 
                    if (_gm != null) _gm.OnCardSelected(card); 
                    HideInspectPanel(); 
                });
            }
        }

        private void CreateInspectButton(string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(_inspectButtonsContainer, false);
            
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 60;
            le.preferredHeight = 60;
            le.flexibleWidth = 1;

            var img = go.AddComponent<UnityEngine.UI.Image>(); 
            img.color = new Color(0.15f, 0.45f, 0.2f); // Slightly brighter green
            
            var btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(action);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            tmp.text = label.ToUpper();
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        public void HideInspectPanel()
        {
            _inspectPanel.SetActive(false);
            _inspectingIndex = -1;
        }
}
}
