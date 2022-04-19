using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Player
{
    public partial class PlayerForm : Form
    {
        public PlayerForm()
        {
            InitializeComponent();
        }

        // Deserialize
        public static object Deserialize(byte[] _buffer)
        {
            using (var _stream = new MemoryStream())
            {
                _stream.Write(_buffer, 0, _buffer.Length);
                _stream.Position = 0;
                BinaryFormatter _formatter = new BinaryFormatter();
                return _formatter.Deserialize(_stream);
            }
        }

        // Serialize
        public static byte[] Serialize(object _data)
        {
            using (MemoryStream _stream = new MemoryStream())
            {
                BinaryFormatter _formatter = new BinaryFormatter();
                _formatter.Serialize(_stream, _data);
                return _stream.ToArray();
            }
        }
    }
}
