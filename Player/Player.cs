using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Player
{
    [Serializable]
    internal class Card
    {
        public Card(string pip, string suit, int value)
        {
            this.pip = pip;
            this.suit = suit;
            this.value = value;
        }

        public override string ToString()
        {
            return this.pip + "_" + this.suit;
        }

        public string pip { get; set; }
        public string suit { get; set; }
        public int value { get; set; }
    }

    [Serializable]
    internal class PlayerInfo
    {
        public PlayerInfo(int id, string name, int point)
        {
            this.id = id;
            this.name = name;
            this.point = point;
        }

        public int id { get; set; }
        public string name { get; set; }
        public int point { get; set; }
    }

    [Serializable]
    internal class RequestForm
    {
        public int stateID { get; set; } = 0;
        public int playerID { get; set; } = -1;
        public string playerName { get; set; } = String.Empty;
        public Card sendCard { get; set; } = null;
        public Card[] phom { get; set; } = null;
        public Card[] trash { get; set; } = null;


    }

    [Serializable]
    internal class ResponseForm
    {
        public string status { get; set; } = "success";
        public int stateID { get; set; } = -1;
        public int currentID { get; set; } = -1;
        public int currentRound { get; set; } = -1;
        public int hostID { get; set; } = -1;
        public int numberPlayer { get; set; } = -1;
        public int recceiveID { get; set; } = -1;
        public Card cardHolder { get; set; } = null;
        public Card cardPull { get; set; } = null;
        public PlayerInfo[] playerInfo { get; set; } = null;
        public string messages { get; set; } = String.Empty;
    }

    internal class PhomTool
    {
        //
        //
        //// Work with phom

        // re-range to optimize phom => the last in array is an array contain trash
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
            for (int _value = Player._CardPip.Length - 1; _value > 2; _value--)
            {
                GetPhomNgang(_valueTable, _phom, _trash, _value);
                GetPhomDoc(_valueTable, _phom, _trash, _value);
            }

            // add remain card in trash to phom if possible
            TryAddTrash(_phom, _trash);

            _phom.Add(_trash);
            return _phom.Select(x => x.ToArray()).ToArray();
        }

        // Get phom doc
        private static bool GetPhomDoc(Dictionary<int, List<Card>> _valueTable, List<List<Card>> _phom, List<Card> _trash, int _value)
        {
            // check is there can be phom doc
            if (!_valueTable.ContainsKey(_value) ||
                !_valueTable.ContainsKey(_value - 1) ||
                !_valueTable.ContainsKey(_value - 2)) return false;

            // get duplicate suit in top 3 from _valueTable start from _value
            var _suits = _valueTable[_value]
                         .Select(x => x.suit)
                         .Intersect(_valueTable[_value - 1].Select(x => x.suit))
                         .Intersect(_valueTable[_value - 2].Select(x => x.suit));

            // return if no phom
            if (_suits.Count() == 0) return false;

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

            return true;
        }

        // Get phom ngang
        private static bool GetPhomNgang(Dictionary<int, List<Card>> _valueTable, List<List<Card>> _phom, List<Card> _trash, int _value)
        {
            // check is there phom ngang
            if (!_valueTable.ContainsKey(_value) ||
                _valueTable[_value].Count() <= 2) return false;

            // get dup suit
            string[] _dupSuits = null;
            if (!_valueTable.ContainsKey(_value) ||
                !_valueTable.ContainsKey(_value - 1) ||
                !_valueTable.ContainsKey(_value - 2))
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
                return true;
            }

            return false;
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
    }

    internal class Player
    {
        //
        //
        /// player value

        // contain card on hand
        private Card[] _playerHand = new Card[10];

        //  _playersInfo: <playerID, playerName, gamePoint>
        public PlayerInfo _playersInfo = null;

        // card for pull
        public Card[] _cardHolder = new Card[4];

        // current data
        public int _stateID = 0;            // game state id
        public int _currentID = -1;         // turn for player's id
        public int _currentRound = -1;      // current number round
        public int _hostID = -1;            // current host id
        public int _numberPlayer = 0;       // number of player


        //
        //
        //// game status

        // Contain all game state info
        public static readonly string[] _gameState = new string[] {
            "Wait for player",  // Send the return of update                          (player after connected)
            "Set up game",      // Send cards to player's hands then set stateID to 2 (only host)
            "Play card",        // Send the return of update
            "Take card",        // Send card to current id
            "Reset game",       // Send cards to player's hands then set stateID to 2
        };

        // contain cards pip info
        public static readonly string[] _CardPip = new string[]
        {
            "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King"
        };

        // contain cards suit info
        public static readonly string[] _CardSuit = new string[]
        {
            "Club", "Diamond", "Heard", "Spade"
        };
    }
}
