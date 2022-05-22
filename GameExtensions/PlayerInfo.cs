using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GameExtensions
{
    [Serializable]
    internal class PlayerInfo
    {
        public PlayerInfo(int id, string name, int money)
        {
            this.id = id;
            this.name = name;
            this.money = money;
        }

        public int id { get; set; }
        public string name { get; set; }
        public int money { get; set; }

        public byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(id);
                    writer.Write(name);
                    writer.Write(money);
                }
                return stream.ToArray();
            }
        }

        public static PlayerInfo Desserialize(byte[] buffer)
        {
            if (buffer.Length == 0) return null;

            PlayerInfo playerInfo = new PlayerInfo(-1, String.Empty, 0);
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    playerInfo.id = reader.ReadInt32();
                    playerInfo.name = reader.ReadString();
                    playerInfo.money = reader.ReadInt32();
                }
            }
            return playerInfo;
        }
    }
}