using GameExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Player
{
    public partial class FormGiaoDien : Form
    {
        private static Player _player = null;
        private static TcpClient _client = null;
        private static int _port = 55555;
        private static string _ipAdd = "127.0.0.1";
        private static byte[] _buffer = new byte[1024];
        public  string playerName = String.Empty;

        public FormGiaoDien()
        {
            InitializeComponent();
        }

        private void FormGiaoDien_Load(object sender, EventArgs e)
        {
            // run Form to assign name
            RunAsignForm();

            // create thread to run game
            Thread runGame = new Thread(() =>
            {
                // create socket
                _client = new TcpClient(_ipAdd, _port);

                // assign player to server
                AssignPlayer();

                // run game
                RunGame();
            });
            runGame.IsBackground = true;
            runGame.Start();
        }


        //
        //
        /// update game data and display

        private void UpdateDisplay(ResponseForm res, int thisPlayerID)
        {
            // update other players
            int panelNumber = 1;
            int nextPlayerID = (thisPlayerID + 1) % 4;
            while (thisPlayerID != nextPlayerID)
            {
                // update other player panel
                UpdatePanelPlayers(res, nextPlayerID, panelNumber);

                // update next value
                panelNumber++;
                nextPlayerID = (nextPlayerID + 1) % 4;
            }

            // udpate main player
            UpdateMainPlayer(res);
        }

        private void UpdatePanelPlayers(ResponseForm res, int otherPlayerID, int panelNumber)
        {
            switch (panelNumber)
            {
                case 1:
                    if (res.playerInfo[otherPlayerID] is null)
                    {
                        Invoke(new Action(() =>
                        {
                            panel1.Hide();
                        }));
                    }
                    else
                    {
                        Invoke(new Action(() =>
                        {
                            panel1.Show();
                            name1.Text = res.playerInfo[otherPlayerID].name;
                            name1.Text += (res.hostID == otherPlayerID) ? " (host)" : String.Empty;
                            name1.BackColor = (res.currentID == otherPlayerID) ? Color.Green : Color.Red;
                        }));
                    }
                    break;

                case 2:
                    if (res.playerInfo[otherPlayerID] is null)
                    {
                        Invoke(new Action(() =>
                        {
                            panel2.Hide();
                        }));
                    }
                    else
                    {
                        Invoke(new Action(() =>
                        {
                            panel2.Show();
                            name2.Text = res.playerInfo[otherPlayerID].name;
                            name2.Text += (res.hostID == otherPlayerID) ? " (host)" : String.Empty;
                            name2.BackColor = (res.currentID == otherPlayerID) ? Color.Green : Color.Red;
                        }));
                    }
                    break;

                case 3:
                    if (res.playerInfo[otherPlayerID] is null)
                    {
                        Invoke(new Action(() =>
                        {
                            panel3.Hide();
                        }));
                    }
                    else
                    {
                        Invoke(new Action(() =>
                        {
                            panel3.Show();
                            name3.Text = res.playerInfo[otherPlayerID].name;
                            name3.Text += (res.hostID == otherPlayerID) ? " (host)" : String.Empty;
                            name3.BackColor = (res.currentID == otherPlayerID) ? Color.Green : Color.Red;
                        }));
                    }
                    break;
            }
        }

        private void UpdateMainPlayer(ResponseForm res)
        {
            // display name and specified host or not
            Invoke(new Action(() =>
            {
                main_name.Text = playerName;
                main_name.Text += (res.hostID == _player.GetPlayerInfo().id) ? " (host)" : String.Empty;
                main_name.BackColor = 
                    (res.currentID == res.playerInfo.First(a => a.name == playerName).id) ? 
                    Color.Green : Color.Red;
            }));

            // update display button
            UpdateButton();

            // update display card on hand
            UpdateDisplayCard();
        }

        // not finish
        private void UpdateButton()
        {
            throw new NotImplementedException();
        }


        //
        //
        /// handle socket and run game

        // req assign new player
        private void AssignPlayer()
        {
            if (_player.GetPlayerInfo().id == -1)
            {
                var req = _player.RequestAddPlayer();
                SendRequest(req);
            }
        }

        // send request to server
        private void SendRequest(RequestForm req)
        {
            // check is there connection
            if (!_client.Connected)
            {
                // close client socket if no connection
                _client.Close();
                return;
            }
            else
            {
                // create stream for sending request
                var stream = _client.GetStream();

                // serialize request and send
                var sendBuffer = req.Serialize();
                stream.Write(sendBuffer, 0, sendBuffer.Length);
            }
        }

        // run game
        private void RunGame()
        {
            while (_client.Connected)
            {
                // check is there data
                if (!_client.GetStream().DataAvailable) continue;

                // if there is data then get stream
                var stream = _client.GetStream();

                // read data from stream
                stream.Read(_buffer, 0, _buffer.Length);
                var res = ResponseForm.Desserialize(_buffer);
                Array.Clear(_buffer, 0, _buffer.Length);

                // update game data form response
                _player.HandleResponse(res);

                // update display
                UpdateDisplay(res, _player.GetPlayerInfo().id);
            }
        }


        //
        //
        /// Handle assign form
        private void RunAsignForm()
        {
            // run form dang nhap to get name
            this.Hide();
            using (var form = new FormDangNhap(this))
            {
                // load form dang nhap
                form.ShowDialog();
            }

            // if no name assign quit game or else create player
            if (String.IsNullOrEmpty(playerName)) this.Close();
            else
            {
                _player = new Player(playerName);
                this.Show();
            }
        }

        //
        //
        // Handle display cards (not finish)
        private void UpdateDisplayCard()
        {
            throw new NotImplementedException();
        }
    }
}
