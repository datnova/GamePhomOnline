using System;
using System.Collections.Generic;
using System.Text;

namespace GameExtensions
{
    internal static class ConstantData
    {
        // Contain all game state info
        public static readonly string[] gameState = new string[] {
            "Wait for player",  // Send the return of update                          (player after connected)
            "Set up game",      // Send cards to player's hands then set stateID to 2 (only host)
            "Play card",        // Send the return of update
            "Take card",        // Send card to current id
            "Reset game",       // Send cards to player's hands then set stateID to 2
        };

        // contain cards pip info
        public static readonly string[] cardPip = new string[]
        {
            "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King"
        };

        // contain cards suit info
        public static readonly string[] cardSuit = new string[]
        {
            "Club", "Diamond", "Heard", "Spade"
        };

    }
}
