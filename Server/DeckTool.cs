using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameExtensions;
using static GameExtensions.ConstantData;

namespace Server
{
    internal static class DeckTool
    {
        // devide card for all players last array is a remain cards
        public static Card[][] DevideCard(int _hostID, bool _shuffle = false)
        {
            // create cards deck and shuffle
            var _deck = CreateCardDeck();
            ShuffleCarDeck(_deck);

            var _res = new Card[5][];

            // divide cards
            for (int i = 0; i < 4; i++)
            {
                if (i == _hostID)
                {
                    _res[i] = _deck.Take(10).ToArray();
                    _deck = _deck.Skip(10).Take(_deck.Length - 10).ToArray();
                }
                else
                {
                    _res[i] = _deck.Take(9).ToArray();
                    _deck = _deck.Skip(9).Take(_deck.Length - 9).ToArray();
                }
            }

            // add remain
            _res[4] = _deck;

            return _res;
        }

        // scoring cards
        public static int Scoring(Card[] _cards)
        {
            Card[] _trash = PhomTool.OptimizePhom(_cards).Last();
            return _trash.Select(x => x.value).ToArray().Sum();
        }

        // create array of card
        public static Card[] CreateCardDeck()
        {
            // for more detail read this url:
            // https://en.wikipedia.org/wiki/Standard_52-card_deck#:~:text=clubs%20(%E2%99%A3).-,Nomenclature,or%20%22Ace%20of%20Spades%22.

            Card[] _cards = new Card[52];

            int i = 0;
            foreach (var _pip in _CardPip.Select((value, index) => new { index, value }))
            {
                foreach (var _suit in _CardSuit)
                {
                    _cards[i] = new Card(_pip.value, _suit, _pip.index + 1);
                    i++;
                }
            }

            return _cards;
        }

        // Shuffle cards Deck
        private static void ShuffleCarDeck(Card[] _CardDeck)
        {
            Random rng = new Random();
            int n = _CardDeck.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                var value = _CardDeck[k];
                _CardDeck[k] = _CardDeck[n];
                _CardDeck[n] = value;
            }
        }
    }
}
