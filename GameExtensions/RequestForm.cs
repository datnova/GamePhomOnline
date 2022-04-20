using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GameExtensions
{
    [Serializable]
    internal class RequestForm
    {
        public int stateID { get; set; } = 0;
        public int playerID { get; set; } = -1;
        public string playerName { get; set; } = String.Empty;
        public Card sendCard { get; set; } = null;
        public Card[][] phom { get; set; } = null;
        public Card[] trash { get; set; } = null;
    }
}
