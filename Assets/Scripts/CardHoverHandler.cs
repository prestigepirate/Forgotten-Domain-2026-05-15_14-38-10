using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForgottenDomain
{
    public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private GameObject _glowObject;
        private Vector3 _originalScale;
        private float _hoverScale = 1.05f;

        public void Setup(GameObject glow)
        {
            _glowObject = glow;
            if (_glowObject != null) _glowObject.SetActive(false);
            _originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_glowObject != null) _glowObject.SetActive(true);
            transform.localScale = _originalScale * _hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_glowObject != null) _glowObject.SetActive(false);
            transform.localScale = _originalScale;
        }
    }
}
