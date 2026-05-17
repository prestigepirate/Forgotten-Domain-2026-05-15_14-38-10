using UnityEngine;
using TMPro;

namespace ForgottenDomain
{
    public class MonsterIndicator : MonoBehaviour
    {
        private TextMeshPro _textMesh;
        private Monster _owner;
        private Transform _camTransform;

        private int _lastHp, _lastAtk, _lastDef, _lastLvl;

        public void Initialize(Monster owner)
        {
            _owner = owner;
            _camTransform = Camera.main.transform;

            GameObject textGo = new GameObject("IndicatorText");
            textGo.transform.SetParent(transform);
            textGo.transform.localPosition = Vector3.zero;

            _textMesh = textGo.AddComponent<TextMeshPro>();
            _textMesh.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (_textMesh.font == null) Debug.LogWarning("[MonsterIndicator] Could not find default TMP Font Asset!");
            
            _textMesh.fontSize = 4;
            _textMesh.alignment = TextAlignmentOptions.Center;
            
            if (_owner.OwnerTeam == Team.Opponent)
                _textMesh.color = new Color(1f, 0.2f, 0.2f); // Enemy Red
            else
                _textMesh.color = Color.white; // Player White
                
            _textMesh.outlineWidth = 0.2f;
            _textMesh.outlineColor = Color.black;

            ForceUpdate();
        }

        private void LateUpdate()
        {
            if (_camTransform == null || _owner == null) return;
            
            // Billboard towards camera
            transform.rotation = Quaternion.LookRotation(transform.position - _camTransform.position);
            
            if (_owner.CurrentHealth != _lastHp || _owner.Attack != _lastAtk || _owner.Defense != _lastDef || _owner.Level != _lastLvl)
            {
                UpdateText();
            }
        }

        private void ForceUpdate()
        {
            UpdateText();
        }

        private void UpdateText()
        {
            if (_owner == null || _textMesh == null) return;

            _lastHp = _owner.CurrentHealth;
            _lastAtk = _owner.Attack;
            _lastDef = _owner.Defense;
            _lastLvl = _owner.Level;

            string healthColor = _lastHp < _owner.MaxHealth ? "<color=red>" : "<color=white>";
            _textMesh.text = $"{_owner.DisplayName}\n" +
                             $"Lv.{_lastLvl}\n" +
                             $"<size=80%>{healthColor}HP: {_lastHp}</color>\n" +
                             $"ATK:{_lastAtk}  DEF:{_lastDef}</size>";
        }
    }
}
