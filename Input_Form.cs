using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _3dQRA
{
    public partial class Input_Form : Form
    {
        public Input_Form()
        {
            InitializeComponent();
        }

        private void Input_Form_Load(object sender, EventArgs e)
        {
            tabPage1.Text = "地图";
            tabPage2.Text = "设备";
        }
    }
}
