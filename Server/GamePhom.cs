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
        public Dictionary<string, string>[][] _playersHand = new Dictionary<string, string>[4][];

        /*
         _playersInfo:
            playerID,
            roundPoint,
            gamePoint,
         */
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
        public int _turnCounter = -1;       // count turn in a round, reset round if counter > number player

        // return player id if success or -1 if fail
        public int AddPlayer()
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
                        { "roundPoint", "0" },
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

        // after set up game server will send player array of cards (10 or 9 cards)
        public void SetUpGame()
        {
            // raise error
            if (_numberPlayer < 2)
            {
                throw new InvalidOperationException("not enough player to start a game!");
            }

            // devide card
            DevideCard();

            // set up value
            _stateID = 1;
            _currentRound = 1;
            _turnCounter = 1;
            _currentID = _hostID;
        }

        /*fix bug*/
        public Dictionary<string, string> UpdateGame(Dictionary<string, string> _recvPlayerData)
        {
            
            // _recvPlayerData contain:
            //     "stateID": string (this is number in type string)
            //     "currentID": string (this is number in type string)
            //     "card": string (if dont send card back set to empty string)

            
            // return dictionary of error contain:
            //     "status": string (update game: fail)
            //     "messages": string (explain error)
            if (int.Parse(_recvPlayerData["stateID"]) != _stateID)
            {
                return new Dictionary<string, string>() {
                    { "status", "Fail" },
                    { "messages", "Incorrect game's state" },
                };
            }
            else if (int.Parse(_recvPlayerData["currentID"]) != _currentID)
            {
                return new Dictionary<string, string>() {
                    { "status", "Fail" },
                    { "messages", "Incorrect current player" },
                };
            }

            
            // return dictionary of success contain:
            //    "status": string (update game: success)
            //    "messages": string (explain error)
            switch (_recvPlayerData["stateID"])
            {
                case "0": // Wait for player
                    return new Dictionary<string, string>() {
                        { "status", "success" },
                        { "messages", "" },
                    };

                case "1": // Set up game 
                    return new Dictionary<string, string>() {
                        { "status", "success" },
                        { "messages", "" },
                    };
                case "2": // Play card
                case "3": // Take card
                case "4": // End round
                case "5": // Reset round
                case "6": // End game
                case "7": // Reset game
            }
        }

        // reset game to new state
        public void ResetGame()
        {
            /*
            Reset everything to default but not player ID.
            So dont need to add player again.
            But still need to run SetUpGame()
            */

            // reset everyone hand cards
            Array.Clear(_playersHand, 0, _playersHand.Length);

            // reset player point info
            foreach (var _player in _playersInfo)
            {
                if (_player is null) continue;
                _player["roundPoint"] = "0";
                _player["gamePoint"]  = "0";
            }

            // reset card deck
            _drawDeck = null;

            // reset card holder
            _cardHolder = null;

            // reset game value to default
            _stateID = 0;            // game state id
            _currentID = -1;         // turn for player's id
            _currentRound = -1;      // current number round
            _hostID = -1;            // current host id
            _numberPlayer = 0;       // number of player
            _turnCounter = -1;       // count number of turn in one round
        }

        // only reset round
        public void ResetRound()
        {
            // reset everyone hand cards
            Array.Clear(_playersHand, 0, _playersHand.Length);

            // update player point info
            foreach (var _player in _playersInfo)
            {
                if (_player is null) continue;

                // update game point
                _player["gamePoint"] = (
                    int.Parse(_player["gamePoint"]) + 
                    int.Parse(_player["roundPoint"])
                ).ToString();

                _player["roundPoint"] = "0";
            }

            // devide card back to everyone hand
            DevideCard();

            // reset card deck
            _drawDeck = null;

            // reset game value for new round
            _stateID = 1;            // game state id
            _currentID = _hostID;    // turn for player's id
            _currentRound++;         // current number round
            _turnCounter = 1;        // count number of turn in one round
        }

        // get all game info => dictionay
        public Dictionary<string, Dictionary<string, string>> GetGameInfo(int _recvID = -1)
        {
            var _gameStatus = new Dictionary<string, string>()
            {
                { "status id", _stateID.ToString() },
                { "current id", _currentID.ToString() },
                { "current round", _currentRound.ToString() },
                { "host id", _hostID.ToString() },
                { "number player", _numberPlayer.ToString() },
                { "recceive ID",  (_recvID == -1) ? String.Empty : _recvID.ToString()}
            };

            return new Dictionary<string, Dictionary<string, string>>()
            {
                { "game status",     _gameStatus },
                { "player1",     _playersInfo[0] },
                { "player2",     _playersInfo[1] },
                { "player3",     _playersInfo[2] },
                { "player4",     _playersInfo[3] }
            };
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
                foreach (var _suit in _CardsSuit)
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
        public readonly string[] _gameState = new string[] {
            "Wait for player",  // all player can send except host
            "Set up game",      // only host can send this
            "Play card",
            "Take card",
            "End round",
            "Reset round",
            "End game",
            "Reset game"
        };

        // contain cards pip info
        private static readonly string[] _CardPip = new string[]
        {
            "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King"
        };

        // contain cards suit info
        private static readonly string[] _CardsSuit = new string[]
        {
            "Club", "Diamond", "Heard", "Spade"
        };
    }
}
