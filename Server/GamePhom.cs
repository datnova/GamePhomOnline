using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class Card
    {
        public Card(string pip, string suit, int value)
        {
            this.pip = pip;
            this.suit= suit;
            this.value = value;
        }

        public string pip { get; set; }
        public string suit { get; set; }
        public int value { get; set; }
    }

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

    internal class RequestForm
    {
        public int stateID { get; set; } = 0;
        public int playerID { get; set; } = -1;
        public string playerName { get; set; } = String.Empty;
        public Card sendCard { get; set; } = null;
        public Card[] phom { get; set; } = null;
        public Card[] trash { get; set; } = null;
    }

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

    internal class GamePhom
    {
        // max 4 players (min 2 players), each player contain array of max 10 cards (min 9 cards)
        private Card[][] _playersHand = new Card[4][];

        //  _playersInfo: <playerID, playerName, gamePoint>
        public PlayerInfo[] _playersInfo = new PlayerInfo[4];

        // contain cards deck for draw after run set up game
        public Card[] _drawDeck = null;

        // card for pull
        public Card _cardHolder = null;

        // current data
        public int _stateID = 0;            // game state id
        public int _currentID = -1;         // turn for player's id
        public int _currentRound = -1;      // current number round
        public int _hostID = -1;            // current host id
        public int _numberPlayer = 0;       // number of player

        //
        //
        //// Player set up:

        // return player id if success or -1 if fail
        public int AddPlayer(string player_name)
        {
            if (_hostID < 0) _hostID = 0;
            _numberPlayer++;

            // assign player to empty locate
            int _tempID = 0;
            while (_tempID < 4)
            {
                if (_playersInfo[_tempID] is null)
                {
                    _playersInfo[_tempID] = new PlayerInfo(_tempID, player_name, 0);
                    return _tempID; // return add success
                }
                _tempID++;
            }
            return -1; // return add fail
        }

        // return bool success or not
        public bool RemovePlayer(int _playerID)
        {
            if (_playersInfo[_playerID] is null) return false;

            _playersInfo[_playerID] = null;
            _numberPlayer--;

            // shift host id to next player
            // shift current player to next player
            if (_playerID == _hostID || _playerID == _currentID)
            {
                for (int i = (_playerID + 1) % 4; i != _playerID; i = (i + 1) % 4)
                {
                    if (_playersInfo[i] != null)
                    {
                        if (_playerID == _hostID) _hostID = i;
                        if (_playerID == _currentID) _currentID = i;
                    }
                }
            }

            return true;
        }


        //
        //
        //// Game run:

        // after set up game server should send player array of cards (10 or 9 cards)
        public void SetUpGame()
        {
            // devide card
            DevideCard();

            // set up value
            _stateID = 0;
            _currentRound = 0;
            _currentID = -1;
        }

        // receive data and update game <need take card>
        public ResponseForm HandleGame(RequestForm _playerRequest)
        {
            // _recvPlayerData contain:
            //     "stateID":    player current state id
            //     "playerID":   player id
            //     "playerName"  player name
            //     "card":       string (if dont send card back set to null)


            // return dictionary of error contain:
            //     "status":        fail
            //     "recceiveID" :   id send to, if empty do broadcast
            //     "messages":      explain error
            if (_playerRequest.stateID != _stateID)
            {
                var _response = new ResponseForm();
                _response.status = "fail";
                _response.messages = "Incorrect game's state";
                _response.recceiveID = _playerRequest.playerID;

                return _response;
            }
            else if (_playerRequest.playerID != _currentID)
            {
                var _response = new ResponseForm();
                _response.status = "fail";
                _response.messages = "Incorrect current player";
                _response.recceiveID = _playerRequest.playerID;

                return _response;
            }
            
            // return dictionary of success contain:
            //    "status": string (update game: success)
            //    "messages": string (explain error)
            switch (_playerRequest.stateID)
            {
                case 0: // Wait for player    |   return game ìnfo for player
                    return HandleWaitForPlayer(_playerRequest);
                case 1: // Set up game        |   devide cards and send for player
                    return HandleSetUpGame(_playerRequest);
                case 2: // Play card          |   move card to card holder
                    return HandlePlayCard(_playerRequest);
                case 3: // Take card          |   send card to player hand and remove card from deck or holder
                    return HandleTakeCard(_playerRequest);
                case 4: // Reset game         |   reset game, send back point
                    return ResetGame(_playerRequest);
                default:  // Defaul error     |   return error
                    {
                        var _response = new ResponseForm();
                        _response.status = "fail";
                        _response.messages = "Invalid input";
                        _response.recceiveID = _playerRequest.playerID;

                        return _response;
                    }
            }
        }

        // get all game info => dictionay
        public ResponseForm GetGameInfo(int _recvID = -1)
        {
            // recceive ID: -1, mean for broadcast

            var _reponse = new ResponseForm();
            _reponse.status = "success";
            _reponse.stateID = _stateID;
            _reponse.currentID = _currentID;
            _reponse.currentRound = _currentRound;
            _reponse.hostID = _hostID;
            _reponse.numberPlayer = _numberPlayer;
            _reponse.recceiveID = _recvID;
            _reponse.playerInfo = _playersInfo;
            _reponse.messages = String.Empty;
            
            return _reponse;
        }


        //
        //
        //// Game handle for update:

        // wait for player handle
        private ResponseForm HandleWaitForPlayer(RequestForm _playerRequest)
        {
            var _response = GetGameInfo();

            if (_playerRequest.playerID == -1)
            {
                if (AddPlayer(_playerRequest.playerName) == -1)
                {
                    _response.status = "fali";
                    _response.messages = "full player";
                }
                else
                {
                    _response.status = "success";
                    _response.messages = "Waiting for another player";
                }
            }

            return _response;
        }

        // sete up game handle
        private ResponseForm HandleSetUpGame(RequestForm _playerRequest)
        {
            var _reponse = new ResponseForm();

            // check number of player
            if (_numberPlayer < 2)
            {
                _reponse.status = "fail";
                _reponse.recceiveID = _playerRequest.playerID;
                _reponse.messages = "Not enough player";

                return _reponse;
            }

            // only host can start the game
            if (_playerRequest.playerID != _hostID)
            {
                _reponse.status = "fail";
                _reponse.recceiveID = _playerRequest.playerID;
                _reponse.messages = "Invalid host";

                return _reponse;
            }

            // this is instruction for server to send cards to player
            _stateID = 1;
            _currentID = _hostID;
            _currentRound = 1;

            _reponse = GetGameInfo();
            _reponse.status = "success";
            _reponse.messages = "Sending cards to player's hands";

            return _reponse;
        }

        // handle play card
        private ResponseForm HandlePlayCard(RequestForm _playerRequest)
        {
            // check u trang
            ResponseForm _response = HandleUTrang(_playerRequest);
            if (_response is null) _response = new ResponseForm();
            else return _response;

            // if player dont send card
            if (_playerRequest.sendCard is null)
            {
                _response.status = "fail";
                _response.recceiveID = _playerRequest.playerID;
                _response.messages = "Invalid card";

                return _response;
            }

            // if card not in hand
            int indexCard = Array.IndexOf(
                _playersHand[_playerRequest.playerID], 
                _playerRequest.sendCard
                );

            if (indexCard == -1)
            {
                _response.status = "fail";
                _response.recceiveID = _playerRequest.playerID;
                _response.messages = "Card not in hand";

                return _response;
            }

            // update value
            _playersHand[_playerRequest.playerID][indexCard] = null; 
            _cardHolder = _playerRequest.sendCard;

            while (true)
            {
                _currentID = (_currentID + 1) % 4;
                if (!(_playersInfo[_currentID] is null)) break;
            }

            _stateID = 3;

            _response = GetGameInfo();
            _response.cardHolder = _cardHolder;
            _response.messages = "update card holder";

            return _response;
        }

        // handle take card
        private ResponseForm HandleTakeCard(RequestForm _playerRequest)
        {
            // check u trang
            ResponseForm _response = HandleUTrang(_playerRequest);
            if (_response is null) _response = new ResponseForm();
            else return _response;

            // take card from card deck
            if (_playerRequest.sendCard is null)
            {
                // take card from draw deck
                var _card = _drawDeck.FirstOrDefault(s => !(s is null));

                // return error if no card left from draw deck
                if (_card is null)
                {
                    _response.status = "fail";
                    _response.messages = "Draw deck has no card left";
                    _response.recceiveID = _playerRequest.playerID;

                    return _response;
                }

                // update value
                _stateID = 2;
                if (_currentID == _hostID)
                {
                    _currentRound++;
                }
                while (true)
                {
                    _currentID = (_currentID + 1) % 4;
                    if (!(_playersInfo[_currentID] is null)) break;
                }

                _drawDeck[Array.IndexOf(_drawDeck, _card)] = null;

                _response = GetGameInfo();
                _response.cardPull = _card;
                _response.messages = "Sending card from deck";

                return _response;
            }

            // take card from card holder
            if (_playerRequest.sendCard != _cardHolder)
            {
                _response.status = "fail";
                _response.recceiveID = _playerRequest.playerID;
                _response.messages = "Invalid card from card holder";

                return _response;
            }
            else
            {
                // update value
                _stateID = 2;
                if (_currentID == _hostID)
                {
                    _currentRound++;
                }
                while (true)
                {
                    _currentID = (_currentID + 1) % 4;
                    if (!(_playersInfo[_currentID] is null)) break;
                }

                // take card from card holder
                var _card = _cardHolder;
                _cardHolder = null;

                _response = GetGameInfo();
                _response.cardPull = _card;
                _response.messages = "Sending card from card holder";

                return _response;
            }
        }

        // reset game to new state or new round
        private ResponseForm ResetGame(RequestForm _playerRequest)
        {
            var _reponse = new ResponseForm();

            // check is it end game
            if (_playerRequest.phom is null && _playerRequest.trash is null)
            {
                // check is it host
                if (_playerRequest.playerID != _hostID)
                {
                    _reponse.status = "fail";
                    _reponse.recceiveID = _playerRequest.playerID;
                    _reponse.messages = "Invalid host";

                    return _reponse;
                }

                // check is it 5 round
                if (_currentRound != 5)
                {
                    _reponse.status = "fail";
                    _reponse.recceiveID = _playerRequest.playerID;
                    _reponse.messages = "Invalid round to reset";

                    return _reponse;
                }
            }

            // reset everyone hand's cards
            Array.Clear(_playersHand, 0, _playersHand.Length);

            // scoring player point
            for (int i = 0; i < _playersHand.Length; i++)
            {
                if (_playersInfo[i] is null) continue;

                _playersInfo[i].point = Scoring(_playersHand[i]);
            }

            // reset card holder
            _cardHolder = null;

            // set up game again
            SetUpGame();

            var _response = GetGameInfo();

            // add status
            _reponse.status = "success";
            _reponse.messages = "Send point to players";

            return _reponse;
        }

        // get card decks to send
        public Card[][] GetCardsToSend()
        {
            if (_stateID != 1) return null;
            _stateID = 2;
            return _playersHand;
        }

        // handle u trang
        private ResponseForm HandleUTrang(RequestForm _playerRequest)
        {
            // check is there u trang
            if (_playerRequest.phom is null || _playerRequest.trash is null) return null;

            // create temp cards
            var _tempCards = _playerRequest.phom.ToList();
            _tempCards.AddRange(_playerRequest.trash.ToList());

            // check every cards are in hand
            foreach (var _card in _tempCards)
            {
                if (_card is null) continue;

                if (!_tempCards.Contains(_card))
                {
                    // create repoonse form
                    var _reponse = new ResponseForm();

                    _reponse.status = "fail";
                    _reponse.messages = "Invalid card in hand";

                    return _reponse;
                }
            }

            return ResetGame(_playerRequest);
        }

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
            for (int _value = _CardPip.Length - 1; _value > 2; _value--)
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

        private static bool CheckPhom(Card[] _cards)
        {
            return (CheckPhomDoc(_cards) || CheckPhomNgang(_cards));
        }

        // check valid phom doc (maybe)
        private static bool CheckPhomDoc(Card[] _cards)
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
        private static bool CheckPhomNgang(Card[] _cards)
        {
            if (_cards.Length <= 1) return true;
            return _cards.All(a => a.pip == _cards[0].pip);
        }


        //
        //
        //// card function:

        // devide card for all players
        private void DevideCard()
        {
            // create cards deck and shuffle
            this._drawDeck = CreateCardDeck();
            ShuffleCarDeck(_drawDeck);

            // divide cards for players
            for (int i = 0; i < 4; i++)
            {
                if (i == _hostID)
                {
                    _playersHand[i] = _drawDeck.Take(10).ToArray();
                    Array.Copy(_drawDeck, 10, _drawDeck, 0, _drawDeck.Length - 10);
                }
                else
                {
                    _playersHand[i] = _drawDeck.Take(9).ToArray();
                    Array.Copy(_drawDeck, 9, _drawDeck, 0, _drawDeck.Length - 9);
                }
            }
        }

        // create array of card
        private Card[] CreateCardDeck()
        {
            // for more detail read this url:
            // https://en.wikipedia.org/wiki/Standard_52-card_deck#:~:text=clubs%20(%E2%99%A3).-,Nomenclature,or%20%22Ace%20of%20Spades%22.

            Card[] _cards = new Card[52];

            int i = 0;
            foreach (var _pip in _CardPip.Select((value, index) => new { index, value }))
            {
                foreach (var _suit in _CardSuit)
                    _cards[i] = new Card(_pip.value, _suit, _pip.index + 1);
            }

            return _cards;
        }

        // Shuffle cards Deck
        private void ShuffleCarDeck(Card[] _CardDeck)
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

        // scoring cards
        private int Scoring(Card[] _cards)
        {
            Card[] _trash = OptimizePhom(_cards).Last();
            return _trash.Select(x => x.value).ToArray().Sum();
        }

        //
        //
        //// game value

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
