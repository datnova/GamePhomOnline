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
        public int currentID { get; set; } = -1;
        public int currentRound { get; set; } = -1;
        public int hostID { get; set; } = -1;
        public int numberPlayer { get; set; } = -1;
        public int recceiveID { get; set; } = -1;
        public string messages { get; set; } = String.Empty;
        public Card cardHolder { get; set; } = null;
        public Card[] cardPull { get; set; } = null;
        public PlayerInfo[] playerInfo { get; set; } = null;

        public byte[] Serialize()
        {
            byte[] _tempBuffer;
            using (MemoryStream _stream = new MemoryStream())
            {
                using (BinaryWriter _writer = new BinaryWriter(_stream))
                {
                    _writer.Write(status);
                    _writer.Write(stateID);
                    _writer.Write(currentID);
                    _writer.Write(currentRound);
                    _writer.Write(hostID);
                    _writer.Write(numberPlayer);
                    _writer.Write(recceiveID);
                    _writer.Write(messages);

                    // add card holder
                    if (cardHolder is null) _writer.Write(0);
                    else
                    {
                        _tempBuffer = cardHolder.Serialize();
                        _writer.Write(_tempBuffer.Length);
                        _writer.Write(_tempBuffer, 0, _tempBuffer.Length);
                    }

                    // add card pull
                    if (cardPull is null) _writer.Write(0);
                    else
                    {
                        _writer.Write(cardPull.Length);

                        foreach (var _temp in cardPull)
                        {
                            _tempBuffer = _temp.Serialize();
                            _writer.Write(_tempBuffer.Length);
                            _writer.Write(_tempBuffer, 0, _tempBuffer.Length);
                        }
                    }

                    // add player info
                    if (playerInfo is null) _writer.Write(0);
                    else
                    {
                        _writer.Write(playerInfo.Length);

                        foreach (var _temp in playerInfo)
                        {
                            _tempBuffer = _temp.Serialize();
                            _writer.Write(_tempBuffer.Length);
                            _writer.Write(_tempBuffer);
                        }
                    }

                }
                return _stream.ToArray();
            }
        }

        public static ResponseForm Desserialize(byte[] data)
        {
            var _res = new ResponseForm();
            int _tempLength;
            using (MemoryStream _stream = new MemoryStream(data))
            {
                using (BinaryReader _reader = new BinaryReader(_stream))
                {
                    _res.status = _reader.ReadString();
                    _res.stateID = _reader.ReadInt32();
                    _res.currentID = _reader.ReadInt32();
                    _res.currentRound = _reader.ReadInt32();
                    _res.hostID = _reader.ReadInt32();
                    _res.numberPlayer = _reader.ReadInt32();
                    _res.recceiveID = _reader.ReadInt32();
                    _res.messages = _reader.ReadString();

                    // read card holder
                    if ((_tempLength = _reader.ReadInt32()) != 0)
                    {
                        _res.cardHolder = Card.Desserialize(_reader.ReadBytes(_tempLength));
                    }

                    // read card pull
                    if ((_tempLength = _reader.ReadInt32()) != 0)
                    {
                        _res.cardPull = new Card[_tempLength];
                        for (int i = 0; i < _res.cardPull.Length; i++)
                        {
                            _tempLength = _reader.ReadInt32();
                            _res.cardPull[i] = Card.Desserialize(_reader.ReadBytes(_tempLength));
                        }
                    }

                    // read player info
                    if ((_tempLength = _reader.ReadInt32()) != 0)
                    {
                        _res.playerInfo = new PlayerInfo[_tempLength];
                        for (int i = 0; i < _res.playerInfo.Length; i++)
                        {
                            _tempLength = _reader.ReadInt32();
                            _res.playerInfo[i] = PlayerInfo.Desserialize(_reader.ReadBytes(_tempLength));
                        }
                    }

                    return _res;
                }
            }
        }
    }
}
