using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GameExtensions
{
    [Serializable]
    internal class PlayerInfo
    {
        public PlayerInfo(int id, string name, int point)
        {
            this.id = id;
            this.name = name;
            this.point = point;
        }

        public int id { get; set; }
        public string name { get; set; }
        public int point { get; set; }

        public byte[] Serialize()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(id);
                    writer.Write(name);
                    writer.Write(point);
                }
                return m.ToArray();
            }
        }

        public static PlayerInfo Desserialize(byte[] data)
        {
            PlayerInfo result = new PlayerInfo(-1, String.Empty, 0);
            using (MemoryStream m = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    result.id = reader.ReadInt32();
                    result.name = reader.ReadString();
                    result.point = reader.ReadInt32();
                }
            }
            return result;
        }
    }
}
