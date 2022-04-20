using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GameExtensions
{
    [Serializable]
    internal class Card
    {
        public Card(string pip, string suit, int value)
        {
            this.pip = pip;
            this.suit = suit;
            this.value = value;
        }

        public override string ToString()
        {
            return this.pip + "_" + this.suit;
        }

        public string pip { get; set; }
        public string suit { get; set; }
        public int value { get; set; }

        public byte[] Serialize()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(pip);
                    writer.Write(suit);
                    writer.Write(value);
                }
                return m.ToArray();
            }
        }

        public static Card Desserialize(byte[] data)
        {
            Card result = new Card(String.Empty, String.Empty, 0);
            using (MemoryStream m = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    result.pip = reader.ReadString();
                    result.suit = reader.ReadString();
                    result.value = reader.ReadInt32();
                }
            }
            return result;
        }
    }
}
