using System.Collections.Generic;
using UnityEngine;

namespace ForgottenDomain
{
    [System.Serializable]
    public class Deck
    {
        [SerializeField] private List<CardData> cards = new List<CardData>();
        private List<CardData> _drawPile = new List<CardData>();

        public int Remaining => _drawPile.Count;

        public void Initialize(List<CardData> cardList) { cards = new List<CardData>(cardList); ShuffleAll(); }

        public void ShuffleAll()
        {
            _drawPile = new List<CardData>(cards);
            for (int i = _drawPile.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (_drawPile[i], _drawPile[j]) = (_drawPile[j], _drawPile[i]); }
        }

        public CardData Draw()
        {
            if (_drawPile.Count == 0) return null;
            var c = _drawPile[0]; _drawPile.RemoveAt(0); return c;
        }
    }
}
