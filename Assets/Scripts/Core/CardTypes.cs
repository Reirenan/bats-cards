using System;

namespace BatsSolitaire.Core
{
    public enum CardSuit { Clubs, Diamonds, Hearts, Spades }
    public enum CardRank { Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }
    public enum PileKind { Stock, Waste, Tableau, Foundation }
    public enum GameMode { Random, Winnable, Daily }

    [Serializable]
    public struct CardId : IEquatable<CardId>
    {
        public CardSuit Suit;
        public CardRank Rank;

        public CardId(CardSuit suit, CardRank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        public bool IsRed => Suit == CardSuit.Diamonds || Suit == CardSuit.Hearts;
        public int Value => (int)Rank;
        public string Key => $"{Suit}_{Rank}";
        public string ShortName => $"{RankSymbol}{SuitSymbol}";
        public string RankSymbol => Rank switch
        {
            CardRank.Ace => "A",
            CardRank.Jack => "J",
            CardRank.Queen => "Q",
            CardRank.King => "K",
            _ => ((int)Rank).ToString()
        };
        public string SuitSymbol => Suit switch
        {
            CardSuit.Clubs => "♣",
            CardSuit.Diamonds => "♦",
            CardSuit.Hearts => "♥",
            _ => "♠"
        };

        public bool Equals(CardId other) => Suit == other.Suit && Rank == other.Rank;
        public override bool Equals(object obj) => obj is CardId other && Equals(other);
        public override int GetHashCode() => ((int)Suit * 397) ^ (int)Rank;
        public override string ToString() => ShortName;
    }

    [Serializable]
    public sealed class CardState
    {
        public CardId Card;
        public bool FaceUp;

        public CardState(CardId card, bool faceUp)
        {
            Card = card;
            FaceUp = faceUp;
        }

        public CardState Clone() => new(Card, FaceUp);
    }

    [Serializable]
    public struct PileRef
    {
        public PileKind Kind;
        public int Index;

        public PileRef(PileKind kind, int index = 0)
        {
            Kind = kind;
            Index = index;
        }
    }

    [Serializable]
    public sealed class Move
    {
        public PileRef From;
        public PileRef To;
        public int CardIndex;

        public Move(PileRef from, PileRef to, int cardIndex = -1)
        {
            From = from;
            To = to;
            CardIndex = cardIndex;
        }
    }
}
