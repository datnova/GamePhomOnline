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

            // loop to check is connected
            while (clientSocket.Connected || stream != null)
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

                // Add socket if assign success
                if (!AddSocket(res, clientSocket)) break;

                // send back response
                SendBackResponse(res);
            }
        }

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
            if (res.stateID == 1)
            {
                var reses = _gamePhom.GetCardsToSend();
                for (int i = 0; i < 4; i++)
                    ServerSend(_clientSockets[i], reses[i]);
            }
        }

        private static void ServerSend(TcpClient client, ResponseForm res)
        {
            if (CheckConnection(client))
            {
                var stream = client.GetStream();
                var sendBuffer = res.Serialize();
                stream.Write(sendBuffer, 0, sendBuffer.Length);
            }
        }

        private static bool CheckConnection(TcpClient client)
        {
            if (!client.Connected)
            {
                var tempID = Array.IndexOf(_clientSockets, client);
                _clientSockets[tempID].Close();
                _clientSockets[tempID] = null;
                return false;
            }

            return true;
        }

        private static bool AddSocket(ResponseForm res, TcpClient clientSocket)
        {
            if (res.status == "success" && res.senderID == -1)
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
    }
}
