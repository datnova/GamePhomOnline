using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GameExtensions;
using static GameExtensions.ConstantData;
using static Server.DeckTool;

namespace Server
{
    internal class GamePhom
    {
        //
        //
        /// game value

        // max 4 players (min 2 players), each player contain array of max 10 cards (min 9 cards)
        public Card[][] _playersHand = new Card[4][];

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
                _response.receiveID = _playerRequest.playerID;

                return _response;
            }
            else if (_playerRequest.playerID != _currentID)
            {
                var _response = new ResponseForm();
                _response.status = "fail";
                _response.messages = "Incorrect current player";
                _response.receiveID = _playerRequest.playerID;

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
                        _response.receiveID = _playerRequest.playerID;

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
            _reponse.receiveID = _recvID;
            _reponse.playerInfo = _playersInfo;
            _reponse.messages = String.Empty;
            
            return _reponse;
        }

        // after set up game server should send player array of cards (10 or 9 cards)
        private void SetUpGame()
        {
            // devide card
            var _tempDeck = DevideCard(_hostID, true);
            _playersHand = _tempDeck.Take(4).ToArray();
            _drawDeck = _tempDeck.Last();

            // set up date
            _stateID = 1;
            _currentID = _hostID;
            _currentRound = 1;
        }


        //
        //
        //// Game handle for update:

        // wait for player handle
        private ResponseForm HandleWaitForPlayer(RequestForm _playerRequest)
        {
            ResponseForm _response;
            if (_playerRequest.playerID == -1)
            {
                if (AddPlayer(_playerRequest.playerName) == -1)
                {
                    _response = new ResponseForm();
                    _response.status = "fail";
                    _response.receiveID = _playerRequest.playerID;
                    _response.messages = "Full player";
                }
                else
                {
                    _response = GetGameInfo();
                    _response.status = "success";
                    _response.messages = "Waiting for another player";
                }
            }
            else
            {
                _response = new ResponseForm();
                _response.status = "fail";
                _response.receiveID = _playerRequest.playerID;
                _response.messages = "Already existed";
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
                _reponse.receiveID = _playerRequest.playerID;
                _reponse.messages = "Not enough player";

                return _reponse;
            }

            // only host can start the game
            if (_playerRequest.playerID != _hostID)
            {
                _reponse.status = "fail";
                _reponse.receiveID = _playerRequest.playerID;
                _reponse.messages = "Invalid host";

                return _reponse;
            }

            SetUpGame();

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
                _response.receiveID = _playerRequest.playerID;
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
                _response.receiveID = _playerRequest.playerID;
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
                    _response.receiveID = _playerRequest.playerID;

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
                _response.cardPull = new Card[] { _card };
                _response.messages = "Sending card from deck";

                return _response;
            }

            // take card from card holder
            // if card not in card holder
            if (_playerRequest.sendCard != _cardHolder)
            {
                _response.status = "fail";
                _response.receiveID = _playerRequest.playerID;
                _response.messages = "Invalid card from card holder";

                return _response;
            }
            // if card in card holder
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
                _response.cardPull = new Card[] { _card };
                _response.messages = "Sending card from card holder";

                return _response;
            }
        }

        // reset game to new state or new round
        private ResponseForm ResetGame(RequestForm _playerRequest, bool utrang = false)
        {
            var _response = new ResponseForm();

            // check is it end game
            if (!utrang)
            {
                // check is it host
                if (_playerRequest.playerID != _hostID)
                {
                    _response.status = "fail";
                    _response.receiveID = _playerRequest.playerID;
                    _response.messages = "Invalid host";

                    return _response;
                }

                // check is it 5 round
                if (_currentRound != 5)
                {
                    _response.status = "fail";
                    _response.receiveID = _playerRequest.playerID;
                    _response.messages = "Invalid round to reset";

                    return _response;
                }
            }

            // reset everyone hand's cards
            Array.Clear(_playersHand, 0, _playersHand.Length);

            // scoring player point
            if (!utrang)
            {
                for (int i = 0; i < _playersHand.Length; i++)
                {
                    if (_playersInfo[i] is null) continue;
                    _playersInfo[i].point = Scoring(_playersHand[i]);
                }
            }
            else
            {
                for (int i = 0; i < _playersHand.Length; i++)
                {
                    if (_playersInfo[i] is null) continue;
                    if (_playerRequest.playerID == i) continue;
                    _playersInfo[i].point = 9999;
                }
            }

            // reset card holder
            _cardHolder = null;

            _response = GetGameInfo();

            // set up game again
            SetUpGame();

            // add status
            _response.status = "success";
            _response.messages = "Send point to players";

            return _response;
        }

        // get card decks to send
        public ResponseForm[] GetCardsToSend()
        {
            if (_stateID != 1) return null;
            _stateID = 2;

            var _reponses = new ResponseForm[4];
            for (int i = 0; i < 4; i++)
            {
                if (_playersInfo[i] is null) continue;
                _reponses[i] = GetGameInfo(i);
                _reponses[i].cardPull = _playersHand[i];
            }

            return _reponses;
        }

        // handle u trang
        private ResponseForm HandleUTrang(RequestForm _playerRequest)
        {
            // check is there u trang
            if (_playerRequest.phom is null || 
                _playerRequest.phom.Length < 3)
            {
                // create repoonse form
                var _reponse = new ResponseForm();

                _reponse.status = "fail";
                _reponse.receiveID = _playerRequest.playerID;
                _reponse.messages = "Invalid u trang";

                return _reponse;
            }

            // create temp cards
            List<Card> _tempCards = new List<Card>();
            foreach (var _temp in _playerRequest.phom)
            {
                _tempCards.AddRange(_temp);
            }
            if (_playerRequest.trash != null) _tempCards.AddRange(_playerRequest.trash);

            // check every cards are in hand
            if (!_tempCards.SequenceEqual(_playersHand[_playerRequest.playerID]))
            {
                // create repoonse form
                var _reponse = new ResponseForm();

                _reponse.status = "fail";
                _reponse.receiveID = _playerRequest.playerID;
                _reponse.messages = "Invalid cards in hand";

                return _reponse;
            }

            return ResetGame(_playerRequest, true);
        }
    }
}
