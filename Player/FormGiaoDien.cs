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
        private static string _ipAdd = "127.0.0.1";
        private static int _port = 55555;
        private static TcpClient _client = null;
        private static byte[] _buffer = new byte[1024];

        private static Player _player = null;
        private static Card _cardChoose = null;

        public string playerName;
        public int playerMoney;

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
                Invoke(new Action(() => DisplayPanelPlayers(res, nextPlayerID, panelNumber)));

                // update next value
                panelNumber++;
                nextPlayerID = (nextPlayerID + 1) % 4;
            }

            // udpate main player
            Invoke(new Action(() => DisplayMainPlayer(res)));
        }

        // display panel other player
        private void DisplayPanelPlayers(ResponseForm res, int otherPlayerID, int panelNumber)
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
                if (_player.GetCardHolder()[otherPlayerID] is null) tempCardHolder.Hide();
                else if (_player.GetGameInfo().stateID == 0) tempCardHolder.Hide();
                else
                {
                    string nameCard = _player.GetCardHolder()[otherPlayerID].ToString();
                    tempCardHolder.Image = (Bitmap)Properties.Resources.ResourceManager.GetObject(nameCard);
                    tempCardHolder.Show();
                }

                // set card on hand
                // if no card on hand
                var IsCardOnHand = _player.GetPlayerHand() != null;
                for (int i = 1; i < 10; i++)
                {
                    var tempPanelCard = (PictureBox)tempPanel.Controls["panelcard" + panelNumber + i];
                    if (!IsCardOnHand || _player.GetGameInfo().stateID == 0) tempPanelCard.Hide();
                    else tempPanelCard.Show();
                }
            }));
        }

        // display main player
        private void DisplayMainPlayer(ResponseForm res)
        {
            // display name, specified host or not, on turn, card holder and cards on hand
            Invoke(new Action(() =>
            {
                // display name + is host + color turn
                DisplayNameTag(res);

                // display big deck
                DisplayBigDeck();

                // display main card holder
                DisplayMainCardHolder();

                // display card choose
                DisplayCardChoose();

                // display display button
                DisplayButton();

                // display display card on hand
                DisplayHandCards();

                // display game info table
                DisplayGameInfo();

                // display game score
                DisplayGameMoney(res);
            }));
        }

        private void DisplayNameTag(ResponseForm res)
        {
            main_name.Text = playerName;
            main_name.Text += (res.hostID == _player.GetPlayerInfo().id) ? " (host)" : String.Empty;
            main_name.BackColor =
                (res.currentID == _player.GetPlayerInfo().id) ?
                Color.Green : Color.Orange;
        }

        private void DisplayBigDeck()
        {
            // set display big deck
            var tempStateID = _player.GetGameInfo().stateID;
            if (tempStateID == 2 || tempStateID == 3) big_deck.Show();
            else big_deck.Hide();
        }

        private void DisplayMainCardHolder()
        {
            // set card holder
            if (_player.GetCardHolder()[_player.GetPlayerInfo().id] is null) mainholder.Hide();
            else if (_player.GetGameInfo().stateID == 0) mainholder.Hide();
            else
            {
                mainholder.Show();
                string nameCard = _player.GetCardHolder()[_player.GetPlayerInfo().id].ToString();
                mainholder.Image = (Bitmap)Properties.Resources.ResourceManager.GetObject(nameCard);
            }
        }

        private void DisplayButton()
        {
            // get game info
            var gameState = _player.GetGameInfo();

            // check rerange button
            if (_player.GetPlayerHand() != null && _player.GetGameInfo().stateID != 0) rerange_btn.Show();
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
        }

        private void DisplayHandCards()
        {
            // check are there cards in hand if not then hide
            if (_player.GetPlayerHand() is null || _player.GetGameInfo().stateID == 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    // get picture box element
                    var tempPictureBox = (PictureBox)Controls["main_card" + (i + 1)];
                    tempPictureBox.Hide();
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
                    tempPictureBox.Show();
                    string nameCard = playerHand[i].ToString();
                    tempPictureBox.Image = (Bitmap)Properties.Resources.ResourceManager.GetObject(nameCard);
                }
            }
        }

        private void DisplayGameInfo()
        {
            if (_player.GetGameInfo().stateID == 0)
            {
                info_game_table.Hide();
                return;
            }

            info_game_table.Show();
            info_game_table.Text = $"state: {ConstantData.gameState[_player.GetGameInfo().stateID]}\r\n";
            info_game_table.Text += $"current id: {_player.GetGameInfo().currentID}\r\n";
            info_game_table.Text += $"current round: {_player.GetGameInfo().currentRound}\r\n";
        }

        private void DisplayGameMoney(ResponseForm res)
        {
            // only update at wait player
            if (res.stateID != 0) return;

            // get list player
            var tempPlayer = res.playerInfo.Where(a => a != null);
            tempPlayer = tempPlayer.OrderByDescending(a => a.money).ToArray();

            // set value to point table
            money_table.Clear(); 
            foreach (var player in tempPlayer)
            {
                if (player is null) continue;
                money_table.Text += $"{player.name}: {player.money}\r\n";
            }
        }

        private void DisplayCardChoose()
        {
            if (_cardChoose is null ||
                _player.GetGameInfo().stateID == 0) card_choose.Hide();
            else card_choose.Show();
        }

        //
        //
        /// handle socket and run game

        // req assign new player
        private void AssignPlayer()
        {
            if (_player.GetPlayerInfo() is null || _player.GetPlayerInfo().id == -1)
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

        // send request to end game
        private void HandleEndGame()
        {
            var req = _player.RequestRestartGame();
            if (req is null) return;

            SendRequest(req);
        }

        // reset game if no player is play
        private void HandleNoPlayerContinue()
        {
            var tempGameInfo = _player.GetGameInfo();
            if ((tempGameInfo.stateID == 2 ||
                tempGameInfo.stateID == 3) &&
                tempGameInfo.numberPlayer == 1) this.Close();
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

                // display message
                if (DisplayChatMessages(res.playerInfo[res.senderID].name, res.messages)) continue;

                // check end game to reset
                HandleEndGame();

                // if there is one player continue to play then quit
                HandleNoPlayerContinue();

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
                _player = new Player(playerName, playerMoney);
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

            // display hand cards
            Invoke(new Action(DisplayHandCards));
        }

        private void play_btn_Click(object sender, EventArgs e)
        {
            var req = _player.RequestPlayCard(_cardChoose);
            if (req is null)
            {
                MessageBox.Show("Error");
                return;
            }

            _cardChoose = null;
            card_choose.Image = null;
            SendRequest(req);
        }

        // take card click
        private void big_deck_Click(object sender, EventArgs e)
        {
            var req = _player.RequestTakeCard(takeFromCardHolder: false);
            SendRequest(req);
        }

        private void take_btn_Click(object sender, EventArgs e)
        {
            var req = _player.RequestTakeCard(takeFromCardHolder: true);
            SendRequest(req);
        }


        //
        //
        /// Handle choosing cards.
        
        void SetCardChoose(object sender, EventArgs e)
        {
            var mainCard = (PictureBox)sender;
            // if no card
            if (mainCard.Image is null)
            {
                card_choose.Image = null;
                return;
            }

            // get card index 
            string cardIndexStr = mainCard.Name.Substring(mainCard.Name.Length - 1);
            int cardIndex = int.Parse(cardIndexStr);
            cardIndex = (cardIndex == 0) ? 9 : cardIndex - 1;

            // set card choose
            _cardChoose = _player.GetPlayerHand()[cardIndex];

            // update display card choose
            card_choose.Image = mainCard.Image;
            card_choose.Show();
        }


        //
        //
        /// hanle chat

        private void btn_sendchat_Click(object sender, EventArgs e)
        {
            // make request
            RequestForm req = _player.RequestSendChat(input_chat.Text);
            input_chat.Clear();

            // send request
            if (req is null) return;
            SendRequest(req);
        }

        private bool DisplayChatMessages(string name, string messages)
        {
            // if no messages
            if (messages == String.Empty) return false;

            // display messages
            Invoke(new Action(() => {
                // display to chat box
                chat_box.Text += name.PadRight(7) + ": " + messages + Environment.NewLine;

                // auto scroll to end
                chat_box.ScrollToCaret();
            }));

            return true;
        }
    }
}
