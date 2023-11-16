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
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        /// <summary>  
        /// 设置透明按钮样式  
        /// </summary>  
        private void SetBtnStyle(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;//样式  
            btn.ForeColor = Color.Transparent;//前景  
            btn.BackColor = Color.Transparent;//去背景  
            btn.FlatAppearance.BorderSize = 0;//去边线  
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;//鼠标经过  
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;//鼠标按下  
        }
        private void btn_MouseHover(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            btn.FlatAppearance.BorderSize = 1;
        }
        private void btn_MouseLeave(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            btn.FlatAppearance.BorderSize = 0;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this .Width = (int)(0.86 * SystemInformation.WorkingArea.Width );
            this.Top = (int)(SystemInformation.WorkingArea.Height  *0.07);
            this.Height = (int)(SystemInformation.WorkingArea.Height * 0.86);
            this.Left = (int)(SystemInformation.WorkingArea.Height * 0.07);
            this.label1.Text  = "储运站场火灾爆燃事故三维定量风险评估软件";
            this.label1.Top = (int)(this.Height *0.3)-50;
            this.label1.Left = this.Width  / 2 - 450;
            this.label1.Font = new Font("楷书", 30, FontStyle.Bold); //第一个是字体，第二个大小，第三个是样式，
            this.label1.ForeColor = Color.Red; //颜色
            this.button1.Text = "进入程序";
            this.button1.Height = this.Height;
            this.button1.Width = this.Width;

            this.label2.Size=new Size(80,30);
            this.label2.Location  = new Point((int)(this.Width * 0.27), (int)(this.Height * 0.885));

            this.textBox1.Size = new Size((int)(this.Width * 0.1), 30);
            this.textBox1.Location = new Point((int)(this.Width * 0.3), (int)(this.Height * 0.88));

            this.label3.Size = new Size(80, 30);
            this.label3.Location = new Point((int)(this.Width * 0.47), (int)(this.Height * 0.885));

            this.textBox2.Size = new Size((int)(this.Width * 0.1), 30);
            this.textBox2.Location = new Point((int)(this.Width * 0.5), (int)(this.Height * 0.88));

            SetBtnStyle(button1);

            this.button1.ForeColor = Color.Yellow;
            this.button1.Size = new Size((int)(this.Width * 0.1), 30);
            this.button1.Location = new Point((int)(this.Width * 0.65), (int)(this.Height * 0.88));

        }

        private void button1_Click(object sender, EventArgs e)
        {
            /*如果帐号密码与预设一致，则进入界面*/
            if (textBox1.Text=="" || textBox2.Text=="")
            {
            Form2 Frm = new Form2();
            Frm.Show();
            this.Hide();
            }
        }
    }
}
