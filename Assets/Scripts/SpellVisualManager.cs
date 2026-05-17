using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace ForgottenDomain
{
    public class SpellVisualManager : MonoBehaviour
    {
        public static SpellVisualManager Instance { get; private set; }

        [Header("UI References")]
        private Canvas _canvas;
        private GameObject _popPanel;
        private Image _cardImage;
        private TextMeshProUGUI _cardNameText;
        private RectTransform _popRT;

        [Header("VFX Settings")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private GameObject _impactPrefab;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            BuildUI();
        }

        private void BuildUI()
        {
            var go = new GameObject("SpellVisualCanvas");
            go.transform.SetParent(transform);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100; // Above everything else
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();

            _popPanel = new GameObject("MagicCardPop");
            _popPanel.transform.SetParent(_canvas.transform, false);
            _popRT = _popPanel.AddComponent<RectTransform>();
            _popRT.anchorMin = new Vector2(0, 0.5f);
            _popRT.anchorMax = new Vector2(0, 0.5f);
            _popRT.pivot = new Vector2(0, 0.5f);
            _popRT.anchoredPosition = new Vector2(-400, 0); // Start off-screen
            _popRT.sizeDelta = new Vector2(300, 450);

            var bg = _popPanel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            
            var frame = new GameObject("Frame");
            frame.transform.SetParent(_popPanel.transform, false);
            var frt = frame.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.sizeDelta = new Vector2(-10, -10);
            frame.AddComponent<Image>().color = new Color(1f, 0.8f, 0.2f, 0.5f);

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(_popPanel.transform, false);
            var nrt = nameGo.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0.8f); nrt.anchorMax = new Vector2(1, 0.95f);
            nrt.sizeDelta = Vector2.zero;
            _cardNameText = nameGo.AddComponent<TextMeshProUGUI>();
            _cardNameText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            _cardNameText.alignment = TextAlignmentOptions.Center;
            _cardNameText.fontSize = 24;
            _cardNameText.fontStyle = FontStyles.Bold;

            _popPanel.SetActive(false);
        }

        public void PlaySpellSequence(CardData card, Vector3 targetWorldPos)
        {
            StartCoroutine(SpellSequenceRoutine(card, targetWorldPos));
        }

        private IEnumerator SpellSequenceRoutine(CardData card, Vector3 targetWorldPos)
        {
            // 1. Pop Card
            _cardNameText.text = card.cardName;
            _popPanel.SetActive(true);
            
            // Slide in
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * 3f;
                _popRT.anchoredPosition = Vector2.Lerp(new Vector2(-400, 0), new Vector2(50, 0), t);
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            // 2. Shoot Projectile
            // Get screen position of the card pop center
            Vector2 screenPos = _popRT.position;
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            Vector3 spawnPos = ray.GetPoint(5f); // Spawn 5 units from camera

            GameObject proj = CreateProjectile(card);
            proj.transform.position = spawnPos;

            float projTime = 0;
            float duration = 0.8f;
            Vector3 startPos = spawnPos;
            
            while (projTime < 1)
            {
                projTime += Time.deltaTime / duration;
                proj.transform.position = Vector3.Lerp(startPos, targetWorldPos, projTime);
                proj.transform.LookAt(targetWorldPos);
                yield return null;
            }

            Destroy(proj);

            // 3. Impact
            CreateImpact(card, targetWorldPos);

            yield return new WaitForSeconds(1f);

            // 4. Slide out
            t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * 3f;
                _popRT.anchoredPosition = Vector2.Lerp(new Vector2(50, 0), new Vector2(-400, 0), t);
                yield return null;
            }

            _popPanel.SetActive(false);
        }

        private GameObject CreateProjectile(CardData card)
        {
            GameObject go = new GameObject("Projectile");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startSize = 1.0f;
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = ps.emission;
            emission.rateOverTime = 40;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            // Add Trails
            var trails = ps.trails;
            trails.enabled = true;
            trails.ratio = 1.0f;
            trails.lifetime = 0.2f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, 0f);
            trails.colorOverTrail = new ParticleSystem.MinMaxGradient(GetCardColor(card, 2f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            // Use Particle Unlit shader for better blending
            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);   // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            
            Texture2D tex = Resources.Load<Texture2D>("VFX/FireProjectile");
            mat.mainTexture = tex;
            
            // Apply HDR color for glow
            mat.SetColor("_BaseColor", GetCardColor(card, 3f)); 
            renderer.material = mat;
            
            renderer.trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            renderer.trailMaterial.SetFloat("_Surface", 1);
            renderer.trailMaterial.SetColor("_BaseColor", GetCardColor(card, 2f));

            // Add a point light for environment glow
            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(go.transform);
            light.color = GetCardColor(card, 1f);
            light.range = 6f;
            light.intensity = 15f; // URP lights need high intensity

            return go;
        }

        private void CreateImpact(CardData card, Vector3 pos)
        {
            GameObject go = new GameObject("Impact");
            go.transform.position = pos + Vector3.up * 0.5f;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startSize = 2.0f;
            main.startColor = Color.white;
            main.stopAction = ParticleSystemStopAction.Destroy;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 40) });

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            mat.SetFloat("_Surface", 1);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive for impact
            
            Texture2D tex = Resources.Load<Texture2D>("VFX/EnergyImpact");
            mat.mainTexture = tex;
            mat.SetColor("_BaseColor", GetCardColor(card, 5f));
            renderer.material = mat;

            ps.Play();
            
            // Add Sparkles
            GameObject sparkles = new GameObject("Sparkles");
            sparkles.transform.position = pos + Vector3.up * 0.5f;
            var sps = sparkles.AddComponent<ParticleSystem>();
            var smain = sps.main;
            smain.startSize = 0.1f;
            smain.startSpeed = 5f;
            smain.startColor = Color.white;
            smain.duration = 0.5f;
            smain.loop = false;
            smain.stopAction = ParticleSystemStopAction.Destroy;
            
            var semission = sps.emission;
            semission.rateOverTime = 0;
            semission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 20) });

            var srenderer = sparkles.GetComponent<ParticleSystemRenderer>();
            srenderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            srenderer.material.SetColor("_BaseColor", Color.white * 5f); // Bright white glow
            
            sps.Play();
            
            // Temporary impact light
            var light = new GameObject("ImpactLight").AddComponent<Light>();
            light.transform.position = pos + Vector3.up * 1f;
            light.color = GetCardColor(card, 1f);
            light.range = 10f;
            light.intensity = 40f;
            Destroy(light.gameObject, 0.4f);
        }

        private Color GetCardColor(CardData card, float intensity = 1f)
        {
            Color baseColor = Color.red;
            if (card.cardName.Contains("Zero")) baseColor = Color.cyan;
            else if (card.cardName.Contains("Gamma")) baseColor = Color.green;
            else if (card.cardName.Contains("Dark")) baseColor = new Color(0.7f, 0, 1f);
            else if (card.cardName.Contains("Event")) baseColor = new Color(0.1f, 0.1f, 0.2f);
            
            return baseColor * intensity;
        }
    }
}
