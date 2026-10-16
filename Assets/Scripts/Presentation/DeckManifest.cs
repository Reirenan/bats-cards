using System;
using System.Collections.Generic;
using BatsSolitaire.Core;
using UnityEngine;

namespace BatsSolitaire.Presentation
{
    [CreateAssetMenu(menuName = "Bats Cards/Deck Manifest")]
    public sealed class DeckManifest : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public CardSuit Suit;
            public CardRank Rank;
            public Sprite Sprite;
            public string CommonNamePt;
            public string CommonNameEn;
            public string ScientificName;
        }

        public Sprite Back;
        public List<Entry> Cards = new();

        public Entry Find(CardId card) => Cards.Find(entry => entry.Suit == card.Suit && entry.Rank == card.Rank);
    }
}
