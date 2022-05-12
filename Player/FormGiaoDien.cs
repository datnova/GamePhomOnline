﻿using GameExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
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
                try
                {
                    // create socket
                    _client = new TcpClient(_ipAdd, _port);

                    // assign player to server
                    AssignPlayer();

                    // run game
                    RunGame();
                }
                catch (SocketException)
                {
                    Invoke(new Action(this.Hide));
                    MessageBox.Show("No server run");
                    Invoke(new Action(this.Close));
                }
            });
            runGame.IsBackground = true;
            runGame.Start();
        }


        //
        //
        /// update game data and display

        private void UpdateDisplay(ResponseForm res, int thisPlayerID)
        {
            // if not assign success quit program
            if (_player.GetPlayerInfo().id == -1) Invoke(new Action(this.Close));

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
            // identifile panel
            var tempPanel = (Panel)Controls["panel" + panelNumber];

            // check if player dont exist
            if (res.playerInfo[otherPlayerID] is null)
            {
                Invoke(new Action(tempPanel.Hide));
                return;
            }

            // if exist display name, specified host or not, on turn, card holder
            Invoke(new Action(() =>
            {
                // show panel
                tempPanel.Show();

                // set name + is host + color turn
                var tempName = (TextBox)tempPanel.Controls["name" + panelNumber];
                tempName.Text = res.playerInfo[otherPlayerID].name;
                tempName.Text += (res.hostID == otherPlayerID) ? " (host)" : String.Empty;
                tempName.BackColor = (res.currentID == otherPlayerID) ? Color.Green : Color.Orange;

                // set card holder
                var tempCardHolder = (PictureBox)tempPanel.Controls["cardholder" + panelNumber];
                if (_player.GetCardHolder()[otherPlayerID] is null) tempCardHolder.Image = null;
                else
                {
                    string nameCard = _player.GetCardHolder()[otherPlayerID].ToString();
                    tempCardHolder.Image = (Bitmap)Properties.Resources.ResourceManager.GetObject(nameCard);
                }
            }));
        }

        private void UpdateMainPlayer(ResponseForm res)
        {
            // display name, specified host or not, on turn, card holder and cards on hand
            Invoke(new Action(() =>
            {
                // set name + is host + color turn
                main_name.Text = playerName;
                main_name.Text += (res.hostID == _player.GetPlayerInfo().id) ? " (host)" : String.Empty;
                main_name.BackColor = 
                    (res.currentID == res.playerInfo.First(a => a.name == playerName).id) ? 
                    Color.Green : Color.Orange;

                // set card holder
                if (_player.GetCardHolder()[_player.GetPlayerInfo().id] is null) cardholder3.Image = null;
                else
                {
                    string nameCard = _player.GetCardHolder()[_player.GetPlayerInfo().id].ToString();
                    cardholder3.Image = (Bitmap)Properties.Resources.ResourceManager.GetObject(nameCard);
                }
            }));

            // update display button
            UpdateButton();

            // update display card on hand
            UpdateHandCards();
        }

        private void UpdateButton()
        {
            // get game info
            var gameState = _player.GetGameInfo();

            Invoke(new Action(() =>
            {
                // check rerange button
                if (_player.GetPlayerHand() != null) rerange_btn.Enabled = true;
                else rerange_btn.Enabled = false;

                // check is my turn
                if (gameState.currentID != _player.GetPlayerInfo().id)
                {
                    start_btn.Enabled = false;
                    take_btn.Enabled = false;
                    big_deck.Enabled = false;
                    return;
                }

                // check start button
                if (gameState.stateID == 1) start_btn.Enabled = true;
                else start_btn.Enabled = false;

                // check play button
                if (gameState.stateID == 2) play_btn.Enabled = true;
                else start_btn.Enabled = false;

                // check draw button
                if (gameState.stateID == 3)
                {
                    take_btn.Enabled = true;
                    big_deck.Enabled = true;
                } 
                else
                {
                    take_btn.Enabled = false;
                    big_deck.Enabled = false;
                }
            }));
        }

        private void UpdateHandCards()
        {
            // chekc are there cards in hand
            if (_player.GetPlayerHand() is null) return;

            // get every picture box to display if not then hide
            // get cards on hand
            var playerHand = _player.GetPlayerHand().Where(a => a != null).ToArray();
            for (int i = 0; i < 10; i++)
            {
                // get picture box element
                var tempPictureBox = (PictureBox)Controls["main_card" + (i + 1)];
                
                // check if cards out of range then hide picture box
                if (i >= playerHand.Length) tempPictureBox.Hide();
                else
                {
                    // display card
                    string nameCard = playerHand[i].ToString();
                    tempPictureBox.Image = (Bitmap)Properties.Resources.ResourceManager.GetObject(nameCard);
                }
            }
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
    }
}
