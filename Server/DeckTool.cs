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
        public static Card[][] DevideCard(int hostID, bool shuffle = false)
        {
            // create cards deck and shuffle
            var deck = CreateCardDeck();
            ShuffleCarDeck(deck);

            var result = new Card[5][];

            // divide cards
            for (int i = 0; i < 4; i++)
            {
                if (i == hostID)
                {
                    result[i] = deck.Take(10).ToArray();
                    deck = deck.Skip(10).Take(deck.Length - 10).ToArray();
                }
                else
                {
                    result[i] = deck.Take(9).ToArray();
                    deck = deck.Skip(9).Take(deck.Length - 9).ToArray();
                }
            }

            // add remain
            result[4] = deck;

            return result;
        }

        // scoring cards
        public static int Scoring(Card[] cards)
        {
            Card[] tempCards = PhomTool.OptimizePhom(cards).Last();
            return tempCards.Select(x => x.value).ToArray().Sum();
        }

        // create array of card
        public static Card[] CreateCardDeck()
        {
            // for more detail read this url:
            // https://en.wikipedia.org/wiki/Standard_52-card_deck#:~:text=clubs%20(%E2%99%A3).-,Nomenclature,or%20%22Ace%20of%20Spades%22.

            Card[] cards = new Card[52];

            int i = 0;
            foreach (var pip in cardPip.Select((value, index) => new { index, value }))
            {
                foreach (var suit in cardSuit)
                {
                    cards[i] = new Card(pip.value, suit, pip.index + 1);
                    i++;
                }
            }

            return cards;
        }

        // Shuffle cards Deck
        public static void ShuffleCarDeck(Card[] cards)
        {
            Random rng = new Random();
            int n = cards.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);

                // swap
                var value = cards[k];
                cards[k] = cards[n];
                cards[n] = value;
            }
        }
    }
}
