using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace ForgottenDomain
{
    public class MainMenuManager : MonoBehaviour
    {
        private Canvas _canvas;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;

        private void Awake()
        {
            CreateMainMenu();
        }

        private void CreateMainMenu()
        {
            // Create Canvas
            GameObject canvasGO = new GameObject("MainMenuCanvas");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Dramatic dark background panel
            var bg = CreatePanel("Background", _canvas.transform, new Vector2(0,0), new Vector2(1,1), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(0,0));
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.02f, 0.02f, 0.03f, 1f);

            // Main Title - FORGOTTEN DOMAIN
            _titleText = CreateTitleText("FORGOTTEN DOMAIN", new Vector2(0, 180));

            // Subtitle - A SYNCINGSHIPS PROJECT
            _subtitleText = CreateSubtitleText("A SYNCINGSHIPS PROJECT", new Vector2(0, 80));

            // Buttons
            CreateButton("Play", new Vector2(0, -80), () => LoadGameScene());
            CreateButton("Deck Builder", new Vector2(0, -160));
            CreateButton("Collection", new Vector2(0, -240));
            CreateButton("Settings", new Vector2(0, -320));
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Image img = go.AddComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);
            return go;
        }

        private TextMeshProUGUI CreateTitleText(string text, Vector2 position)
        {
            GameObject go = new GameObject("Title");
            go.transform.SetParent(_canvas.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 120;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.9f, 0.95f, 1f);
            return tmp;
        }

        private TextMeshProUGUI CreateSubtitleText(string text, Vector2 position)
        {
            GameObject go = new GameObject("Subtitle");
            go.transform.SetParent(_canvas.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.6f, 0.65f, 0.7f);
            return tmp;
        }

        private void CreateButton(string buttonText, Vector2 position, UnityEngine.Events.UnityAction onClick = null)
        {
            GameObject btnGO = new GameObject(buttonText + "Button");
            btnGO.transform.SetParent(_canvas.transform, false);
            RectTransform rt = btnGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(500, 80);

            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.1f, 1f);

            TextMeshProUGUI tmp = btnGO.AddComponent<TextMeshProUGUI>();
            tmp.text = buttonText;
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0f, 0.9f, 0.85f);

            Button btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener(onClick ?? (() => Debug.Log(buttonText + " clicked")));
        }

        private void LoadGameScene()
        {
            SceneManager.LoadScene("Battlefield");
        }
    }
}