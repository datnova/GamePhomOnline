using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class GamePhom
    {
        // max 4 players (min 2 players), each player contain array of max 10 cards (min 9 cards)
        private Dictionary<string, string>[][] _playersHand = new Dictionary<string, string>[4][];

        //  _playersInfo: <playerID, playerName, gamePoint>
        public Dictionary<string, string>[] _playersInfo = new Dictionary<string, string>[4];

        // contain cards deck for draw after run set up game
        public Dictionary<string, string>[] _drawDeck = null;

        // card for pull
        public Dictionary<string, string> _cardHolder = null;

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
                    _playersInfo[_tempID] = new Dictionary<string, string>() {
                        { "playerID" , _tempID.ToString() },
                        { "playerName", player_name },
                        { "gamePoint" , "0" },
                    };
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
            _playersHand[_playerID] = null;
            _numberPlayer--;

            // shift host id to next player
            // shift current player to next player
            if (_playerID == _hostID || _playerID == _currentID)
            {
                int i = (_playerID + 1) % 4;
                while (i != _playerID)
                {
                    if (_playersInfo[i] != null)
                    {
                        if (_playerID == _hostID) _hostID = i;
                        if (_playerID == _currentID) _currentID = i;
                    }
                    i = (i + 1) % 4;
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
            _currentRound = 1;
            _currentID = -1;
        }

        // receive data and update game <need take card>
        public Dictionary<string, string> HandleGame(Dictionary<string, string> _recvPlayerData)
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
            if (_recvPlayerData["stateID"] != _stateID.ToString())
            {
                return new Dictionary<string, string>() {
                    { "status", "Fail" },
                    { "recceiveID",  _recvPlayerData["playerID"] },
                    { "messages", "Incorrect game's state" },
                };
            }
            else if (_recvPlayerData["playerID"] != _currentID.ToString())
            {
                return new Dictionary<string, string>() {
                    { "status", "Fail" },
                    { "recceiveID",  _recvPlayerData["playerID"] },
                    { "messages", "Incorrect current player" },
                };
            }
            
            // return dictionary of success contain:
            //    "status": string (update game: success)
            //    "messages": string (explain error)
            switch (_recvPlayerData["stateID"])
            {
                case "0": // Wait for player    |   return game ìnfo for player
                    return HandleWaitForPlayer(_recvPlayerData);
                case "1": // Set up game        |   devide cards and send for player
                    return HandleSetUpGame(_recvPlayerData);
                case "2": // Play card          |   move card to card holder
                    return HandlePlayCard(_recvPlayerData);
                case "3": // Take card          |   send card to player hand and remove card from deck or holder
                    return HandleTakeCard(_recvPlayerData);
                case "4": // Reset game         |   reset game, send back point
                    return ResetGame(_recvPlayerData);
                default:  // Defaul error       |   return error
                    return new Dictionary<string, string>() {
                        { "status", "Fail" },
                        { "recceiveID",  _recvPlayerData["playerID"] },
                        { "messages", "Invalid input" },
                    };
            }
        }

        
        //
        //
        //// Game handle for update:

        // wait for player handle
        private Dictionary<string, string> HandleWaitForPlayer(Dictionary<string, string> _recvPlayerData)
        {
            if (_recvPlayerData["playerID"] == "-1")
            {
                AddPlayer(_recvPlayerData["playerName"]);
            }

            var _res = GetGameInfo();
            _res.Add("status", "Success");
            _res.Add("message", "Waiting for another player");

            return _res;
        }

        // sete up game handle
        private Dictionary<string, string> HandleSetUpGame(Dictionary<string, string> _recvPlayerData)
        {
            if (_numberPlayer < 2)
            {
                return new Dictionary<string, string>() {
                    { "status", "Fail" },
                    { "recceiveID",  _recvPlayerData["playerID"] },
                    { "message", "Not enough player" },
                };
            }

            if (_recvPlayerData["playerID"] != _hostID.ToString())
            {
                return new Dictionary<string, string>() {
                    { "status", "Fail" },
                    { "recceiveID",  _recvPlayerData["playerID"] },
                    { "message", "Invalid host" },
                };
            }

            // this is instruction for server to send cards to player
            _stateID = 1;
            return new Dictionary<string, string>() {
                { "status", "Success" },
                { "stateID", _stateID.ToString() },
                { "recceiveID",  "-1" },
                { "message", "Sending cards to player's hands" },
            };
        }

        // handle play card
        private Dictionary<string, string> HandlePlayCard(Dictionary<string, string> _recvPlayerData)
        {
            // if dont send card
            if (String.IsNullOrEmpty(_recvPlayerData["card"]))
            {
                return new Dictionary<string, string>(){
                    { "status", "Fail" },
                    { "recceiveID", _recvPlayerData["playerID"] },
                    { "message", "Invalid card" },
                };
            }

            // if invalid card
            var _tempCard = StringToCard(_recvPlayerData["card"]);
            if (_tempCard == null)
            {
                return new Dictionary<string, string>(){
                    { "status", "Fail" },
                    { "recceiveID", _recvPlayerData["playerID"] },
                    { "message", "Unrecognized card" },
                };
            }

            // if card not in hand
            int playerID = int.Parse(_recvPlayerData["playerID"]);
            int indexCard;
            if ((indexCard = Array.IndexOf(_playersHand[playerID], _tempCard)) == -1)
            {
                return new Dictionary<string, string>(){
                    { "status", "Fail" },
                    { "recceiveID", _recvPlayerData["playerID"] },
                    { "message", "Card not in hand" },
                };
            }

            // update value
            _playersHand[playerID][indexCard] = null; 
            _cardHolder = _tempCard;
            while (true)
            {
                _currentID = (_currentID + 1) % 4;
                if (!(_playersInfo[_currentID] is null)) break;
            }

            _stateID = 3;

            return new Dictionary<string, string>(){
                { "status", "Success" },
                { "stateID", _stateID.ToString() },
                { "currentID", _currentID.ToString() },
                { "cardHolder", CardToString(_cardHolder) },
                { "recceiveID", "-1" },
                { "message", "update card holder" },
            };
        }

        // handle take card
        private Dictionary<string, string> HandleTakeCard(Dictionary<string, string> _recvPlayerData)
        {
            // take card from card deck
            if (_recvPlayerData["card"] is null)
            {
                // take card from draw deck
                var _card = _drawDeck.FirstOrDefault(s => !(s is null));

                // return error if no card left from draw deck
                if (_card is null)
                {
                    return new Dictionary<string, string>() {
                        { "status", "Fail" },
                        { "recceiveID",  _recvPlayerData["playerID"] },
                        { "message", "Draw deck has no card left" },
                    };
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

                return new Dictionary<string, string>() {
                    { "status", "Success" },
                    { "stateID", _stateID.ToString() },
                    { "currentRound", _currentRound.ToString() },
                    { "currentID", _currentID.ToString() },
                    { "recceiveID",  "-1" },
                    { "card", CardToString(_card) },
                    { "message", "Sending card from deck" },
                };
            }

            // if card invalid
            if (StringToCard(_recvPlayerData["card"]) is null)
            {
                return new Dictionary<string, string>() {
                    { "status", "Fail" },
                    { "recceiveID",  _recvPlayerData["playerID"] },
                    { "message", "Invalid card" },
                };
            }
            else if (StringToCard(_recvPlayerData["card"]) != _cardHolder)
            {
                return new Dictionary<string, string>() {
                    { "status", "Fail" },
                    { "recceiveID",  _recvPlayerData["playerID"] },
                    { "message", "Invalid card from card holder" },
                };
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
                var _temp = _cardHolder;
                _cardHolder = null;

                return new Dictionary<string, string>() {
                    { "status", "Success" },
                    { "stateID", _stateID.ToString() },
                    { "currentRound", _currentRound.ToString() },
                    { "currentID", _currentID.ToString() },
                    { "recceiveID",  "-1" },
                    { "card", CardToString(_temp) },
                    { "message", "Sending card from deck" },
                };
            }
        }

        // reset game to new state or new round
        private Dictionary<string, string> ResetGame(Dictionary<string, string> _recvPlayerData)
        {
            // check is it host
            if (_recvPlayerData["playerID"] != _hostID.ToString())
            {
                return new Dictionary<string, string>() {
                    { "status", "Fail" },
                    { "recceiveID",  _recvPlayerData["playerID"] },
                    { "message", "Invalid host" },
                };
            }

            // check is it 5 round
            if (_currentRound != 5)
            {
                return new Dictionary<string, string>() {
                    { "status", "Fail" },
                    { "recceiveID",  _recvPlayerData["playerID"] },
                    { "message", "invalid round to reset" },
                };
            }

            var _res = GetGameInfo();

            // reset everyone hand cards
            Array.Clear(_playersHand, 0, _playersHand.Length);

            // reset player point info
            foreach (var _player in _playersInfo)
            {
                if (_player is null) continue;
                _player["gamePoint"] = "0";
            }

            // reset card holder
            _cardHolder = null;

            // set up game again
            SetUpGame();

            // add status
            _res.Add("status", "Success");
            _res.Add("message", "Send point to players");

            return _res;
        }

        // get card decks to send
        public Dictionary<string, string>[][] DecksToSend()
        {
            if (_stateID != 1) return null;
            _stateID = 2;
            return _playersHand;
        }

        //
        //
        //// Game function:

        // get all game info => dictionay
        public Dictionary<string, string> GetGameInfo(int _recvID = -1)
        {
            // recceive ID: -1, mean for broadcast

            var _gameStatus = new Dictionary<string, string>()
            {
                { "stateID", _stateID.ToString() },
                { "currentID", _currentID.ToString() },
                { "currentRound", _currentRound.ToString() },
                { "hostID", _hostID.ToString() },
                { "numberPlayer", _numberPlayer.ToString() },
                { "recceiveID",  _recvID.ToString() },
            };

            for (int i = 0; i < _playersInfo.Length; i++)
            {
                if (_playersInfo[i] is null)
                {
                    _gameStatus.Add("player" + i.ToString(), null);
                }
                else
                {
                    _gameStatus.Add("player" + i.ToString(),
                        _playersInfo[i]["playerID"] + " " +
                        _playersInfo[i]["playerName"] + " " +
                        _playersInfo[i]["gamePoint"]
                        );
                }
            }
            return _gameStatus;
        }

        // convert card to string
        public static string CardToString(Dictionary<string, string> _card)
        {
            if (_card is null) return String.Empty;
            return _card["pip"] + "-" + _card["suit"];
        }

        // convert string to card
        public static Dictionary<string, string> StringToCard(string _cardName)
        {
            var _tempCard = _cardName.Split('-');
            if (_tempCard.Length != 2) return null;

            if (Array.IndexOf(_CardPip, _tempCard[0]) == -1 ||
                Array.IndexOf(_CardSuit, _tempCard[1]) == -1)
            {
                return null;
            }

            return new Dictionary<string, string>() {
                { "pip", _tempCard[0] },
                { "suit", _tempCard[1] },
                { "value", Array.IndexOf(_CardPip, _tempCard[0]).ToString() },
            };
        }

        // devide card for all players
        private void DevideCard()
        {
            // create cards deck and shuffle
            _drawDeck = CreateCardDeck();
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
        private Dictionary<string, string>[] CreateCardDeck()
        {
            // for more detail read this url:
            // https://en.wikipedia.org/wiki/Standard_52-card_deck#:~:text=clubs%20(%E2%99%A3).-,Nomenclature,or%20%22Ace%20of%20Spades%22.

            Dictionary<string, string>[] _cards = new Dictionary<string, string>[52];

            int i = 0;
            foreach (var _pip in _CardPip.Select((value, index) => new { index, value }))
            {
                foreach (var _suit in _CardSuit)
                {
                    _cards[i] = new Dictionary<string, string>()
                    {
                        { "pip", _pip.value },
                        { "suit", _suit },
                        { "value", (_pip.index + 1).ToString() }
                    };
                    i++;
                }
            }

            return _cards;
        }

        // Shuffle cards Deck
        private void ShuffleCarDeck(Dictionary<string, string>[] _CardDeck)
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
