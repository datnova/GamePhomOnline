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
        public Player(string name, int money)
        {
            _playersInfo = new PlayerInfo(-1, name, money);
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
            // if fail return false
            if (res.status != "success") return false;

            // get player hand or pull card
            if (res.cardPull != null && res.cardPull.Length != 0)
            {
                // update player hand when devide cards
                if (_playerHand is null)
                    _playerHand = res.cardPull;
                // update draw card
                else if (res.senderID == _playersInfo.id)
                {
                    // add card pull to hand
                    int tempNullIndex = _playerHand.ToList().FindIndex(a => a is null);
                    if (tempNullIndex == -1) _playerHand = _playerHand.ToList().Append(res.cardPull[0]).ToArray();
                    else _playerHand[tempNullIndex] = res.cardPull[0];

                    // erase card holder
                    var tempHolderIndex = _cardHolder.ToList()
                                                     .FindIndex(a => a != null &&
                                                                     a.pip == res.cardPull[0].pip &&
                                                                     a.suit == res.cardPull[0].suit);
                    if (tempHolderIndex != -1) _cardHolder[tempHolderIndex] = null;
                }
                // if other player pull card from card holder
                else
                {
                    // erase card holder
                    var tempHolderIndex = _cardHolder.ToList()
                                                     .FindIndex(a => a != null &&
                                                                     a.pip == res.cardPull[0].pip &&
                                                                     a.suit == res.cardPull[0].suit);
                    if (tempHolderIndex != -1) _cardHolder[tempHolderIndex] = null;
                }
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

            // update card holder when play card
            if (res.cardHolder != null)
                _cardHolder[_currentID] = res.cardHolder;

            // set up default if reset game
            if (_stateID == 0)
            {
                _cardHolder = new Card[4];
                _playerHand = null;
            }

            // update game info
            _stateID = res.stateID;
            _currentID = res.currentID;
            _currentRound = res.currentRound;
            _hostID = res.hostID;
            _numberPlayer = res.numberPlayer;

            return true;
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
        /// Set cards on hand
        public void SetPlayerHand(Card[] cards)
        {
            // set cards to length 10
            var tempCards = new Card[10];
            for (int i = 0; i < cards.Length; i++)
                tempCards[i] = cards[i];

            // set player hand
            _playerHand = tempCards;
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
            req.money = _playersInfo.money;
            req.playerID = -1;
            return req;
        }

        public RequestForm RequestPlayCard(Card card)
        {
            // check value
            if (card is null || _stateID != 2 || _currentID != _playersInfo.id) return null;

            int cardIndex = Array.FindIndex(_playerHand, a => 
                a != null && a.pip == card.pip && a.suit == card.suit);

            if (cardIndex == -1) return null;
            else _playerHand[cardIndex] = null;

            var res = new RequestForm();
            res.stateID = _stateID;
            res.playerName = _playersInfo.name;
            res.playerID = _playersInfo.id;
            res.sendCard = card;
            return res;
        }

        public RequestForm RequestTakeCard(bool takeFromCardHolder)
        {
            if (_stateID != 3 || _currentID != _playersInfo.id) return null;

            var req = new RequestForm();
            req.playerName = _playersInfo.name;
            req.stateID = _stateID;
            req.playerID = _playersInfo.id;

            // get card from card holder
            var cardHolderIndex = (_playersInfo.id + 3) % 4; // (decrease 1)
            while (cardHolderIndex != _playersInfo.id)
            {
                if (_cardHolder[cardHolderIndex] != null) break;
                cardHolderIndex = (cardHolderIndex + 3) % 4;
            }
            req.sendCard = (takeFromCardHolder) ? _cardHolder[cardHolderIndex] : null;
            return req;
        }
    
        public RequestForm RequestStartGame()
        {
            if (_stateID != 0 || _hostID != _playersInfo.id) return null;

            var res = new RequestForm();
            res.playerName = _playersInfo.name;
            res.stateID = _stateID;
            res.playerID = _playersInfo.id;
            return res;
        }

        public RequestForm RequestRestartGame()
        {
            if (_currentRound != 5 || _playersInfo.id != _hostID) return null;

            var res = new RequestForm();
            res.playerName = _playersInfo.name;
            res.stateID = _stateID;
            res.playerID = _playersInfo.id;
            return res;
        }
    }
}
