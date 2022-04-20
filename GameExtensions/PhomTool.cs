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
        public static Card[][] OptimizePhom(Card[] _deck)
        {
            // líst contain phom and trash
            List<List<Card>> _phom = new List<List<Card>>();
            var _trash = _deck.ToList();

            // create value table
            Dictionary<int, List<Card>> _valueTable = new Dictionary<int, List<Card>>();

            // add card to value table
            foreach (var _card in _trash)
            {
                if (!_valueTable.ContainsKey(_card.value))
                    _valueTable.Add(_card.value, new List<Card>());

                _valueTable[_card.value].Add(_card);
            }

            // loop thought value table
            for (int _value = _CardPip.Length - 1; _value >= 0; _value--)
            {
                GetPhomNgang(_valueTable, _phom, _trash, _value);
                if (_value > 1)
                {
                    GetPhomDoc(_valueTable, _phom, _trash, _value);
                }
            }

            // add remain card in trash to phom if possible
            TryAddTrash(_phom, _trash);

            _phom.Add(_trash);
            return _phom.Select(x => x.ToArray()).ToArray();
        }

        // check is it phom
        public static bool CheckPhom(Card[] _cards)
        {
            return (CheckPhomDoc(_cards) || CheckPhomNgang(_cards));
        }

        // check valid phom doc (maybe)
        public static bool CheckPhomDoc(Card[] _cards)
        {
            if (_cards.Length <= 1) return true;

            List<int> _temp = new List<int>();
            string _tempSuit = null;

            // check same suit and add to temp líst
            foreach (var _card in _cards)
            {
                if (_tempSuit is null)
                {
                    _tempSuit = _card.suit;
                }
                else if (_tempSuit != _card.suit)
                {
                    return false;
                }
                _temp.Add(_card.value);
            }

            _temp.Sort((a, b) => b.CompareTo(a)); // descending sort
            // check is it follow order
            for (int i = 0; i < _temp.Count - 1; i++)
            {
                if (_temp[i] - _temp[i + 1] != 1)
                    return false;
            }
            return true;
        }

        // check valid phom ngang (maybe)
        public static bool CheckPhomNgang(Card[] _cards)
        {
            if (_cards.Length <= 1) return true;
            return _cards.All(a => a.pip == _cards[0].pip);
        }

        // Get phom doc
        private static void GetPhomDoc(Dictionary<int, List<Card>> _valueTable, List<List<Card>> _phom, List<Card> _trash, int _value)
        {
            // check is there can be phom doc
            if (!_valueTable.ContainsKey(_value) ||
                !_valueTable.ContainsKey(_value - 1) ||
                !_valueTable.ContainsKey(_value - 2)) return;

            // get duplicate suit in top 3 from _valueTable start from _value
            var _suits = _valueTable[_value]
                         .Select(x => x.suit)
                         .Intersect(_valueTable[_value - 1].Select(x => x.suit))
                         .Intersect(_valueTable[_value - 2].Select(x => x.suit));

            // return if no phom
            if (_suits.Count() == 0) return;

            // get phom doc from each suit
            foreach (var _suit in _suits)
            {
                // add phom
                _phom.Add(_trash.Where(x => (x.suit == _suit) && (_value - x.value) <= 2).ToList());

                // remove from value table
                for (int i = 0; i < 3; i++)
                {
                    _valueTable[_value - i].RemoveAll(x => x.suit == _suit);
                    if (_valueTable[_value - i].Count == 0) _valueTable.Remove(_value - i);
                }

                // remove from trash
                _trash.RemoveAll(x => (x.suit == _suit) && ((_value - x.value) <= 2));
            }
        }

        // Get phom ngang
        private static void GetPhomNgang(Dictionary<int, List<Card>> _valueTable, List<List<Card>> _phom, List<Card> _trash, int _value)
        {
            // check is there phom ngang
            if (!_valueTable.ContainsKey(_value) ||
                _valueTable[_value].Count() <= 2) return;

            // get dup suit
            string[] _dupSuits = null;
            if (_value > 1 &&
                _valueTable.ContainsKey(_value) && 
                _valueTable.ContainsKey(_value - 1) &&
                _valueTable.ContainsKey(_value - 2))
            {
                // get duplicate suit in top 3 from _valueTable start from _value
                _dupSuits = _valueTable[_value]
                            .Select(x => x.suit)
                            .Intersect(_valueTable[_value - 1].Select(x => x.suit))
                            .Intersect(_valueTable[_value - 2].Select(x => x.suit)).ToArray();
            }

            // if there is no phom doc
            if (_dupSuits is null || _dupSuits.Length == 0)
            {
                _phom.Add(_valueTable[_value]);
                _valueTable.Remove(_value);
                _trash.RemoveAll(x => x.value == _value);
            }
            // if there is 1 phom doc
            else if (_dupSuits.Length == 1)
            {
                // if there is 3 card that create phom ngang
                if (_valueTable[_value].Count() == 3)
                {
                    _phom.Add(_valueTable[_value]);
                    _valueTable.Remove(_value);
                    _trash.RemoveAll(x => x.value == _value);
                }
                // if there is 4 card that create phom ngang
                else
                {
                    // add to phom
                    _phom.Add(_valueTable[_value]);
                    _phom[_phom.Count() - 1].RemoveAll(x => _dupSuits.Contains(x.suit));

                    // remove from table
                    _valueTable[_value].RemoveAll(x => _phom[_phom.Count() - 1].Contains(x));
                    if (_valueTable[_value].Count() == 0) _valueTable.Remove(_value);

                    // remove from trash
                    _trash.RemoveAll(x => _phom[_phom.Count() - 1].Contains(x));
                }
            }
        }

        // try add trash to phom
        private static void TryAddTrash(List<List<Card>> _phom, List<Card> _trash)
        {
            for (int i = _trash.Count - 1; i >= 0; i--)
            {
                foreach (var _tempPhom in _phom)
                {
                    _tempPhom.Add(_trash[i]);
                    if (!CheckPhom(_tempPhom.ToArray()))
                    {
                        _tempPhom.Remove(_trash[i]);
                    }
                    else
                    {
                        _trash.RemoveAt(i);
                    }
                }
            }
        }
    }

}
