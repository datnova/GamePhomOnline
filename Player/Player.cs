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
    }
}
