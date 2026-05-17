using UnityEngine;
using TMPro;

namespace ForgottenDomain
{
    public class DamageText : MonoBehaviour
    {
        private TextMeshPro _text;
        private float _lifetime = 1.5f;
        private Vector3 _velocity;

        public void Setup(string text, Color color)
        {
            _text = gameObject.AddComponent<TextMeshPro>();
            _text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            
            _text.text = text;
            _text.color = color;
            _text.fontSize = 6;
            _text.alignment = TextAlignmentOptions.Center;
            _text.outlineWidth = 0.2f;
            _text.outlineColor = Color.black;

            _velocity = new Vector3(Random.Range(-0.5f, 0.5f), 2f, Random.Range(-0.5f, 0.5f));
            Destroy(gameObject, _lifetime);
        }

        private void Update()
        {
            transform.position += _velocity * Time.deltaTime;
            _velocity.y -= 2f * Time.deltaTime; // Gravity-ish
            
            // Fade out
            var c = _text.color;
            c.a -= Time.deltaTime / _lifetime;
            _text.color = c;

            // Face camera
            if (Camera.main != null)
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }

        public static void Create(Vector3 position, string text, Color color)
        {
            var go = new GameObject("DamageText");
            go.transform.position = position + Vector3.up * 2f;
            var dt = go.AddComponent<DamageText>();
            dt.Setup(text, color);
        }
    }
}