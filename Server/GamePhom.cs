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
        private Card[][] _playersHand = new Card[4][];

        //  _playersInfo: <playerID, playerName, gamePoint>
        private PlayerInfo[] _playersInfo = new PlayerInfo[4];

        // contain cards deck for draw after run set up game
        private Card[] _drawDeck = null;

        // card for pull
        private Card _cardHolder = null;

        // current data
        private int _stateID = 0;            // game state id
        private int _currentID = -1;         // turn for player's id
        private int _currentRound = -1;      // current number round
        private int _hostID = -1;            // current host id
        private int _numberPlayer = 0;       // number of player


        //
        //
        //// Player set up:

        // return player id if success or -1 if fail
        public int AddPlayer(string playerName)
        {
            if (_hostID < 0) _hostID = 0;
            _numberPlayer++;

            // assign player to empty locate
            int tempID = 0;
            while (tempID < 4)
            {
                if (_playersInfo[tempID] is null)
                {
                    _playersInfo[tempID] = new PlayerInfo(tempID, playerName, 0);
                    return tempID; // return add success
                }
                tempID++;
            }
            return -1; // return add fail
        }

        // return bool success or not
        public bool RemovePlayer(int playerID)
        {
            if (_playersInfo[playerID] is null) return false;

            _playersInfo[playerID] = null;
            _numberPlayer--;

            // if no player left
            if (_playersInfo.All(a => a is null))
            {
                _hostID = -1;
                _currentID = -1;
            }
            // shift host id to next player
            // shift current player to next player
            else if (playerID == _hostID || playerID == _currentID)
            {
                for (int i = (playerID + 1) % 4; i != playerID; i = (i + 1) % 4)
                {
                    if (_playersInfo[i] != null)
                    {
                        if (playerID == _hostID) _hostID = i;
                        if (playerID == _currentID) _currentID = i;
                    }
                }
            }

            return true;
        }


        //
        //
        //// Game run:
        
        // receive data and update game <need take card>
        public ResponseForm HandleGame(RequestForm playerRequest)
        {
            // assign player 
            if (playerRequest.playerID == -1)
                return HandleWaitForPlayer(playerRequest);

            // check correct request
            if (!(playerRequest.stateID == _stateID))
            {
                var res = new ResponseForm();
                res.status = "fail";
                res.messages = "Incorrect game's state";
                res.receiveID = playerRequest.playerID;

                return res;
            }
            else if (playerRequest.playerID != _currentID && _currentID != -1)
            {
                var res = new ResponseForm();
                res.status = "fail";
                res.messages = "Incorrect current player";
                res.receiveID = playerRequest.playerID;

                return res;
            }
            
            // return dictionary of error contain:
            //     "status":        fail
            //     "recceiveID" :   id send to, if empty do broadcast
            //     "messages":      explain error

            // return dictionary of success contain:
            //    "status": success
            //    "messages": string (explain error)
            //    game info ...
            switch (playerRequest.stateID)
            {
                case 0: // Set up game        |   devide cards and send for player
                    return HandleSetUpGame(playerRequest);
                case 2: // Play card          |   move card to card holder
                    return HandlePlayCard(playerRequest);
                case 3: // Take card          |   send card to player hand and remove card from deck or holder
                    return HandleTakeCard(playerRequest);
                case 4: // Reset game         |   reset game, send back point
                    return ResetGame(playerRequest);
                default:  // Defaul error     |   return error
                    {
                        var res = new ResponseForm();
                        res.status = "fail";
                        res.messages = "Invalid input";
                        res.receiveID = playerRequest.playerID;

                        return res;
                    }
            }
        }

        // get all game info => dictionay
        public ResponseForm GetGameInfo(int recvID = -1)
        {
            // recceive ID: -1, for broadcast

            var res = new ResponseForm();
            res.status = "success";
            res.stateID = _stateID;
            res.currentID = _currentID;
            res.currentRound = _currentRound;
            res.hostID = _hostID;
            res.numberPlayer = _numberPlayer;
            res.receiveID = recvID;
            res.playerInfo = _playersInfo;
            res.messages = String.Empty;
            
            return res;
        }

        // get card decks to send
        public ResponseForm[] GetCardsToSend()
        {
            if (_stateID != 1) return null;

            _stateID = 2;
            var res = new ResponseForm[4];
            for (int i = 0; i < 4; i++)
            {
                if (_playersInfo[i] is null) continue;
                res[i] = GetGameInfo(i);
                res[i].cardPull = _playersHand[i];
            }

            return res;
        }

        //  check game start
        public bool IsGameStart()
        {
            return (_stateID == 2 || _stateID == 3) ? true : false;
        }


        //
        //
        //// Game handle for update:

        // wait for player handle
        private ResponseForm HandleWaitForPlayer(RequestForm playerRequest)
        {
            // check is name existing
            foreach (var playerInfo in _playersInfo)
            {
                if (playerInfo is null) continue;
                if (playerInfo.name == playerRequest.playerName)
                {
                    var res = new ResponseForm();
                    res.status = "fail";
                    res.receiveID = playerRequest.playerID;
                    res.messages = "Player name existed";
                    return res;
                }
            }

            // check is player already assign
            if (playerRequest.playerID == -1)
            {
                // if add player unsuccess
                int newID = AddPlayer(playerRequest.playerName);
                if (newID == -1)
                {
                    var res = new ResponseForm();
                    res.status = "fail";
                    res.receiveID = playerRequest.playerID;
                    res.messages = "Full player";
                    return res;
                }
                // else assign success
                else
                {
                    var res = GetGameInfo();
                    res.senderID = newID;
                    res.status = "success";
                    res.messages = "Waiting for another player";

                    return res;
                }
            }
            // return already assign
            else
            {
                var res = new ResponseForm();
                res.status = "fail";
                res.receiveID = playerRequest.playerID;
                res.messages = "Already existed";
                return res;
            }
        }

        // sete up game handle
        private ResponseForm HandleSetUpGame(RequestForm playerRequest)
        {
            var res = new ResponseForm();

            // only host can start the game
            if (playerRequest.playerID != _hostID)
            {
                res.status = "fail";
                res.receiveID = playerRequest.playerID;
                res.messages = "Invalid host";

                return res;
            }

            SetUpGame();

            res = GetGameInfo();
            res.senderID = playerRequest.playerID;
            res.status = "success";
            res.messages = "Sending cards to player's hands";

            return res;
        }

        // handle play card
        private ResponseForm HandlePlayCard(RequestForm playerRequest)
        {
            // check u trang
            var res = HandleUTrang(playerRequest);
            if (res is null) res = new ResponseForm();
            else return res;

            // if player dont send card
            if (playerRequest.sendCard is null)
            {
                res.status = "fail";
                res.receiveID = playerRequest.playerID;
                res.messages = "Invalid card";

                return res;
            }

            // if card not in hand
            int indexCard = Array.FindIndex(_playersHand[playerRequest.playerID], 
                a => a != null &&
                     a.pip == playerRequest.sendCard.pip && 
                     a.suit == playerRequest.sendCard.suit);

            if (indexCard == -1)
            {
                res.status = "fail";
                res.receiveID = playerRequest.playerID;
                res.messages = "Card not in hand";

                return res;
            }

            // update value
            _playersHand[playerRequest.playerID][indexCard] = null; 
            _cardHolder = playerRequest.sendCard;

            // update next player
            while (true)
            {
                _currentID = (_currentID + 1) % 4;
                if (!(_playersInfo[_currentID] is null)) break;
            }

            // set state id
            _stateID = 3;

            // get basic info and send back
            res = GetGameInfo();
            res.senderID = playerRequest.playerID;
            res.cardHolder = _cardHolder;
            res.messages = "update card holder";

            return res;
        }

        // handle take card
        private ResponseForm HandleTakeCard(RequestForm playerRequest)
        {
            // check u trang
            var res = HandleUTrang(playerRequest);
            if (res is null) res = new ResponseForm();
            else return res;

            // take card from card deck
            if (playerRequest.sendCard is null)
            {
                // take card from draw deck
                var card = _drawDeck.FirstOrDefault(s => !(s is null));

                // return error if no card left from draw deck
                if (card is null)
                {
                    res.status = "fail";
                    res.messages = "Draw deck has no card left";
                    res.receiveID = playerRequest.playerID;

                    return res;
                }

                // update stateID and round
                _stateID = 2;
                if (_currentID == _hostID)
                {
                    _currentRound++;
                    if (_currentRound == 5) _stateID = 4;
                }

                _drawDeck[Array.IndexOf(_drawDeck, card)] = null;

                res = GetGameInfo();
                res.senderID = playerRequest.playerID;
                res.cardPull = new Card[] { card };
                res.messages = "Sending card from deck";

                return res;
            }

            // take card from card holder
            // if card not in card holder
            if (playerRequest.sendCard.pip != _cardHolder.pip && 
                playerRequest.sendCard.suit != _cardHolder.suit)
            {
                res.status = "fail";
                res.receiveID = playerRequest.playerID;
                res.messages = "Invalid card from card holder";

                return res;
            }
            // if card in card holder
            else 
            {
                // update value
                _stateID = 2;
                if (_currentID == _hostID)
                {
                    _currentRound++;
                    if (_currentRound == 5) _stateID = 4;
                }

                // take card from card holder
                var card = _cardHolder;
                _cardHolder = null;

                res = GetGameInfo();
                res.senderID = playerRequest.playerID;
                res.cardPull = new Card[] { card };
                res.messages = "Sending card from card holder";

                return res;
            }
        }

        // reset game to new state or new round
        private ResponseForm ResetGame(RequestForm playerRequest, bool utrang = false)
        {
            var res = new ResponseForm();

            // check is it end game
            if (!utrang)
            {
                // check is it host send reset
                if (playerRequest.playerID != _hostID)
                {
                    res.status = "fail";
                    res.receiveID = playerRequest.playerID;
                    res.messages = "Invalid host";

                    return res;
                }

                // check is it 5 round
                if (_currentRound != 5)
                {
                    res.status = "fail";
                    res.receiveID = playerRequest.playerID;
                    res.messages = "Invalid round to reset";

                    return res;
                }
            }
            else
            {
                if (_currentRound == 2 || _currentRound == 3)
                {
                    res.status = "fail";
                    res.receiveID = playerRequest.playerID;
                    res.messages = "Invalid round to reset";

                    return res;
                }
            }


            // scoring player point
            if (!utrang)
            {
                for (int i = 0; i < _playersHand.Length; i++)
                {
                    if (_playersInfo[i] is null) continue;
                    _playersInfo[i].point += Scoring(_playersHand[i]);
                }
            }
            else
            {
                for (int i = 0; i < _playersHand.Length; i++)
                {
                    // if there is no player or player u trang then skip
                    if (_playersInfo[i] is null) continue;
                    if (playerRequest.playerID == i) continue;

                    // add point to all player
                    _playersInfo[i].point += 12;
                }
            }

            // reset game data
            _playersHand = new Card[4][];
            _cardHolder = null;
            _drawDeck = null;

            _stateID = 0;
            _currentID = -1;
            _currentRound = -1;

            // add status and return
            res = GetGameInfo();
            res.status = "success";
            res.messages = "Send point to players";

            return res;
        }

        // handle u trang
        private ResponseForm HandleUTrang(RequestForm playerRequest)
        {
            // check is there u trang
            if (playerRequest.phom is null) return null;

            // create temp cards
            List<Card> tempCards = new List<Card>();
            foreach (var _temp in playerRequest.phom)
            {
                tempCards.AddRange(_temp);
            }
            if (playerRequest.trash != null) tempCards.AddRange(playerRequest.trash);

            // check every cards are in hand
            if (!tempCards.SequenceEqual(_playersHand[playerRequest.playerID]))
            {
                // create repoonse form
                var res = new ResponseForm();

                res.status = "fail";
                res.receiveID = playerRequest.playerID;
                res.messages = "Invalid cards in hand";

                return res;
            }

            return ResetGame(playerRequest, true);
        }

        // after set up game server should send player array of cards (10 or 9 cards)
        private void SetUpGame()
        {
            // devide card
            var tempDeck = DevideCard(_hostID, true);
            _playersHand = tempDeck.Take(4).ToArray();
            _drawDeck = tempDeck.Last();

            // set up date
            _stateID = 1;
            _currentID = _hostID;
            _currentRound = 1;
        }
    }
}
