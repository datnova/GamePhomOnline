using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameExtensions;

namespace Player
{
    internal class Player
    {
        // init
        public Player(string name)
        {
            _playersInfo = new PlayerInfo(-1, name, 0);
        }

        //
        //
        /// player value

        // contain card on hand
        public Card[] _playerHand = null;

        //  _playersInfo: <playerID, playerName, gamePoint>
        public PlayerInfo _playersInfo;

        // card for pull
        public Card[] _cardHolder = new Card[4];

        // current data
        public int _stateID = 0;            // game state id
        public int _currentID = -1;         // turn for player's id
        public int _currentRound = -1;      // current number round
        public int _hostID = -1;            // current host id
        public int _numberPlayer = 0;       // number of player

        public void HanldeResponse(ResponseForm _response)
        {
            if (_response.status == "success")
            {
                // get player hand or pull card
                if (_response.cardPull != null && _response.cardPull.Length != 0)
                    if (_response.cardPull.Length == 1)
                        _cardHolder[_currentID] = _response.cardPull[0];
                    else
                        _playerHand = _response.cardPull;

                // add id
                foreach (var _temp in _response.playerInfo)
                {
                    if (_temp is null) continue;
                    if (_temp.name == _playersInfo.name)
                    {
                        _playersInfo = _temp;
                        break;
                    }
                }

                _stateID = _response.stateID;
                _currentID = _response.currentID;
                _currentRound = _response.currentRound;
                _hostID = _response.hostID;
                _numberPlayer = _response.numberPlayer;
            }
        }

        public RequestForm RequestAddPlayer()
        {
            var _request = new RequestForm();
            _request.stateID = _stateID;
            _request.playerName = _playersInfo.name;
            _request.playerID = -1;
            return _request;
        }

        public RequestForm RequestPlayCard(Card _card)
        {
            var _request = new RequestForm();
            _request.playerName = _playersInfo.name;
            _request.playerID = _playersInfo.id;
            _request.sendCard = _card;
            return _request;
        }

        public RequestForm RequestTakeCard(bool _fromCardHolder)
        {
            var _request = new RequestForm();
            _request.playerName = _playersInfo.name;
            _request.playerID = _playersInfo.id;
            _request.sendCard = (_fromCardHolder) ? _cardHolder[_playersInfo.id] : null;
            return _request;
        }
    }
}
