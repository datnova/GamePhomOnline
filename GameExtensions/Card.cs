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
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(pip);
                    writer.Write(suit);
                    writer.Write(value);
                }
                return stream.ToArray();
            }
        }

        public static Card Desserialize(byte[] buffer)
        {
            if (buffer.Length == 0) return null;

            Card card = new Card(String.Empty, String.Empty, 0);
            using (MemoryStream m = new MemoryStream(buffer))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    card.pip = reader.ReadString();
                    card.suit = reader.ReadString();
                    card.value = reader.ReadInt32();
                }
            }
            return card;
        }
    }
}
