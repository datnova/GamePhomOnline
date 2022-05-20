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

        // time turn
        private int _timeTurn = 0;           // player's time end turn


        //
        //
        //// Player set up and remove

        // return player id if success or -1 if fail
        public int AddPlayer(string playerName, int money)
        {
            if (_hostID < 0) _hostID = 0;
            _numberPlayer++;

            // assign player to empty locate
            int tempID = 0;
            while (tempID < 4)
            {
                if (_playersInfo[tempID] is null)
                {
                    _playersInfo[tempID] = new PlayerInfo(tempID, playerName, money);
                    return tempID; // return add success
                }
                tempID++;
            }
            return -1; // return add fail
        }

        // return bool success or not
        public bool RemovePlayer(int playerID)
        {
            // check is player existing
            if (_playersInfo[playerID] is null) return false;

            // remove player 
            _playersInfo[playerID] = null;
            _numberPlayer--;

            // if no player left
            if (_numberPlayer == 0)
            {
                // reset player data
                _playersInfo = new PlayerInfo[4];
                _playersHand = new Card[4][];
                _drawDeck = null;
                _cardHolder = null;

                // reset game info
                _stateID = 0;
                _currentID = -1;
                _currentRound = -1;
                _hostID = -1;
                _numberPlayer = 0;
                _timeTurn = 0;

                return true;
            }

            // check removed player ís a host or a current turn player.
            if (playerID == _hostID || playerID == _currentID)
            {
                // loop to next available player
                for (int i = (playerID + 1) % 4; ; i = (i + 1) % 4)
                {
                    // if player not available go to next player
                    if (_playersInfo[i] is null) continue;

                    // shift host id to next player
                    if (playerID == _hostID) _hostID = i;

                    // shift current player turn to next player (might be bug)
                    if (playerID == _currentID) _currentID = i;

                    // change state if in draw card
                    _stateID = (_stateID == 2) ? 3 : _stateID;

                    break;
                }
            }

            // update time turn
            if (_stateID == 2 || _stateID == 3) 
                _timeTurn = (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

            return true;
        }


        //
        //
        //// Game run:

        // after set up game server should send player array of cards (10 or 9 cards)
        private void RunSetUpGame()
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


        //
        //
        /// Get game basic info

        // get all game info
        public ResponseForm GetGameInfo(int recvID = -1)
        {
            // recceive ID: -1, for broadcast
            var res = new ResponseForm();
            res.status = "success";
            res.timesTamp = (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
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

        //  check game start
        public bool IsGameStart()
        {
            return (_stateID == 2 || _stateID == 3) ? true : false;
        }


        //
        //
        //// Game handle for update:

        // receive data and update game
        public ResponseForm HandleGame(RequestForm playerRequest)
        {
            // handle chat
            if (playerRequest.chatMessages != string.Empty && playerRequest.playerID != -1)
                return HandleChat(playerRequest);

            // assign player 
            if (playerRequest.playerID == -1 && _stateID == 0)
                return HandleWaitForPlayer(playerRequest);

            // check correct request
            if (playerRequest.stateID != _stateID)
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
                    return HandleResetGame(playerRequest);
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
                int newID = AddPlayer(playerRequest.playerName, playerRequest.money);
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

        // set up game handle
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

            RunSetUpGame();

            res = GetGameInfo();
            res.senderID = playerRequest.playerID;
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

            // set up game turn
            _timeTurn = res.timesTamp;

            return res;
        }


        // handle take card
        private ResponseForm HandleTakeCard(RequestForm playerRequest)
        {
            // check u trang
            var res = HandleUTrang(playerRequest);
            if (res != null) return res;

            if (playerRequest.sendCard != null && 
                (playerRequest.sendCard.pip != _cardHolder.pip ||
                 playerRequest.sendCard.suit != _cardHolder.suit))
            {
                res.status = "fail";
                res.receiveID = playerRequest.playerID;
                res.messages = "Invalid card from card holder";

                return res;
            }

            // take card from card deck
            if (playerRequest.sendCard is null) return HanleDrawFromDeck(playerRequest);
            // take card from card holder
            else return HanleDrawFromHolder(playerRequest);
        }

        // draw card from card deck
        private ResponseForm HanleDrawFromDeck(RequestForm playerRequest)
        {
            var res = new ResponseForm();

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

            // update card deck and cards on hand
            _drawDeck[Array.IndexOf(_drawDeck, card)] = null;
            if (_playersHand[_currentID].All(a => a != null))
                _playersHand[_currentID] = _playersHand[_currentID].ToList().Append(card).ToArray();
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    if (_playersHand[_currentID][i] != null) continue;
                    _playersHand[_currentID][i] = card;
                    break;
                }
            }

            res = GetGameInfo();
            res.senderID = playerRequest.playerID;
            res.cardPull = new Card[] { card };

            _timeTurn = res.timesTamp;

            return res;
        }

        // draw card from card deck
        private ResponseForm HanleDrawFromHolder(RequestForm playerRequest)
        {
            var res = new ResponseForm();

            // update value
            _stateID = 2;
            if (_currentID == _hostID)
            {
                // increase round if back to host turn
                _currentRound++;
                // reset if all 4 round
                if (_currentRound == 5) _stateID = 4;
            }

            // take card from card holder and update card deck
            var card = _cardHolder;
            _cardHolder = null;

            if (_playersHand[_currentID].All(a => a != null))
                _playersHand[_currentID] = _playersHand[_currentID].ToList().Append(card).ToArray();
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    if (_playersHand[_currentID][i] != null) continue;
                    _playersHand[_currentID][i] = card;
                    break;
                }
            }

            res = GetGameInfo();
            res.senderID = playerRequest.playerID;
            res.cardPull = new Card[] { card };

            _timeTurn = res.timesTamp;

            return res;
        }


        // reset game to new state or new round
        private ResponseForm HandleResetGame(RequestForm playerRequest, bool utrang = false)
        {
            var res = new ResponseForm();

            // check is it end game or u trang
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
            // if it's u trang then check is it play or draw card turn
            else if (_currentRound == 2 || _currentRound == 3)
            {
                res.status = "fail";
                res.receiveID = playerRequest.playerID;
                res.messages = "Invalid round to reset";

                return res;
            }


            // scoring player point
            // if not u trang case
            if (!utrang)
            {
                // cuoc is 15% sum positive money
                var cuoc = (int)(_playersInfo.Where(a => a != null)
                                             .Select(a => Math.Max(a.money, 0))
                                             .Average() * 15 / 100);

                // ranking by score
                var playerScore = _playersHand.Select((a, i) => (_playersInfo[i] is null) ? null :
                                                  new { score = Scoring(a), index = i })
                                              .Where(a => a != null)
                                              .OrderBy(a => a.score)
                                              .ToArray();

                // cuoc * (1 + playerScore.Length) * playerScore.Length / (2 * playerScore.Length)
                int avgCuoc = cuoc * (1 + playerScore.Length) / 2;

                // calculate money
                for (int i = 0; i < playerScore.Length; i++)
                {
                    _playersInfo[playerScore[i].index].money +=  avgCuoc - (cuoc * (i + 1));
                }
            }
            // if u trang case
            else
            {
                // cuoc is 15% sum positive money
                var cuoc = (int)(_playersInfo.Where(a => a != null)
                                             .Select(a => Math.Max(a.money, 0))
                                             .Average() * 15 / 100);

                for (int i = 0; i < _playersInfo.Length; i++)
                {
                    if (_playersInfo[i] is null) continue;

                    // add 5 time all cuoc in table
                    if (playerRequest.playerID == i)
                        _playersInfo[i].money += cuoc * _numberPlayer * 5;

                    // give cuoc
                    _playersInfo[i].money -= cuoc * 5;
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

            return HandleResetGame(playerRequest, true);
        }

        // get card decks to send
        public ResponseForm[] GetCardsToSend()
        {
            if (_stateID != 1) return null;

            _stateID = 2;
            _timeTurn = GetGameInfo().timesTamp;

            var res = new ResponseForm[4];
            for (int i = 0; i < 4; i++)
            {
                if (_playersInfo[i] is null) continue;
                res[i] = GetGameInfo(i);
                res[i].timesTamp = _timeTurn;
                res[i].cardPull = _playersHand[i];
            }

            return res;
        }


        //
        //
        /// handle chat messages
        
        private ResponseForm HandleChat(RequestForm playerRequest)
        {
            // broadcast message to all player
            var res = new ResponseForm();
            res = GetGameInfo();
            res.senderID = playerRequest.playerID;
            res.messages = playerRequest.chatMessages;
            return res;
        }


        //
        //
        /// false request to end turn
        
        public RequestForm FalseEndTurn(int maxTime)
        {
            if (_stateID != 2 && _stateID != 3) return null;

            var timeNow = (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            var temp = timeNow - _timeTurn;
            if (timeNow - _timeTurn < maxTime) return null;
            else if (_stateID == 2)
            {
                // play first card on server
                var res = new RequestForm();
                res.stateID = _stateID;
                res.playerName = _playersInfo[_currentID].name;
                res.playerID = _currentID;
                res.sendCard = _playersHand[_currentID][0];

                return res;
            }
            else if (_stateID == 3)
            {
                // draw card
                var req = new RequestForm();
                req.stateID = _stateID;
                req.playerName = _playersInfo[_currentID].name;
                req.playerID = _currentID;
                req.sendCard = _cardHolder;

                return req;
            }
            return null;
        }
    }
}
