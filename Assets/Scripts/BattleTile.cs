using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace ForgottenDomain
{
    public class BattleTile : MonoBehaviour, IPointerClickHandler
    {
        public Vector2Int coordinates;
        public UnityEvent<BattleTile> OnTileClicked;

        [SerializeField] private GameObject placementHighlight;
        [SerializeField] private GameObject moveRangeHighlight;

        [SerializeField] private GameObject trapHighlight;
        private GameObject _radiusHighlight;
        private TMPro.TextMeshPro _trapNameText;

        public Monster OccupyingMonster { get; set; }
        public CardData SetTrap { get; set; }
        public Team TrapOwner { get; set; }
        public bool IsOccupied => OccupyingMonster != null;
        public bool IsWalkable => !IsOccupied;

        private void Awake()
        {
            if (GetComponent<Collider>() == null)
                gameObject.AddComponent<BoxCollider>().size = new Vector3(1f, 1f, 0.01f);
        }

        public void OnPointerClick(PointerEventData eventData) => OnTileClicked?.Invoke(this);

        public void ShowPlacement() => ToggleHighlight(ref placementHighlight, "PlacementQuad", new Color(0.3f, 1f, 0.4f, 0.4f), 0.022f, true);
        public void HidePlacement() => ToggleHighlight(ref placementHighlight, null, default, 0, false);
        public void ShowMoveRange() => ToggleHighlight(ref moveRangeHighlight, "MoveQuad", new Color(0.3f, 0.5f, 1f, 0.35f), 0.020f, true);
        public void HideMoveRange() => ToggleHighlight(ref moveRangeHighlight, null, default, 0, false);
        
        public void ShowTrapMarker(string name) 
        {
            // Subtle fine red glow effect
            ToggleHighlight(ref trapHighlight, "TrapIndicator", new Color(1f, 0.1f, 0.1f, 0.15f), 0.024f, true, true);
            
            // Name indicator - Compact and professional
            if (_trapNameText == null)
            {
                var textGo = new GameObject("TrapName");
                textGo.transform.SetParent(transform);
                textGo.transform.localPosition = new Vector3(0, 0.1f, 0); // Very close to the tile
                textGo.transform.localRotation = Quaternion.Euler(90, 0, 0);
                _trapNameText = textGo.AddComponent<TMPro.TextMeshPro>();
                _trapNameText.font = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                _trapNameText.fontSize = 2f;
                _trapNameText.alignment = TMPro.TextAlignmentOptions.Center;
                _trapNameText.color = new Color(1f, 0.3f, 0.3f, 0.7f);
                _trapNameText.outlineWidth = 0.05f;
            }
            _trapNameText.text = name.ToUpper();
            _trapNameText.gameObject.SetActive(true);
        }

        public void HideTrapMarker() 
        {
            if (trapHighlight != null) trapHighlight.SetActive(false);
            if (_trapNameText != null) _trapNameText.gameObject.SetActive(false);
        }

        public void ShowTrapRadius(Color color) 
        {
            // Very soft glow for the radius (very low alpha)
            Color subtleGlow = new Color(color.r, color.g, color.b, 0.05f);
            ToggleHighlight(ref _radiusHighlight, "RadiusGlow", subtleGlow, 0.015f, true);
        }

        public void HideTrapRadius() 
        {
            if (_radiusHighlight != null) _radiusHighlight.SetActive(false);
        }

        private void ToggleHighlight(ref GameObject hl, string name, Color color, float yOff, bool show, bool isIndicator = false)
        {
            if (!show) { if (hl) hl.SetActive(false); return; }
            if (hl == null) hl = CreateHighlightQuad(name, color, yOff, isIndicator);
            hl.SetActive(true);
            
            // Update color if already exists
            var mr = hl.GetComponent<MeshRenderer>();
            if (mr != null) mr.material.SetColor("_BaseColor", color);
        }

        private GameObject CreateHighlightQuad(string name, Color color, float yOff, bool isIndicator = false)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(transform);
            quad.transform.localPosition = new Vector3(0, yOff, 0);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            
            // Slightly smaller than tile for a clean look
            quad.transform.localScale = new Vector3(1.9f, 1.9f, 1f); 

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", color);
            
            // Set rendering mode to transparent
            mat.SetFloat("_Surface", 1); 
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Destroy(quad.GetComponent<Collider>());
            return quad;
        }

        public int DistanceTo(BattleTile other) =>
            other == null ? int.MaxValue : Mathf.Max(Mathf.Abs(coordinates.x - other.coordinates.x), Mathf.Abs(coordinates.y - other.coordinates.y));
}
}
