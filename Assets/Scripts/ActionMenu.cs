using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ForgottenDomain
{
    public class ActionMenu : MonoBehaviour
    {
        private GameObject _panel;
        private Button _moveButton;
        private Button _attackButton;
        private Monster _targetMonster;
        private Canvas _canvas;

        private void Awake()
        {
            BuildUI();
            Hide();
        }

        private void BuildUI()
        {
            var go = new GameObject("ActionMenuCanvas");
            go.transform.SetParent(transform);
            go.layer = 5;
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 20;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("Panel");
            _panel.transform.SetParent(go.transform, false);
            _panel.layer = 5;
            var rt = _panel.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 100);
            rt.pivot = new Vector2(0.5f, 0f);
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

            var layout = _panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;

            _moveButton = CreateButton(_panel.transform, "Move", () => GameManager.Instance.StartMove());
            _attackButton = CreateButton(_panel.transform, "Attack", () => GameManager.Instance.StartAttack());
        }

        private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            go.layer = 5;
            var btn = go.AddComponent<Button>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.25f);
            
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            textGo.layer = 5;
            var txt = textGo.AddComponent<TextMeshProUGUI>();
            txt.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            txt.text = label;
            txt.fontSize = 20;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            
            btn.onClick.AddListener(action);
            return btn;
        }

        public void Show(Monster monster)
        {
            _targetMonster = monster;
            _panel.SetActive(true);
            UpdatePosition();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            _targetMonster = null;
        }

        private void Update()
        {
            if (_targetMonster != null) UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (_targetMonster == null) return;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(_targetMonster.transform.position + Vector3.up * 2f);
            _panel.transform.position = screenPos;
        }
    }
}