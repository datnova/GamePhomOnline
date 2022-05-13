using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameExtensions;

namespace Player
{
    internal class Player
    {
        //
        //
        /// init
        public Player(string name)
        {
            _playersInfo = new PlayerInfo(-1, name, 0);
        }


        //
        //
        /// player value

        // contain card on hand
        private Card[] _playerHand = null;

        //  _playersInfo: <playerID, playerName, gamePoint>
        private PlayerInfo _playersInfo;

        // card for pull
        private Card[] _cardHolder = new Card[4];

        // current data
        private int _stateID = 0;            // game state id
        private int _currentID = -1;         // turn for player's id
        private int _currentRound = -1;      // current number round
        private int _hostID = -1;            // current host id
        private int _numberPlayer = 0;       // number of player


        //
        //
        /// Handle and update game from response 
        public bool HandleResponse(ResponseForm res)
        {
            if (res.status == "success")
            {
                // get player hand or pull card
                if (res.cardPull != null && res.cardPull.Length != 0)
                {
                    if (res.cardPull.Length == 1)
                        _cardHolder[_currentID] = res.cardPull[0];
                    else
                        _playerHand = res.cardPull;
                }
                // check id already assign
                else if (_playersInfo.id == -1)
                {
                    foreach (var playerInfo in res.playerInfo)
                    {
                        if (playerInfo is null) continue;
                        if (playerInfo.name == _playersInfo.name)
                        {
                            _playersInfo = playerInfo;
                            break;
                        }
                    }
                }

                // update game info
                _stateID = res.stateID;
                _currentID = res.currentID;
                _currentRound = res.currentRound;
                _hostID = res.hostID;
                _numberPlayer = res.numberPlayer;

                return true;
            }
            else return false;
        }


        //
        //
        /// Get data method
        public PlayerInfo GetPlayerInfo()
        {
            return _playersInfo;
        }

        public (int stateID, int currentID, int currentRound, int hostID, int numberPlayer) GetGameInfo()
        {
            return (_stateID, _currentID, _currentRound, _hostID, _numberPlayer);
        }

        public Card[] GetPlayerHand()
        {
            return _playerHand;
        }

        public Card[] GetCardHolder()
        {
            return _cardHolder;
        }


        //
        //
        /// create request
        public RequestForm RequestAddPlayer()
        {
            if (_playersInfo.id != -1) return null;

            var req = new RequestForm();
            req.stateID = _stateID;
            req.playerName = _playersInfo.name;
            req.playerID = -1;
            return req;
        }

        public RequestForm RequestPlayCard(Card card)
        {
            if (_stateID != 2 || _currentID != _playersInfo.id) return null;

            int cardIndex = Array.IndexOf(_playerHand, card);
            if (cardIndex == -1) return null;
            else _playerHand[cardIndex] = null;

            var res = new RequestForm();
            res.playerName = _playersInfo.name;
            res.playerID = _playersInfo.id;
            res.sendCard = card;
            return res;
        }

        public RequestForm RequestTakeCard(bool takeFromCardHolder)
        {
            if (_stateID != 3 || _currentID != _playersInfo.id) return null;

            var res = new RequestForm();
            res.playerName = _playersInfo.name;
            res.playerID = _playersInfo.id;
            res.sendCard = (takeFromCardHolder) ? _cardHolder[_playersInfo.id] : null;
            return res;
        }
    
        public RequestForm RequestStartGame()
        {
            if (_stateID != 1 || _hostID != _playersInfo.id) return null;

            var res = new RequestForm();
            res.playerName = _playersInfo.name;
            res.stateID = _stateID;
            res.playerID = _playersInfo.id;
            return res;
        }

        public RequestForm RequestRestartGame()
        {
            if (_stateID != 4) return null;

            var res = new RequestForm();
            res.playerName = _playersInfo.name;
            res.stateID = _stateID;
            res.playerID = _playersInfo.id;
            return res;
        }
    }
}
