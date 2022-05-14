using GameExtensions;
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
                Invoke(new Action(() =>
                {
                    UpdatePanelPlayers(res, nextPlayerID, panelNumber);
                }));

                // update next value
                panelNumber++;
                nextPlayerID = (nextPlayerID + 1) % 4;
            }

            // udpate main player
            Invoke(new Action(() =>
            {
               UpdateMainPlayer(res);
            }));
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

                // set card on hand
                // if no card on hand
                var IsCardOnHand = _player.GetPlayerHand() != null;
                for (int i = 1; i < 10; i++)
                {
                    var tempPanelCard = (PictureBox)tempPanel.Controls["panelcard" + panelNumber + i];
                    if (!IsCardOnHand) tempPanelCard.Hide();
                    else tempPanelCard.Show();
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
                    (res.currentID == _player.GetPlayerInfo().id) ? 
                    Color.Green : Color.Orange;

                // set card holder
                if (_player.GetCardHolder()[_player.GetPlayerInfo().id] is null) cardholder3.Image = null;
                else
                {
                    string nameCard = _player.GetCardHolder()[_player.GetPlayerInfo().id].ToString();
                    cardholder3.Image = (Bitmap)Properties.Resources.ResourceManager.GetObject(nameCard);
                }

                // set display big deck
                var tempStateID = _player.GetGameInfo().stateID;
                if (tempStateID == 2 || tempStateID == 3) big_deck.Show();
                else big_deck.Hide();
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
                if (_player.GetPlayerHand() != null) rerange_btn.Show();
                else rerange_btn.Hide();

                // check start button
                if (gameState.numberPlayer >= 2 &&
                    gameState.hostID == _player.GetPlayerInfo().id &&
                    gameState.stateID == 0) start_btn.Show();
                else start_btn.Hide();

                // check play button
                if (gameState.stateID == 2 &&
                    gameState.currentID == _player.GetPlayerInfo().id) play_btn.Show();
                else play_btn.Hide();

                // check draw button
                if (gameState.stateID == 3 &&
                    gameState.currentID == _player.GetPlayerInfo().id)
                {
                    take_btn.Show();
                    big_deck.Enabled = true;
                } 
                else
                {
                    take_btn.Hide();
                    big_deck.Enabled = false;
                }
            }));
        }

        private void UpdateHandCards()
        {
            // check are there cards in hand if not then hide
            if (_player.GetPlayerHand() is null)
            {
                for (int i = 0; i < 10; i++)
                {
                    // get picture box element
                    var tempPictureBox = (PictureBox)Controls["main_card" + (i + 1)];
                    tempPictureBox.Image = null;
                }

                return;
            }

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
                if (!_player.HandleResponse(res))
                {
                    MessageBox.Show(res.messages);
                    continue;
                }

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
        /// Handle button
        private void start_btn_Click(object sender, EventArgs e)
        {
            var res = _player.RequestStartGame();
            SendRequest(res);
        }

        private void rerange_btn_Click(object sender, EventArgs e)
        {
            // rerange cards
            var tempCards = PhomTool.OptimizePhom(_player.GetPlayerHand());
            var tempHand = new List<Card>();

            foreach (var cards in tempCards)
                tempHand.AddRange(cards.ToList());

            // set up cards on hand
            _player.SetPlayerHand(tempHand.ToArray());

            // update display
            UpdateHandCards();
        }
    }
}
