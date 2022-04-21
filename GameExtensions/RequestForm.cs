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
            byte[] _tempBuffer;
            using (MemoryStream _stream = new MemoryStream())
            {
                using (BinaryWriter _writer = new BinaryWriter(_stream))
                {
                    _writer.Write(stateID);
                    _writer.Write(playerID);
                    _writer.Write(playerName);

                    // add send card
                    if (sendCard is null) _writer.Write(0);
                    else
                    {
                        _tempBuffer = sendCard.Serialize();
                        _writer.Write(_tempBuffer.Length);
                        _writer.Write(_tempBuffer, 0, _tempBuffer.Length);
                    }

                    // add phom
                    if (phom is null) _writer.Write(0);
                    else
                    {
                        _writer.Write(phom.Length);

                        foreach (var _arr in phom)
                        {
                            _writer.Write(_arr.Length);

                            foreach (var _temp in _arr)
                            {
                                _tempBuffer = _temp.Serialize();
                                _writer.Write(_tempBuffer.Length);
                                _writer.Write(_tempBuffer, 0, _tempBuffer.Length);
                            }
                        }
                    }

                    // add trash
                    if (trash is null) _writer.Write(0);
                    else
                    {
                        _writer.Write(trash.Length);

                        foreach (var _temp in trash)
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

        public static RequestForm Desserialize(byte[] data)
        {
            var _res = new RequestForm();
            int _tempLength;
            using (MemoryStream _stream = new MemoryStream(data))
            {
                using (BinaryReader _reader = new BinaryReader(_stream))
                {
                    _res.stateID = _reader.ReadInt32();
                    _res.playerID = _reader.ReadInt32();
                    _res.playerName = _reader.ReadString();

                    // read card holder
                    if ((_tempLength = _reader.ReadInt32()) != 0)
                    {
                        _res.sendCard = Card.Desserialize(_reader.ReadBytes(_tempLength));
                    }

                    // read card pull
                    if ((_tempLength = _reader.ReadInt32()) != 0)
                    {
                        _res.phom = new Card[_tempLength][];
                        for (int i = 0; i < _res.phom.Length; i++)
                        {
                            _tempLength = _reader.ReadInt32();
                            _res.phom[i] = new Card[_tempLength];
                            for (int j = 0; j < _res.phom[i].Length; j++)
                            {
                                _tempLength = _reader.ReadInt32();
                                _res.phom[i][j] = Card.Desserialize(_reader.ReadBytes(_tempLength));
                            }
                        }
                    }

                    // read player info
                    if ((_tempLength = _reader.ReadInt32()) != 0)
                    {
                        _res.trash = new Card[_tempLength];
                        for (int i = 0; i < _res.trash.Length; i++)
                        {
                            _tempLength = _reader.ReadInt32();
                            _res.trash[i] = Card.Desserialize(_reader.ReadBytes(_tempLength));
                        }
                    }

                    return _res;
                }
            }
        }
    }
}
