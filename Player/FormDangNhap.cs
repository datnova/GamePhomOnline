using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Player
{
    public partial class FormDangNhap : Form
    {
        public FormGiaoDien _formGiaoDien = null;

        public FormDangNhap(Form form)
        {
            _formGiaoDien = form as FormGiaoDien;
            InitializeComponent();
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonLogIn_Click(object sender, EventArgs e)
        {
            if (tbName.Text != String.Empty && tbName.Text != String.Empty)
            {
                _formGiaoDien.playerName = tbName.Text;
                _formGiaoDien.playerMoney = int.Parse(tbMoney.Text);
                this.Close();
            }
        }

        private void tbMoney_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}
