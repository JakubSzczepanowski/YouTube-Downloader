using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YT_Downloader
{
    public partial class helpForm : Form
    {
        public helpForm(Form1 form1)
        {
            InitializeComponent();
            string command = form1.command;
            textBox1.Text = command;
        }
    }
}
