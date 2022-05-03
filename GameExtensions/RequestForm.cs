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

        public byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(stateID);
                    writer.Write(playerID);
                    writer.Write(playerName);

                    // add send card
                    if (sendCard is null) writer.Write(0);
                    else
                    {
                        var tempBuffer = sendCard.Serialize();
                        writer.Write(tempBuffer.Length);
                        writer.Write(tempBuffer, 0, tempBuffer.Length);
                    }

                    // add phom
                    if (phom is null) writer.Write(0);
                    else
                    {
                        writer.Write(phom.Length);

                        foreach (var cards in phom)
                        {
                            writer.Write(cards.Length);

                            foreach (var card in cards)
                            {
                                var tempBuffer = card.Serialize();
                                writer.Write(tempBuffer.Length);
                                writer.Write(tempBuffer, 0, tempBuffer.Length);
                            }
                        }
                    }

                    // add trash cards
                    if (trash is null) writer.Write(0);
                    else
                    {
                        writer.Write(trash.Length);

                        foreach (var card in trash)
                        {
                            var tempBuffer = card.Serialize();
                            writer.Write(tempBuffer.Length);
                            writer.Write(tempBuffer);
                        }
                    }

                }
                return stream.ToArray();
            }
        }

        public static RequestForm Desserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    var req = new RequestForm();

                    req.stateID = reader.ReadInt32();
                    req.playerID = reader.ReadInt32();
                    req.playerName = reader.ReadString();

                    // read card holder
                    req.sendCard = Card.Desserialize(reader.ReadBytes(reader.ReadInt32()));

                    // read card pull
                    // set up number of phom
                    req.phom = new Card[reader.ReadInt32()][];
                    if (req.phom.Length == 0) req.phom = null;
                    else
                    {
                        // loop though all phom
                        for (int i = 0; i < req.phom.Length; i++)
                        {
                            // set up number of cards in phom
                            req.phom[i] = new Card[reader.ReadInt32()];
                            for (int j = 0; j < req.phom[i].Length; j++)
                            {
                                // add card to phom
                                req.phom[i][j] = Card.Desserialize(reader.ReadBytes(reader.ReadInt32()));
                            }
                        }
                    }

                    // read player info
                    req.trash = new Card[reader.ReadInt32()];
                    if (req.trash.Length == 0) req.trash = null;
                    else
                    {
                        for (int i = 0; i < req.trash.Length; i++)
                        {
                            req.trash[i] = Card.Desserialize(reader.ReadBytes(reader.ReadInt32()));
                        }
                    }

                    return req;
                }
            }
        }
    }
}
