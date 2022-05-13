using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameExtensions;

namespace Server
{
    internal class Program
    {
        private static TcpListener _server = new TcpListener(IPAddress.Any, 55555);
        private static GamePhom _gamePhom = new GamePhom();
        private static TcpClient[] _clientSockets = new TcpClient[4];

        static void Main(string[] args)
        {
            // start server
            Console.WriteLine("Starting server...");
            _server.Start();

            try
            {
                // thread get connection
                Console.WriteLine("Waiting for a connection...");
                while (true)
                {
                    // get connection
                    if (_gamePhom.IsGameStart()) continue;
                    var clientSocket = _server.AcceptTcpClient();

                    // new thread to handle connection
                    Thread handleThread = new Thread(() => HandleResponse(clientSocket));
                    handleThread.IsBackground = true;
                    handleThread.Start();
                }
            }
            catch(Exception ex)
            {
                // stop server
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Stoping server...");
                _server.Stop();
                Console.ReadLine();
            }
        }

        private static void HandleResponse(TcpClient clientSocket)
        {
            // get stream from socket
            var stream =  (clientSocket.Connected) ? clientSocket.GetStream() : null;

            try
            {
                // loop to check is connected
                while (CheckConnection(clientSocket))
                {
                    // if no data in stream to read check back connnection
                    if (!stream.DataAvailable) continue;

                    // read data from stream
                    var receivebuffer = new byte[1024];
                    stream.Read(receivebuffer, 0, receivebuffer.Length);

                    // deserialize to get request
                    var req = RequestForm.Desserialize(receivebuffer);

                    // handle request and return reponse
                    var res = _gamePhom.HandleGame(req);

                    // handle fail response
                    if (res.status == "fail") ServerSend(clientSocket, res);

                    // Add socket if assign success
                    if (!HandleAddSocket(res, clientSocket))
                    {
                        clientSocket.Close();
                        break;
                    }

                    // handle success response
                    if (res.status == "success") SendBackResponse(res);
                }

                Console.WriteLine("Player disconnected");
                HandleDisconnectSocket(clientSocket);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.ToString());
                HandleDisconnectSocket(clientSocket);
            }
        }


        //
        //
        /// send response

        private static void SendBackResponse(ResponseForm res)
        {
            // check send response
            if (res.receiveID != -1)
                ServerSend(_clientSockets[res.receiveID], res);
            else
                // if -1 mean broadcast
                for (int i = 0; i < 4; i++)
                    ServerSend(_clientSockets[i], res);

            // check send cards
            if (res.stateID == 1 && res.senderID == _gamePhom.GetGameInfo().hostID)
            {
                var reses = _gamePhom.GetCardsToSend();
                for (int i = 0; i < 4; i++)
                    ServerSend(_clientSockets[i], reses[i]);
            }
        }

        private static void ServerSend(TcpClient client, ResponseForm res)
        {
            if (client is null) return;

            if (CheckConnection(client))
            {
                try
                {
                    var stream = client.GetStream();
                    var sendBuffer = res.Serialize();
                    stream.Write(sendBuffer, 0, sendBuffer.Length);
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex.ToString());
                    HandleDisconnectSocket(client);
                }
            }
        }


        //
        //
        /// handle socket connection

        private static bool CheckConnection(TcpClient client)
        {
            // check client close connect
            if (!client.Connected)
            {
                HandleDisconnectSocket(client);
                return false;
            }

            // send empty buffer to check is there connection
            try
            {
                var stream = client.GetStream();
                stream.Write(new byte[0], 0, 0);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.ToString());
                HandleDisconnectSocket(client);
                return false;
            }

            // check crash close connect
            return true;
        }


        //
        //
        /// add and remove socket

        private static bool HandleAddSocket(ResponseForm res, TcpClient clientSocket)
        {
            if (res.status == "success" && res.senderID != -1)
            {
                _clientSockets[res.senderID] = clientSocket;
                return true;
            }
            else if (Array.IndexOf(_clientSockets, clientSocket) != -1)
            {
                return true;
            }
            return false;
        }

        private static void HandleDisconnectSocket(TcpClient client)
        {
            // is socket already remove
            var tempID = Array.IndexOf(_clientSockets, client);
            if (tempID == -1) return;

            // remove socket
            _clientSockets[tempID].Close();
            _clientSockets[tempID] = null;

            // remove player and send response to nother players
            if (_gamePhom.RemovePlayer(tempID))
            {
                SendBackResponse(_gamePhom.GetGameInfo());
            }
        }
    }
}
