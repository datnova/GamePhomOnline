using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static GameExtensions.ConstantData;

namespace GameExtensions
{
    internal static class PhomTool
    {
        //
        //
        //// Work with phom

        // re-range to optimize phom
        public static Card[][] OptimizePhom(Card[] deck)
        {
            // líst contain phom and trash
            List<List<Card>> phom = new List<List<Card>>();
            var trash = deck.ToList();

            // create value table
            Dictionary<int, List<Card>> valueTable = new Dictionary<int, List<Card>>();

            // add card to value table
            foreach (var card in trash)
            {
                if (!valueTable.ContainsKey(card.value))
                    valueTable.Add(card.value, new List<Card>());

                valueTable[card.value].Add(card);
            }

            // loop thought value table
            for (int value = CardPip.Length - 1; value >= 0; value--)
            {
                GetPhomNgang(valueTable, phom, trash, value);
                if (value > 1)
                {
                    GetPhomDoc(valueTable, phom, trash, value);
                }
            }

            // add remain card in trash to phom if possible
            TryAddTrash(phom, trash);

            phom.Add(trash);
            return phom.Select(x => x.ToArray()).ToArray();
        }

        // check is it phom
        public static bool CheckPhom(Card[] cards)
        {
            return (CheckPhomDoc(cards) || CheckPhomNgang(cards));
        }

        // check valid phom doc
        public static bool CheckPhomDoc(Card[] cards)
        {
            if (cards.Length <= 2) return false;

            List<int> valueTable = new List<int>();
            string tempSuit = null;

            // check same suit and add to valueTable
            foreach (var card in cards)
            {
                if (tempSuit is null)
                {
                    tempSuit = card.suit;
                }
                else if (tempSuit != card.suit)
                {
                    return false;
                }
                valueTable .Add(card.value);
            }

            valueTable .Sort((a, b) => b.CompareTo(a)); // descending sort
            // check is it follow order
            for (int i = 0; i < valueTable .Count - 1; i++)
            {
                if (valueTable [i] - valueTable [i + 1] != 1)
                    return false;
            }
            return true;
        }

        // check valid phom ngang (maybe)
        public static bool CheckPhomNgang(Card[] cards)
        {
            if (cards.Length <= 2) return false;
            return cards.All(a => a.pip == cards[0].pip);
        }

        // Get phom doc
        private static void GetPhomDoc(Dictionary<int, List<Card>> valueTable, List<List<Card>> phom, List<Card> trash, int value)
        {
            // check is there can be phom doc
            if (!valueTable.ContainsKey(value) ||
                !valueTable.ContainsKey(value - 1) ||
                !valueTable.ContainsKey(value - 2)) return;

            // get duplicate suit in top 3 from _valueTable start from _value
            var suits = valueTable[value]
                         .Select(x => x.suit)
                         .Intersect(valueTable[value - 1].Select(x => x.suit))
                         .Intersect(valueTable[value - 2].Select(x => x.suit));

            // return if no phom
            if (suits.Count() == 0) return;

            // get phom doc from each suit
            foreach (var suit in suits)
            {
                // add phom
                phom.Add(trash.Where(x => (x.suit == suit) && (value - x.value) <= 2).ToList());

                // remove from value table
                for (int i = 0; i < 3; i++)
                {
                    valueTable[value - i].RemoveAll(x => x.suit == suit);
                    if (valueTable[value - i].Count == 0) valueTable.Remove(value - i);
                }

                // remove from trash
                trash.RemoveAll(x => (x.suit == suit) && ((value - x.value) <= 2));
            }
        }

        // Get phom ngang
        private static void GetPhomNgang(Dictionary<int, List<Card>> valueTable, List<List<Card>> phom, List<Card> trash, int value)
        {
            // check is there phom ngang
            if (!valueTable.ContainsKey(value) ||
                valueTable[value].Count() <= 2) return;

            // get dup suit
            string[] _dupSuits = null;
            if (value > 1 &&
                valueTable.ContainsKey(value) && 
                valueTable.ContainsKey(value - 1) &&
                valueTable.ContainsKey(value - 2))
            {
                // get duplicate suit in top 3 from _valueTable start from _value
                _dupSuits = valueTable[value]
                            .Select(x => x.suit)
                            .Intersect(valueTable[value - 1].Select(x => x.suit))
                            .Intersect(valueTable[value - 2].Select(x => x.suit)).ToArray();
            }

            // if there is no phom doc
            if (_dupSuits is null || _dupSuits.Length == 0)
            {
                phom.Add(valueTable[value]);
                valueTable.Remove(value);
                trash.RemoveAll(x => x.value == value);
            }
            // if there is 1 phom doc
            else if (_dupSuits.Length == 1)
            {
                // if there is 3 card that create phom ngang
                if (valueTable[value].Count() == 3)
                {
                    phom.Add(valueTable[value]);
                    valueTable.Remove(value);
                    trash.RemoveAll(x => x.value == value);
                }
                // if there is 4 card that create phom ngang
                else
                {
                    // add to phom
                    phom.Add(valueTable[value]);
                    phom[phom.Count() - 1].RemoveAll(x => _dupSuits.Contains(x.suit));

                    // remove from table
                    valueTable[value].RemoveAll(x => phom[phom.Count() - 1].Contains(x));
                    if (valueTable[value].Count() == 0) valueTable.Remove(value);

                    // remove from trash
                    trash.RemoveAll(x => phom[phom.Count() - 1].Contains(x));
                }
            }
        }

        // try add trash to phom
        private static void TryAddTrash(List<List<Card>> phom, List<Card> trash)
        {
            for (int i = trash.Count - 1; i >= 0; i--)
            {
                foreach (var _tempPhom in phom)
                {
                    _tempPhom.Add(trash[i]);
                    if (!CheckPhom(_tempPhom.ToArray()))
                    {
                        _tempPhom.Remove(trash[i]);
                    }
                    else
                    {
                        trash.RemoveAt(i);
                    }
                }
            }
        }
    }

}
