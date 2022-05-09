using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GameExtensions
{
    [Serializable]
    internal class ResponseForm
    {
        public string status { get; set; } = "success";
        public int stateID { get; set; } = -1;
        public int senderID { get; set; } = -1;
        public int currentID { get; set; } = -1;
        public int currentRound { get; set; } = -1;
        public int hostID { get; set; } = -1;
        public int numberPlayer { get; set; } = -1;
        public int receiveID { get; set; } = -1;
        public string messages { get; set; } = String.Empty;
        public Card cardHolder { get; set; } = null;
        public Card[] cardPull { get; set; } = null;
        public PlayerInfo[] playerInfo { get; set; } = null;

        public byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(status);
                    writer.Write(stateID);
                    writer.Write(senderID);
                    writer.Write(currentID);
                    writer.Write(currentRound);
                    writer.Write(hostID);
                    writer.Write(numberPlayer);
                    writer.Write(receiveID);
                    writer.Write(messages);

                    // add card holder
                    if (cardHolder is null) writer.Write(0);
                    else
                    {
                        var buffer = cardHolder.Serialize();
                        writer.Write(buffer.Length);
                        writer.Write(buffer, 0, buffer.Length);
                    }

                    // add card pull
                    if (cardPull is null) writer.Write(0);
                    else
                    {
                        writer.Write(cardPull.Length);

                        foreach (var card in cardPull)
                        {
                            var buffer = card.Serialize();
                            writer.Write(buffer.Length);
                            writer.Write(buffer, 0, buffer.Length);
                        }
                    }

                    // add player info
                    if (playerInfo is null) writer.Write(0);
                    else
                    {
                        writer.Write(playerInfo.Length);

                        foreach (var player in playerInfo)
                        {
                            if (player is null)
                            {
                                writer.Write(0);
                                continue;
                            }

                            var buffer = player.Serialize();
                            writer.Write(buffer.Length);
                            writer.Write(buffer, 0, buffer.Length);
                        }
                    }

                }
                return stream.ToArray();
            }
        }

        public static ResponseForm Desserialize(byte[] buffer)
        {
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    var res = new ResponseForm();

                    res.status       = reader.ReadString();
                    res.stateID      = reader.ReadInt32();
                    res.senderID     = reader.ReadInt32();
                    res.currentID    = reader.ReadInt32();
                    res.currentRound = reader.ReadInt32();
                    res.hostID       = reader.ReadInt32();
                    res.numberPlayer = reader.ReadInt32();
                    res.receiveID    = reader.ReadInt32();
                    res.messages     = reader.ReadString();

                    // read card holder
                    res.cardHolder = Card.Desserialize(reader.ReadBytes(reader.ReadInt32()));

                    // read card pull
                    res.cardPull = new Card[reader.ReadInt32()];
                    if (res.cardPull.Length == 0) res.cardPull = null;
                    else
                    {
                        for (int i = 0; i < res.cardPull.Length; i++)
                        {
                            res.cardPull[i] = Card.Desserialize(reader.ReadBytes(reader.ReadInt32()));
                        }
                    }

                    // read player info
                    res.playerInfo = new PlayerInfo[reader.ReadInt32()];
                    if (res.playerInfo.Length == 0) res.playerInfo = null;
                    else
                    {
                        for (int i = 0; i < res.playerInfo.Length; i++)
                        {
                            res.playerInfo[i] = PlayerInfo.Desserialize(reader.ReadBytes(reader.ReadInt32()));
                        }
                    }

                    return res;
                }
            }
        }
    }
}
