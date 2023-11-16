using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using _3dQRA.UI;


public struct ChoosenumTable
{
    public double X;
    public double Y;
    public double[] choosenum;
    public string selectedequip;
}

struct Equipment
{
    public double X;
    public double Y;
    public double Quantity;
    public string Type;
    public string Location;
    public double Tp;
    public double Tbp;
    public string State;
    public double Psat;
    public double Pi;
    public double CriticalValue;
    
    private double Delta()
    {
        double Delta;
        if (Tbp <= -25)
        { Delta = 0; }
        else if (Tbp >= -75 && Tbp < -25)
        { Delta = 1; }
        else if (Tbp >= -125 && Tbp < -75)
        { Delta = 2; }
        else
        { Delta = 3; }
        return Delta;
    }

    public double CommandNum()
    {
        double commandnum = 0;
        double Q1 = 0;
        double Q2 = 0;
        double Q3 = 0;
        if (Type == "工艺设备")
        { Q1 = 1; }
        else if (Type == "存储设备")
        { Q1 = 0.1; }
        else
        { Q1 = 0; }

        if (Location == "室外设备")
        { Q2 = 10; }
        else if (Location == "室内设备")
        { Q2 = 0.1; }
        else
        {
            if (Tp <= Tbp + 5)
            { Q2 = 0.1; }
            else
            { Q2 = 1.0; }
        }

        if (State == "气态")
        { Q3 = 10; }
        else if (State == "液态")
        {
            if (Psat >= 0.3)
            { Q3 = 10; }
            else if (Psat > 0.1 && Psat < 0.3)
            { Q3 = 45 * Psat - 3.5 + Delta(); }
            else
            { Q3 = 10 * Pi + Delta(); }
        }
        else
        { Q3 = 0.1; }
        commandnum = Quantity * Q1 * Q2 * Q3 / CriticalValue;
        return commandnum;
    }
};


struct BoundaryPoint
{
    public double X;
    public double Y;
};

struct InputAreaRectangle
{
    public double x1;
    public double y1;
    public double x2;
    public double y2;
    public double h1;
    public double h2;
    public double pz;

    public Boolean isPointInRectangle(double x3, double y3, double z3)
    {
        Boolean isIn = false;
        if (x3 >= x1 && x3 <= x2)
        {
            if (y3 >= y1 && y3 <= y2 || y3 >= y2 && y3 <= y1)
            {
                if (z3 >= h1 && z3 <= h2)
                {
                    isIn = true;
                }
            }
        }
        return isIn;
    }

    public Boolean isPointInRectangleWithoutZ(double x3, double y3)
    {
        Boolean isIn = false;
        if (x3 >= x1 && x3 <= x2)
        {
            if (y3 >= y1 && y3 <= y2 || y3 >= y2 && y3 <= y1)
            {
                isIn = true;
            }
        }
        return isIn;
    }
}

struct InputAreaCircle
{
    public double x1;
    public double y1;
    public double r;
    public double h1;
    public double h2;
    public double pz;

    public Boolean isPointInCircle(double x2, double y2, double z3)
    {
        Boolean isIn = false;
        if ((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1) <= r * r)
        {
            if (z3 >= h1 && z3 <= h2)
            {
                isIn = true;
            }
        }
        return isIn;
    }

    public Boolean isPointInCircleWithoutZ(double x2, double y2)
    {
        Boolean isIn = false;
        if ((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1) <= r * r)
        {
            isIn = true;
        }
        return isIn;
    }
}

struct FFile
{
    public string fileName;
    public int XNum;
    public int YNum;
    public int ZNum;
    public int totalSize;
    public Decimal bombTime;
    public List<Decimal> valueX_List;
    public List<Decimal> valueY_List;
    public List<Decimal> valueZ_List;
    public List<Decimal> value_List;
    public List<double> value_double_List;
};

namespace _3dQRA
{
    
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private double distance(BoundaryPoint point1, BoundaryPoint point2)
        {
            double distance;
            distance = Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) + (point1.Y - point2.Y) * (point1.Y - point2.Y));
            return distance;
        }
        
        private BoundaryPoint[] DivideBoundary()
        {
            int i;
            int j;
            int k;
            BoundaryPoint[] boundary = new BoundaryPoint[dataGridView2.RowCount];
            for (i = 0; i < dataGridView2.RowCount; i++)
            {
                boundary[i].X = double.Parse(dataGridView2.Rows[i].Cells[1].Value.ToString());
                boundary[i].Y = double.Parse(dataGridView2.Rows[i].Cells[2].Value.ToString());
            }
            BoundaryPoint point1, point2;
            BoundaryPoint[] divideboundary = new BoundaryPoint[Pointnum()];
            int a = 0;
            for (k = 0; k < dataGridView2.RowCount; k++)
            {
                if (k < dataGridView2.RowCount-1)
                {
                    point1 = boundary[k];
                    point2 = boundary[k + 1];
                }
                else
                {
                    point1 = boundary[dataGridView2.RowCount-1];
                    point2 = boundary[0];
                }
                i = (int)Math.Ceiling(distance(point1, point2) / 50);
                for (j = 0; j < i; j++)
                {
                    divideboundary[a].X = point1.X + (point2.X - point1.X) / i * j;
                    divideboundary[a++].Y = point1.Y + (point2.Y - point1.Y) / i * j;
                }
            }
            return divideboundary;
        }

        private Equipment[] renewequips() 
        {
            Equipment[] equips =new Equipment[dataGridView1.RowCount];
            int i;
            for(i=0; i<dataGridView1.RowCount;i++)
            { 
                equips[i].X=double.Parse(dataGridView1.Rows[i].Cells[2].Value.ToString());
                equips[i].Y= double.Parse(dataGridView1.Rows[i].Cells[3].Value.ToString());
                equips[i].Quantity= double.Parse(dataGridView1.Rows[i].Cells[4].Value.ToString ());
                equips[i].Type= dataGridView1.Rows[i].Cells[5].Value.ToString();
                equips[i].Location= dataGridView1.Rows[i].Cells[6].Value.ToString();
                equips[i].Tp= double.Parse(dataGridView1.Rows[i].Cells[7].Value.ToString());
                equips[i].Tbp= double.Parse(dataGridView1.Rows[i].Cells[8].Value.ToString());
                equips[i].State= dataGridView1.Rows[i].Cells[9].Value.ToString();
                equips[i].Psat= double.Parse(dataGridView1.Rows[i].Cells[10].Value.ToString());
                equips[i].Pi= double.Parse(dataGridView1.Rows[i].Cells[11].Value.ToString());
                equips[i].CriticalValue= double.Parse(dataGridView1.Rows[i].Cells[12].Value.ToString());
            }
            return equips;
        }

        private ChoosenumTable[] createchoosenumtable()
        {
            BoundaryPoint[]  divideboundary = DivideBoundary();
            Equipment[] equips =renewequips();
            ChoosenumTable[] choosenumtable = new ChoosenumTable[Pointnum()];
            int i;
            int j;
            double midnum;
            for(i=0; i < Pointnum(); i++)
            {
                choosenumtable[i].X = divideboundary[i].X;
                choosenumtable[i].Y = divideboundary[i].Y;
                choosenumtable[i].choosenum=new double[dataGridView1.RowCount] ;
                for (j=0; j < dataGridView1.RowCount; j++)
                    choosenumtable[i].choosenum[j] =choosenum(divideboundary[i],equips[j]);
                midnum = choosenumtable[i].choosenum.Max()/2;
                for (j = 0; j < dataGridView1.RowCount; j++)
                    if(choosenumtable[i].choosenum[j]>midnum)
                    choosenumtable[i].selectedequip += (j+1)+",";
                choosenumtable[i].selectedequip = choosenumtable[i].selectedequip.Substring(0, choosenumtable[i].selectedequip.Length - 1);
            }
            return choosenumtable;
        }

        private int Pointnum()
        {
            //经验证
            BoundaryPoint[] points = new BoundaryPoint[dataGridView2.RowCount];
            int i;
            for (i = 0; i < dataGridView2.RowCount; i++)
            {
                points[i].X=double.Parse(dataGridView2.Rows[i].Cells[1].Value.ToString());
                points[i].Y = double.Parse(dataGridView2.Rows[i].Cells[2].Value.ToString());
            }
           int Pointnum = 0;
           i = dataGridView2.RowCount-1;
            Pointnum = (int)(Math.Ceiling(distance(points[0],points[i])/50));
            for (;i>0;i--)
            Pointnum+= (int)(Math.Ceiling(distance(points[i],points[i-1])/50));
            return Pointnum;
        }

        private double choosenum(BoundaryPoint point1, Equipment equip)
        {
            double choosenum;
            BoundaryPoint point2;
            point2.X = equip.X;
            point2.Y = equip.Y;
            if (distance(point1, point2) >= 100)
            { 
                choosenum = 1000000 / distance(point1, point2) / distance(point1, point2) / distance(point1, point2) * equip.CommandNum();
            }
            else
            {
                choosenum = -1; 
            }
            return choosenum;
        }


        private void Form2_Load(object sender, EventArgs e)
        {
            View1Flag = false;
            View2Flag = false;
            View3Flag = false;
            View4Flag = false;
            View5Flag = false;
            EDEquipID = -1;
            EquipFrequent = 0.0;
            Pw = new List<double>();
            WindSpeed = 0.0;
            WindGrounpNum = 0;
            Pi = new List<double>();
            Pzr = new List<InputAreaRectangle>();
            Pzc = new List<InputAreaCircle>();

            this.toolBarLabel_DeviceNum.Text = "0";
            this.toolBarLabel_State.Text = "无操作";
            this.保存计算结果到ExcelToolStripMenuItem.Enabled = false;

            //(界面六)
            button7.Enabled = true;
            btn_View6_Calculate.Enabled = false;
            btn_View6_CheckValue.Enabled = false;
            btn_View6_Save_XLS.Enabled = false;
            btn_View6_Draw_Matlab.Enabled = true;

            //(界面三)
            panel_可燃气体瞬间释放.Hide();
            panel_可燃气体连续释放.Hide();
            panel_压缩液化气体瞬间释放.Hide();
            panel_压缩液化气体连续释放.Hide();
            panel_可燃液体释放.Hide();

            panel3.Size =new  Size(1300,560);
            panel3.Location = new Point(10,70);
            panel_可燃气体瞬间释放.Size = new Size(1200, 550);
            panel_可燃气体瞬间释放.Location = new Point(0, 70);
            panel_可燃气体连续释放.Size = new Size(1200, 550);
            panel_可燃气体连续释放.Location = new Point(0, 70);
            panel_压缩液化气体瞬间释放.Size = new Size(1200, 550);
            panel_压缩液化气体瞬间释放.Location = new Point(0, 70);
            panel_压缩液化气体连续释放.Size = new Size(1200, 550);
            panel_压缩液化气体连续释放.Location = new Point(0, 70);
            panel_可燃液体释放.Size = new Size(1200, 550);
            panel_可燃液体释放.Location = new Point(0, 70);
            pictureBox_可燃气体瞬间释放.Size = new Size(1068, 510);
            pictureBox_可燃气体瞬间释放.Location = new Point(0, 0);
            pictureBox_可燃气体连续释放.Size = new Size(1060, 391);
            pictureBox_可燃气体连续释放.Location = new Point(0, 0);
            pictureBox_压缩液化气体瞬间释放.Size = new Size(1178, 478);
            pictureBox_压缩液化气体瞬间释放.Location = new Point(0, 0);
            pictureBox_压缩液化气体连续释放.Size = new Size(1127, 428);
            pictureBox_压缩液化气体连续释放.Location = new Point(0, 0);
            pictureBox_可燃液体释放.Size = new Size(1097, 416);
            pictureBox_可燃液体释放.Location = new Point(0, 0);

            button_View3_TypeChoose.Size = new Size(100, 30);
            button_View3_TypeChoose.Location = new Point(490, 370);
            button10.Size = new Size(100, 30);
            button10.Location = new Point(1050, 500);
            button11.Size = new Size(100, 30);
            button11.Location = new Point(1050, 500);
            button12.Size = new Size(100, 30);
            button12.Location = new Point(1050, 500);
            button13.Size = new Size(100, 30);
            button13.Location = new Point(1050, 500);
            button14.Size = new Size(100, 30);
            button14.Location = new Point(1050, 500);
            button_可燃气体瞬间释放_OK.Size = new Size(100, 30);
            button_可燃气体瞬间释放_OK.Location = new Point(1050, 450);
            button_可燃气体连续释放_OK.Size = new Size(100, 30);
            button_可燃气体连续释放_OK.Location = new Point(1050, 450);
            button_压缩液化气体瞬间释放_OK.Size = new Size(100, 30);
            button_压缩液化气体瞬间释放_OK.Location = new Point(1050, 450);
            button_压缩液化气体连续释放_OK.Size = new Size(100, 30);
            button_压缩液化气体连续释放_OK.Location = new Point(1050, 450);
            button_可燃液体释放_OK.Size = new Size(100, 30);
            button_可燃液体释放_OK.Location = new Point(1050, 450);
            button_可燃气体瞬间释放_OK.Text = "计算概率";
            button_可燃气体连续释放_OK.Text = "计算概率";
            button_压缩液化气体瞬间释放_OK.Text = "计算概率";
            button_压缩液化气体连续释放_OK.Text = "计算概率";
            button_可燃液体释放_OK.Text = "计算概率";

            radioButton3.Location = new Point(470,80);
            radioButton4.Location = new Point(470, 130);
            radioButton5.Location = new Point(470, 180);
            radioButton6.Location = new Point(470, 230);
            radioButton7.Location = new Point(470, 280);

            textBox1_可燃气体瞬间释放.Location = new Point(165, 180);
            textBox2_可燃气体瞬间释放.Location = new Point(355, 70);
            textBox3_可燃气体瞬间释放.Location = new Point(360, 430);
            textBox4_可燃气体瞬间释放.Location = new Point(660, 30);
            textBox5_可燃气体瞬间释放.Location = new Point(760, 112);
            textBox6_可燃气体瞬间释放.Location = new Point(760, 200);
            textBox7_可燃气体瞬间释放.Location = new Point(760, 287);
            textBox8_可燃气体瞬间释放.Location = new Point(760, 372);
            textBox9_可燃气体瞬间释放.Location = new Point(955, 45);
            textBox10_可燃气体瞬间释放.Location = new Point(955, 135);
            textBox11_可燃气体瞬间释放.Location = new Point(955, 225);
            textBox12_可燃气体瞬间释放.Location = new Point(955, 305);
            textBox13_可燃气体瞬间释放.Location = new Point(955, 390);
            textBox14_可燃气体瞬间释放.Location = new Point(565, 330);

            textBox1_可燃气体连续释放.Location = new Point(185, 115);
            textBox2_可燃气体连续释放.Location = new Point(355, 20);
            textBox3_可燃气体连续释放.Location = new Point(355, 280);
            textBox4_可燃气体连续释放.Location = new Point(735, 125);
            textBox5_可燃气体连续释放.Location = new Point(735, 214);
            textBox6_可燃气体连续释放.Location = new Point(970, 40);
            textBox7_可燃气体连续释放.Location = new Point(970, 150);
            textBox8_可燃气体连续释放.Location = new Point(970, 235);
            textBox9_可燃气体连续释放.Location = new Point(535, 165);

            textBox1_压缩气体瞬间释放.Location = new Point(195, 165);
            textBox2_压缩气体瞬间释放.Location = new Point(380, 65);
            textBox3_压缩气体瞬间释放.Location = new Point(380, 395);
            textBox4_压缩气体瞬间释放.Location = new Point(670, 25);
            textBox5_压缩气体瞬间释放.Location = new Point(730, 105);
            textBox6_压缩气体瞬间释放.Location = new Point(730, 188);
            textBox7_压缩气体瞬间释放.Location = new Point(730, 265);
            textBox8_压缩气体瞬间释放.Location = new Point(730, 348);
            textBox9_压缩气体瞬间释放.Location = new Point(1055, 45);
            textBox10_压缩气体瞬间释放.Location = new Point(1055, 125);
            textBox11_压缩气体瞬间释放.Location = new Point(1055, 205);
            textBox12_压缩气体瞬间释放.Location = new Point(1055, 280);
            textBox13_压缩气体瞬间释放.Location = new Point(1055, 365);
            textBox14_压缩气体瞬间释放.Location = new Point(545, 310);

            textBox1_压缩气体连续释放.Location = new Point(190, 145);
            textBox2_压缩气体连续释放.Location = new Point(370, 25);
            textBox3_压缩气体连续释放.Location = new Point(350, 325);
            textBox4_压缩气体连续释放.Location = new Point(735, 145);
            textBox5_压缩气体连续释放.Location = new Point(735, 250);
            textBox6_压缩气体连续释放.Location = new Point(1070, 45);
            textBox7_压缩气体连续释放.Location = new Point(1070, 170);
            textBox8_压缩气体连续释放.Location = new Point(1070, 270);
            textBox9_压缩气体连续释放.Location = new Point(540, 205);

            textBox1_可燃液体释放.Location = new Point(165, 145);
            textBox2_可燃液体释放.Location = new Point(350, 35);
            textBox3_可燃液体释放.Location = new Point(350, 320);
            textBox4_可燃液体释放.Location = new Point(735, 150);
            textBox5_可燃液体释放.Location = new Point(735, 240);
            textBox6_可燃液体释放.Location = new Point(1090, 60);
            textBox7_可燃液体释放.Location = new Point(1090, 170);
            textBox8_可燃液体释放.Location = new Point(1090, 265);
            textBox9_可燃液体释放.Location = new Point(545, 200);

            //Form1 Frm = new Form1();
            // Frm.Show();
            //Frm .Dispose();
            textBox1.Text = "1";
            textBox2.Text = "原油储罐";
            textBox3.Text = "0.0";
            textBox4.Text = "0.0";
            textBox5.Text = "0.0";
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 1;
            textBox6.Text = "0.0";
            textBox7.Text = "0.0";
            textBox8.Text = "0";
            textBox9.Text = "0";
            textBox10.Text = "10000";
            textBox11.Text = "无";

            
            this.Width = (int)(0.86 * SystemInformation.WorkingArea.Width);
            this.Top = (int)(SystemInformation.WorkingArea.Height * 0.07);
            this.Height = (int)(SystemInformation.WorkingArea.Height * 0.86);
            this.Left = (int)(SystemInformation.WorkingArea.Height * 0.07);
            this.tabControl1.TabPages[0].Text = "站场边界输入";
            this.tabControl1.TabPages[1].Text = "站场设备输入";
            this.tabControl1.TabPages[2].Text = "泄漏事故演化及概率";
            this.tabControl1.TabPages[3].Text = "风向和风速分布";
            this.tabControl1.TabPages[4].Text = "站场人员分布";
            this.tabControl1.TabPages[5].Text = "个人风险计算";

            //(界面五)
            this.btn_View4_Check.Visible = false;
            this.btn_View4_Check.Enabled = false;
            DataGridViewRow dr5 = new DataGridViewRow();
            dr5.CreateCells(dataGridView5);
            dr5.Cells[0].Value = "矩形区域1";
            dr5.Cells[1].Value = 72;
            dr5.Cells[2].Value = 0;
            dr5.Cells[3].Value = 77;
            dr5.Cells[4].Value = 150;
            dr5.Cells[5].Value = 0.1;
            dr5.Cells[6].Value = 1.7;
            dr5.Cells[7].Value = 5;
            dataGridView5.Rows.Insert(0, dr5);

            dr5 = new DataGridViewRow();
            dr5.CreateCells(dataGridView5);
            dr5.Cells[0].Value = "矩形区域2";
            dr5.Cells[1].Value = 22;
            dr5.Cells[2].Value = 12;
            dr5.Cells[3].Value = 72;
            dr5.Cells[4].Value = 8;
            dr5.Cells[5].Value = 0.1;
            dr5.Cells[6].Value = 1.7;
            dr5.Cells[7].Value = 5;
            dataGridView5.Rows.Insert(1, dr5);

            DataGridViewRow dr6 = new DataGridViewRow();
            dr6.CreateCells(dataGridView6);
            dr6.Cells[0].Value = "圆形区域";
            dr6.Cells[1].Value = 60;
            dr6.Cells[2].Value = 55;
            dr6.Cells[3].Value = 5;
            dr6.Cells[4].Value = 4.1;
            dr6.Cells[5].Value = 5.8;
            dr6.Cells[6].Value = 6;
            dataGridView6.Rows.Insert(0, dr6);

            //(界面四)
            button5.Visible = true;
            button6.Visible = false;
            //(界面四)
            DataGridViewRow dr1 = new DataGridViewRow();
            dr1.CreateCells(dataGridView4);
            dr1.Cells[0].Value = 1.7;
            dr1.Cells[1].Value = 4.7;
            dr1.Cells[2].Value = 5.2;
            dr1.Cells[3].Value = 3.6;
            dr1.Cells[4].Value = 2.2;
            dr1.Cells[5].Value = 2.2;
            dr1.Cells[6].Value = 7.7;
            dr1.Cells[7].Value = 3.6;
            dr1.Cells[8].Value = 2.5;
            dr1.Cells[9].Value = 1.1;
            dr1.Cells[10].Value = 1.1;
            dr1.Cells[11].Value = 1.6;
            dr1.Cells[12].Value = 1.9;
            dr1.Cells[13].Value = 13.7;
            dr1.Cells[14].Value = 12.9;
            dr1.Cells[15].Value = 13.4;
            dr1.Cells[16].Value = 9.9;
            dataGridView4.Rows.Insert(0, dr1);

            dr1 = new DataGridViewRow();
            dr1.CreateCells(dataGridView4);
            dr1.Cells[0].Value = 2.3;
            dr1.Cells[1].Value = 5.8;
            dr1.Cells[2].Value = 4.9;
            dr1.Cells[3].Value = 2.2;
            dr1.Cells[4].Value = 3.0;
            dr1.Cells[5].Value = 7.1;
            dr1.Cells[6].Value = 19.2;
            dr1.Cells[7].Value = 11.2;
            dr1.Cells[8].Value = 5.2;
            dr1.Cells[9].Value = 3.6;
            dr1.Cells[10].Value = 2.5;
            dr1.Cells[11].Value = 1.6;
            dr1.Cells[12].Value = 2.7;
            dr1.Cells[13].Value = 9.6;
            dr1.Cells[14].Value = 3.8;
            dr1.Cells[15].Value = 7.9;
            dr1.Cells[16].Value = 8.8;
            dataGridView4.Rows.Insert(1, dr1);

            dr1 = new DataGridViewRow();
            dr1.CreateCells(dataGridView4);
            dr1.Cells[0].Value = 2.5;
            dr1.Cells[1].Value = 4.1;
            dr1.Cells[2].Value = 4.9;
            dr1.Cells[3].Value = 1.6;
            dr1.Cells[4].Value = 2.2;
            dr1.Cells[5].Value = 5.2;
            dr1.Cells[6].Value = 20.9;
            dr1.Cells[7].Value = 17.0;
            dr1.Cells[8].Value = 7.1;
            dr1.Cells[9].Value = 7.7;
            dr1.Cells[10].Value = 3.6;
            dr1.Cells[11].Value = 0.5;
            dr1.Cells[12].Value = 1.1;
            dr1.Cells[13].Value = 5.2;
            dr1.Cells[14].Value = 3.6;
            dr1.Cells[15].Value = 7.1;
            dr1.Cells[16].Value = 6.6;
            dataGridView4.Rows.Insert(2, dr1);

            dr1 = new DataGridViewRow();
            dr1.CreateCells(dataGridView4);
            dr1.Cells[0].Value = 3.2;
            dr1.Cells[1].Value = 4.7;
            dr1.Cells[2].Value = 2.7;
            dr1.Cells[3].Value = 3.0;
            dr1.Cells[4].Value = 1.4;
            dr1.Cells[5].Value = 6.0;
            dr1.Cells[6].Value = 19.2;
            dr1.Cells[7].Value = 18.1;
            dr1.Cells[8].Value = 10.1;
            dr1.Cells[9].Value = 7.1;
            dr1.Cells[10].Value = 3.3;
            dr1.Cells[11].Value = 0.3;
            dr1.Cells[12].Value = 0.3;
            dr1.Cells[13].Value = 3.6;
            dr1.Cells[14].Value = 3.6;
            dr1.Cells[15].Value = 6.8;
            dr1.Cells[16].Value = 7.1;
            dataGridView4.Rows.Insert(3, dr1);

            dr1 = new DataGridViewRow();
            dr1.CreateCells(dataGridView4);
            dr1.Cells[0].Value = 5.2;
            dr1.Cells[1].Value = 6.3;
            dr1.Cells[2].Value = 3.6;
            dr1.Cells[3].Value = 1.1;
            dr1.Cells[4].Value = 3.6;
            dr1.Cells[5].Value = 4.9;
            dr1.Cells[6].Value = 15.8;
            dr1.Cells[7].Value = 5.5;
            dr1.Cells[8].Value = 5.5;
            dr1.Cells[9].Value = 3.8;
            dr1.Cells[10].Value = 3.0;
            dr1.Cells[11].Value = 0.8;
            dr1.Cells[12].Value = 1.6;
            dr1.Cells[13].Value = 5.5;
            dr1.Cells[14].Value = 5.5;
            dr1.Cells[15].Value = 10.1;
            dr1.Cells[16].Value = 7.1;
            dataGridView4.Rows.Insert(4, dr1);

            dr1 = new DataGridViewRow();
            dr1.CreateCells(dataGridView4);
            dr1.Cells[0].Value = 7.5;
            dr1.Cells[1].Value = 5.0;
            dr1.Cells[2].Value = 3.0;
            dr1.Cells[3].Value = 1.7;
            dr1.Cells[4].Value = 0.8;
            dr1.Cells[5].Value = 3.6;
            dr1.Cells[6].Value = 8.0;
            dr1.Cells[7].Value = 3.9;
            dr1.Cells[8].Value = 3.6;
            dr1.Cells[9].Value = 3.9;
            dr1.Cells[10].Value = 1.4;
            dr1.Cells[11].Value = 0;
            dr1.Cells[12].Value = 0.8;
            dr1.Cells[13].Value = 7.5;
            dr1.Cells[14].Value = 19.3;
            dr1.Cells[15].Value = 13.8;
            dr1.Cells[16].Value = 7.2;
            dataGridView4.Rows.Insert(5, dr1);

            dr1 = new DataGridViewRow();
            dr1.CreateCells(dataGridView4);
            dr1.Cells[0].Value = 8.5;
            dr1.Cells[1].Value = 4.4;
            dr1.Cells[2].Value = 3.0;
            dr1.Cells[3].Value = 1.4;
            dr1.Cells[4].Value = 1.6;
            dr1.Cells[5].Value = 0.8;
            dr1.Cells[6].Value = 5.5;
            dr1.Cells[7].Value = 2.7;
            dr1.Cells[8].Value = 1.6;
            dr1.Cells[9].Value = 0.8;
            dr1.Cells[10].Value = 1.1;
            dr1.Cells[11].Value = 0.5;
            dr1.Cells[12].Value = 0.5;
            dr1.Cells[13].Value = 11.2;
            dr1.Cells[14].Value = 23.8;
            dr1.Cells[15].Value = 17.8;
            dr1.Cells[16].Value = 6.0;
            dataGridView4.Rows.Insert(6, dr1);

            dr1 = new DataGridViewRow();
            dr1.CreateCells(dataGridView4);
            dr1.Cells[0].Value = 10.5;
            dr1.Cells[1].Value = 3.8;
            dr1.Cells[2].Value = 3.6;
            dr1.Cells[3].Value = 1.9;
            dr1.Cells[4].Value = 1.9;
            dr1.Cells[5].Value = 1.6;
            dr1.Cells[6].Value = 4.1;
            dr1.Cells[7].Value = 1.6;
            dr1.Cells[8].Value = 0.3;
            dr1.Cells[9].Value = 1.1;
            dr1.Cells[10].Value = 0.8;
            dr1.Cells[11].Value = 0;
            dr1.Cells[12].Value = 1.1;
            dr1.Cells[13].Value = 10.7;
            dr1.Cells[14].Value = 26.0;
            dr1.Cells[15].Value = 17.0;
            dr1.Cells[16].Value = 6.0;
            dataGridView4.Rows.Insert(7, dr1);
            
            //(界面六)
            npData = new List<FFile>();

            //(界面二)
            DataGridViewRow dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 1;
            dr.Cells[1].Value = "1#加热炉";
            dr.Cells[2].Value = 27.5;
            dr.Cells[3].Value = 20.5;
            dr.Cells[4].Value = 10973;
            dr.Cells[5].Value = "工艺设备";
            dr.Cells[6].Value= "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 110;
            dr.Cells[9].Value = "液态";
            dr.Cells[10].Value = 1;
            dr.Cells[11].Value = 0.024;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(0, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 2;
            dr.Cells[1].Value = "2#加热炉";
            dr.Cells[2].Value = 27.5;
            dr.Cells[3].Value = 25.5;
            dr.Cells[4].Value = 10973;
            dr.Cells[5].Value = "工艺设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 110;
            dr.Cells[9].Value = "液态";
            dr.Cells[10].Value = 1;
            dr.Cells[11].Value = 0.024;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(1, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 3;
            dr.Cells[1].Value = "3#加热炉";
            dr.Cells[2].Value = 27.5;
            dr.Cells[3].Value = 30.5;
            dr.Cells[4].Value = 10973;
            dr.Cells[5].Value = "工艺设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 110;
            dr.Cells[9].Value = "液态";
            dr.Cells[10].Value = 1;
            dr.Cells[11].Value = 0.024;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(2, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 4;
            dr.Cells[1].Value = "4#加热炉";
            dr.Cells[2].Value = 27.5;
            dr.Cells[3].Value = 35.5;
            dr.Cells[4].Value = 10973;
            dr.Cells[5].Value = "存储设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 110;
            dr.Cells[9].Value = "液态";
            dr.Cells[10].Value = 1;
            dr.Cells[11].Value = 0.024;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(3, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 5;
            dr.Cells[1].Value = "1#三相分离器";
            dr.Cells[2].Value = 34.5;
            dr.Cells[3].Value = 66.5;
            dr.Cells[4].Value = 3000;
            dr.Cells[5].Value = "工艺设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 0;
            dr.Cells[9].Value = "气态";
            dr.Cells[10].Value = 0;
            dr.Cells[11].Value = 0;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "危害物质只考虑天然气";
            dataGridView1.Rows.Insert(4, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 6;
            dr.Cells[1].Value = "2#三相分离器";
            dr.Cells[2].Value = 34.5;
            dr.Cells[3].Value = 72.5;
            dr.Cells[4].Value = 3000;
            dr.Cells[5].Value = "工艺设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 0;
            dr.Cells[9].Value = "气态";
            dr.Cells[10].Value = 0;
            dr.Cells[11].Value = 0;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "物质只考虑天然气";
            dataGridView1.Rows.Insert(5, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 7;
            dr.Cells[1].Value = "3#三相分离器";
            dr.Cells[2].Value = 27.5;
            dr.Cells[3].Value = 78.5;
            dr.Cells[4].Value = 3000;
            dr.Cells[5].Value = "工艺设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 0;
            dr.Cells[9].Value = "气态";
            dr.Cells[10].Value = 0;
            dr.Cells[11].Value = 0;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "物质只考虑天然气";
            dataGridView1.Rows.Insert(6, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 8;
            dr.Cells[1].Value = "原油稳定塔";
            dr.Cells[2].Value = 60;
            dr.Cells[3].Value = 55;
            dr.Cells[4].Value = 132.21;
            dr.Cells[5].Value = "工艺设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 0;
            dr.Cells[9].Value = "气态";
            dr.Cells[10].Value = 0;
            dr.Cells[11].Value = 0;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(7, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 9;
            dr.Cells[1].Value = "负压分离器";
            dr.Cells[2].Value = 58;
            dr.Cells[3].Value = 44;
            dr.Cells[4].Value = 3000;
            dr.Cells[5].Value = "工艺设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 0;
            dr.Cells[9].Value = "气态";
            dr.Cells[10].Value = 0;
            dr.Cells[11].Value = 0;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(8, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 10;
            dr.Cells[1].Value = "低压分离器";
            dr.Cells[2].Value = 45;
            dr.Cells[3].Value = 46;
            dr.Cells[4].Value = 3000;
            dr.Cells[5].Value = "工艺设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 0;
            dr.Cells[9].Value = "气态";
            dr.Cells[10].Value = 0;
            dr.Cells[11].Value = 0;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(9, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 11;
            dr.Cells[1].Value = "中压分离器";
            dr.Cells[2].Value = 58;
            dr.Cells[3].Value = 42;
            dr.Cells[4].Value = 3000;
            dr.Cells[5].Value = "工艺设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 0;
            dr.Cells[9].Value = "气态";
            dr.Cells[10].Value = 0;
            dr.Cells[11].Value = 0;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(10, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 12;
            dr.Cells[1].Value = "1#换热器";
            dr.Cells[2].Value = 53;
            dr.Cells[3].Value = 45;
            dr.Cells[4].Value = 3000;
            dr.Cells[5].Value = "工艺设备";
            dr.Cells[6].Value = "室内设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 0;
            dr.Cells[9].Value = "气态";
            dr.Cells[10].Value = 0;
            dr.Cells[11].Value = 0;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(11, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 13;
            dr.Cells[1].Value = "2#换热器";
            dr.Cells[2].Value = 51.5;
            dr.Cells[3].Value = 44;
            dr.Cells[4].Value = 3000;
            dr.Cells[5].Value = "工艺设备";
            dr.Cells[6].Value = "室内设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 0;
            dr.Cells[9].Value = "气态";
            dr.Cells[10].Value = 0;
            dr.Cells[11].Value = 0;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(12, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 14;
            dr.Cells[1].Value = "3#换热器";
            dr.Cells[2].Value = 50;
            dr.Cells[3].Value = 44;
            dr.Cells[4].Value = 3000;
            dr.Cells[5].Value = "工艺设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 0;
            dr.Cells[9].Value = "气态";
            dr.Cells[10].Value = 0;
            dr.Cells[11].Value = 0;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(13, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 15;
            dr.Cells[1].Value = "1#原油储罐";
            dr.Cells[2].Value = 115;
            dr.Cells[3].Value = 72;
            dr.Cells[4].Value = 2960000;
            dr.Cells[5].Value = "存储设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 110;
            dr.Cells[9].Value = "液态";
            dr.Cells[10].Value = 0.11;
            dr.Cells[11].Value = 0;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(14, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 16;
            dr.Cells[1].Value = "2#原油储罐";
            dr.Cells[2].Value = 115;
            dr.Cells[3].Value = 100;
            dr.Cells[4].Value = 2960000;
            dr.Cells[5].Value = "存储设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 110;
            dr.Cells[9].Value = "液态";
            dr.Cells[10].Value = 0.11;
            dr.Cells[11].Value = 0;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(15, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = 17;
            dr.Cells[1].Value = "3#原油储罐";
            dr.Cells[2].Value = 115;
            dr.Cells[3].Value = 128;
            dr.Cells[4].Value = 2960000;
            dr.Cells[5].Value = "存储设备";
            dr.Cells[6].Value = "室外设备";
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 110;
            dr.Cells[9].Value = "液态";
            dr.Cells[10].Value = 0.11;
            dr.Cells[11].Value = 0;
            dr.Cells[12].Value = 10000;
            dr.Cells[13].Value = "无";
            dataGridView1.Rows.Insert(16, dr);


            //(界面一)
            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 1;
            dr.Cells[1].Value = 10;
            dr.Cells[2].Value = 0;
            dataGridView2.Rows.Insert(0, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 2;
            dr.Cells[1].Value = 10;
            dr.Cells[2].Value = 150;
            dataGridView2.Rows.Insert(1, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 3;
            dr.Cells[1].Value = 140;
            dr.Cells[2].Value = 150;
            dataGridView2.Rows.Insert(2, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 4;
            dr.Cells[1].Value = 140;
            dr.Cells[2].Value = 0;
            dataGridView2.Rows.Insert(3, dr);

            /*
            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 1;
            dr.Cells[1].Value = 0;
            dr.Cells[2].Value = 0;
            dataGridView2.Rows.Insert(0, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 2;
            dr.Cells[1].Value = 710;
            dr.Cells[2].Value = 0;
            dataGridView2.Rows.Insert(1, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 3;
            dr.Cells[1].Value = 710.25;
            dr.Cells[2].Value = 297;
            dataGridView2.Rows.Insert(2, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 4;
            dr.Cells[1].Value = 908.776;
            dr.Cells[2].Value = 297.088;
            dataGridView2.Rows.Insert(3, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 5;
            dr.Cells[1].Value = 908.776;
            dr.Cells[2].Value = 384;
            dataGridView2.Rows.Insert(4, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 6;
            dr.Cells[1].Value = 959.1;
            dr.Cells[2].Value = 384.06;
            dataGridView2.Rows.Insert(5, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 7;
            dr.Cells[1].Value = 959.1;
            dr.Cells[2].Value = 467.56;
            dataGridView2.Rows.Insert(6, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 8;
            dr.Cells[1].Value = 918.916;
            dr.Cells[2].Value = 467.56;
            dataGridView2.Rows.Insert(7, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 9;
            dr.Cells[1].Value = 920;
            dr.Cells[2].Value = 600;
            dataGridView2.Rows.Insert(8, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 10;
            dr.Cells[1].Value = 829;
            dr.Cells[2].Value = 600;
            dataGridView2.Rows.Insert(9, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 11;
            dr.Cells[1].Value = 828;
            dr.Cells[2].Value = 734.76;
            dataGridView2.Rows.Insert(10, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 12;
            dr.Cells[1].Value = 365;
            dr.Cells[2].Value = 734.76;
            dataGridView2.Rows.Insert(11, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 13;
            dr.Cells[1].Value = 365;
            dr.Cells[2].Value = 622;
            dataGridView2.Rows.Insert(12, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 14;
            dr.Cells[1].Value = 270;
            dr.Cells[2].Value = 622;
            dataGridView2.Rows.Insert(13, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 15;
            dr.Cells[1].Value = 270;
            dr.Cells[2].Value = 297;
            dataGridView2.Rows.Insert(14, dr);

            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = 16;
            dr.Cells[1].Value = 0;
            dr.Cells[2].Value = 297;
            dataGridView2.Rows.Insert(15, dr);
             */
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
            System.Environment.Exit(0);
        }

        //表1 压力容器的LOC情景
        private void 表1压力容器的LOC情景toolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form4 Frm = new Form4();
            Frm.Show();
        }

        //表2 过程设备的LOC情景
        private void 不同安装位置的Q2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form5 Frm = new Form5();
            Frm.Show();
        }

        //表3 单层密封常压储罐的LOC情景
        private void 表3工艺条件系数Q3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form6 Frm = new Form6();
            Frm.Show();
        }

        //表4 双层密封常压储罐的LOC情景
        private void 表4液池蒸发的增值ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form7 Frm = new Form7();
            Frm.Show();
        }

        //表5 管道的LOC情景
        private void 表5管道的LOC情景ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form11 Frm = new Form11();
            Frm.Show();
        }

        //表6 泵的LOC情景
        private void 表6泵的LOC情景ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form12 Frm = new Form12();
            Frm.Show();
        }

        //表7 换热器的LOC情景
        private void 表7换热器的LOC情景ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form13 Frm = new Form13();
            Frm.Show();
        }

        //表9固定装置可燃物质泄漏后立即点火概率
        private void 表9固定装置可燃物质泄漏后立即点火概率ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form14 Frm = new Form14();
            Frm.Show();
        }
        
        //固定装置可燃物质泄漏后立即点火概率
        private void 固定装置可燃物质泄漏后立即点火概率ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form14 Frm = new Form14();
            Frm.Show();
        }

        //表10企业内运输设备可燃物质泄漏后立即点火概率
        private void 表10企业内运输设备可燃物质泄漏后立即点火概率ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form15 Frm = new Form15();
            Frm.Show();
        }

        //企业内运输设备可燃物质泄漏后立即点火概率
        private void 企业内运输设备可燃物质泄漏后立即点火概率ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form15 Frm = new Form15();
            Frm.Show();
        }

        //表11点火源在1Min内的点火概率ToolStripMenuItem_Click
        private void 表11点火源在1Min内的点火概率ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form16 Frm = new Form16();
            Frm.Show();
        }

        //点火源在1min内的点火概率
        private void 点火源在1min内的点火概率ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form16 Frm = new Form16();
            Frm.Show();
        }

        //公式1延迟点火的点火概率ToolStripMenuItem_Click
        private void 公式1延迟点火的点火概率ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form17 Frm = new Form17();
            Frm.Show();
        }

        //延迟点火的点火概率
        private void 延迟点火的点火概率ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form17 Frm = new Form17();
            Frm.Show();
        }


        //(界面二)添加设备
        private void button1_Click(object sender, EventArgs e)
        {
            DataGridViewRow dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = textBox1.Text;
            dr.Cells[1].Value = textBox2.Text;
            dr.Cells[2].Value = textBox3.Text;
            dr.Cells[3].Value = textBox4.Text;
            dr.Cells[4].Value = textBox5.Text;
            dr.Cells[5].Value = comboBox1.Text;
            dr.Cells[6].Value = comboBox2.Text;
            dr.Cells[7].Value = textBox6.Text;
            dr.Cells[8].Value = textBox7.Text;
            dr.Cells[9].Value = comboBox3.Text;
            dr.Cells[10].Value = textBox8.Text;
            dr.Cells[11].Value = textBox9.Text;
            dr.Cells[12].Value = textBox10.Text;
            dr.Cells[13].Value = textBox11.Text;

            if (dataGridView1.SelectedRows.Count != 1)
            {
                dataGridView1.Rows.Insert(dataGridView1.RowCount, dr);
                textBox1.Text = (dataGridView1.RowCount+2).ToString();
            }
            else
            { 
                dataGridView1.Rows.Insert(dataGridView1.SelectedRows[0].Index, dr);
                textBox1.Text = (dataGridView1.RowCount+2).ToString();
            }

            for (int i = 1; i < dataGridView1.RowCount + 1; i++)
                dataGridView1.Rows[i - 1].Cells[0].Value = i;
        }

        //(界面二)清空列表
        private void button_View2_清空_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count > 0)
                dataGridView1.Rows.Clear();
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            //已完成
            if (textBox8.Text == "")
                textBox8.Text = "0.0";
            if (comboBox3.SelectedIndex != 1)
            {
                textBox8.Enabled = false;
                textBox9.Enabled = false;
                if (comboBox2.SelectedIndex==2)
                    textBox7.Enabled=true;
                else
                    textBox7.Enabled=false;
            }
            if (comboBox3.SelectedIndex == 1)
            {
                textBox8.Enabled = true;
                textBox9.Enabled = false;
                if (double.Parse(textBox8.Text) <= 0.1)
                {
                    textBox9.Enabled = true;
                }
                if (double.Parse(textBox8.Text) < 0.3)
                {
                    textBox7.Enabled = true;
                }
            }

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex != 2)
            {
                textBox6.Enabled = false;
                textBox7.Enabled = false;
                if (comboBox3.Text == "液态" && Double.Parse(textBox8.Text)<0.3)
                textBox7.Enabled = true;
            }
            if (comboBox2.SelectedIndex == 2)
            {
                textBox6.Enabled = true;
                textBox7.Enabled = true;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Regex Matches = new Regex(@"^([1-9][0-9]*|0)\.?[0-9]*$");
            if (!Matches.IsMatch(textBox1.Text) && textBox1.Text != "")
            {
                textBox1.Text = textBox1.Text.Substring(0, textBox1.Text.Length - 1);
            }
            textBox1.Text = dataGridView1.RowCount.ToString();
        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (textBox3.Text == "")
                textBox3.Text = "0.0";
            Regex Matches = new Regex(@"^([1-9][0-9]*|0)\.?[0-9]*$");
            if (!Matches.IsMatch(textBox3.Text) && textBox3.Text != "")
            {
                textBox3.Text = textBox3.Text.Substring(0, textBox3.Text.Length - 1);
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            Regex Matches=new Regex(@"^([1-9][0-9]*|0)\.?[0-9]*$");
            if (!Matches.IsMatch(textBox4.Text) && textBox4.Text!="")
            {
                textBox4.Text = textBox4.Text.Substring(0, textBox4.Text.Length - 1);
            }
            if (textBox4.Text == "")
                textBox4.Text = "0.0";
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            if (textBox5.Text == "")
                textBox5.Text = "0";
           // Regex Matches = new Regex(@"^[1-9][0-9]*$");
            //if (!Matches.IsMatch(textBox5.Text) && textBox5.Text != "")
            //{
              //  textBox5.Text = textBox5.Text.Substring(0, textBox5.Text.Length - 1);
           // }
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            if (textBox6.Text == "")
            { textBox6.Text = "0.0"; }
            if (textBox6.Text == "-")
            { textBox6.Text = "-0"; }
            Regex Matches = new Regex(@"^-?([1-9][0-9]*|0)\.?[0-9]*$");
            if (!Matches.IsMatch(textBox6.Text) && textBox6.Text != "")
            {
                textBox6.Text = textBox6.Text.Substring(0, textBox6.Text.Length - 1);
            }

        }
        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            if (textBox7.Text == "")
            { textBox7.Text = "0.0"; }
            if (textBox7.Text == "-")
            { textBox7.Text = "-0"; }
            Regex Matches = new Regex(@"^-?([1-9][0-9]*|0)\.?[0-9]*$");
            if (!Matches.IsMatch(textBox7.Text) && textBox7.Text != "")
            {
                textBox7.Text = textBox7.Text.Substring(0, textBox7.Text.Length - 1);
            }
        }
        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            if (textBox8.Text == "")
                textBox8.Text = "0.0";
            if (textBox8.Text == "-")
            { textBox8.Text = "-0"; }
            Regex Matches = new Regex(@"^([1-9][0-9]*|0)\.?[0-9]*$");
            if (!Matches.IsMatch(textBox8.Text) && textBox8.Text != "")
            {
                textBox8.Text = textBox8.Text.Substring(0, textBox8.Text.Length - 1);
            }
            //Tbp的问题
            if (double.Parse(textBox8.Text) < 0.3)
            {
                textBox7.Enabled = true;
            }
            else
            {
                  if(comboBox2.SelectedIndex != 2)
                textBox7.Enabled = false;
                else
                {
                    textBox7.Enabled = true;
                }
            }
            //Pi的问题
            if (double.Parse(textBox8.Text) > 0.1)
                textBox9.Enabled = false;
            else
                textBox9.Enabled = true;
        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            if (textBox9.Text == "")
                textBox9.Text = "0.0";
            Regex Matches = new Regex(@"^([1-9][0-9]*|0)\.?[0-9]*$");
            if (!Matches.IsMatch(textBox9.Text) && textBox9.Text != "")
            {
                textBox9.Text = textBox9.Text.Substring(0, textBox9.Text.Length - 1);
            }
        }
        private void textBox10_TextChanged(object sender, EventArgs e)
        {
            Regex Matches = new Regex(@"^([1-9][0-9]*|0)\.?[0-9]*$");
            if (!Matches.IsMatch(textBox10.Text) && textBox10.Text != "")
            {
                textBox10.Text = textBox10.Text.Substring(0, textBox10.Text.Length - 1);
            }
            if (textBox10.Text == "")
                textBox10.Text = "0.0";
        }


        //(界面一)工厂添加边界点
        private void button2_Click(object sender, EventArgs e)
        {
            DataGridViewRow dr = new DataGridViewRow();
            dr.CreateCells(dataGridView2);
            dr.Cells[0].Value = dataGridView2.RowCount + 1;
            dr.Cells[1].Value = 0.0;
            dr.Cells[2].Value = 0.0;

            if (dataGridView2.SelectedRows.Count != 1)
            {    
                dataGridView2.Rows.Insert(dataGridView2.RowCount, dr);
            }
            else
            {
                dataGridView2.Rows.Insert(dataGridView2.SelectedRows[0].Index, dr);
            }

            for (int i = 1; i < dataGridView2.RowCount + 1; i++)
                dataGridView2.Rows[i-1].Cells[0].Value = i;
        }

        //(界面一)
        private void button3_Click(object sender, EventArgs e)
        {
            int i = dataGridView2.SelectedRows.Count;
            if (dataGridView2.SelectedRows.Count == 0)
            {
                MessageBox.Show("请选择某一或几行作为删除位置！");
            }
            else
            {
                for (; i > 0; i--)
                { dataGridView2.Rows.Remove(dataGridView2.SelectedRows[i - 1]); }
            }
        }

        //(界面二)
        private void button4_Click(object sender, EventArgs e)
        {
            int i =dataGridView1.SelectedRows.Count;
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("请选择某一或几行作为删除位置！");
            }
            else
            { 
            for (; i > 0; i--)
            { dataGridView1.Rows.Remove(dataGridView1.SelectedRows[i - 1]); }
            }
        }

        private void textBox11_TextChanged(object sender, EventArgs e)
        {
            if (textBox11.Text == "")
                textBox11.Text = "无";
        }

        private void textBox10_TextChanged_1(object sender, EventArgs e)
        {

            if (textBox10.Text == "")
                textBox10.Text = "0.0";
            Regex Matches = new Regex(@"^-?([1-9][0-9]*|0)\.?[0-9]*$");
            if (!Matches.IsMatch(textBox7.Text) && textBox7.Text != "")
            {
                textBox7.Text = textBox7.Text.Substring(0, textBox7.Text.Length - 1);
            }
        }

        private void textBox7_TextChanged_1(object sender, EventArgs e)
        {
            if (textBox7.Text == "-")
                textBox7.Text = "-0";
            if (textBox7.Text == "")
                textBox7.Text = "0.0";
            Regex Matches = new Regex(@"^-?([1-9][0-9]*|0)\.?[0-9]*$");
            if (!Matches.IsMatch(textBox7.Text) && textBox7.Text != "")
            {
                textBox7.Text = textBox7.Text.Substring(0, textBox7.Text.Length - 1);
            }
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            if (dataGridView1.Enabled == true)
            {
                button_View2_锁定设备表.Text = "解锁设备表";
                dataGridView1.Enabled = false;
            }
            else
            {
                button_View2_锁定设备表.Text = "锁定设备表";
                dataGridView1.Enabled = true;
            }
        }

        //(界面四)
        private void radioButton1_Click(object sender, EventArgs e)
        {
            panel2.Hide();
            button5.Visible = true;
            button6.Visible = false;
            label34.Text = " 八个风向风速联合频率表";
        }

        //(界面四)
        private void radioButton2_Click(object sender, EventArgs e)
        {
            panel2.Show();
            button5.Visible = false;
            button6.Visible = true;
            label34.Text = "十六个风向风速联合频率表";
        }

        //(界面四)八个风向风速联合频率表新增按钮(dataGridView3)
        private void button5_Click(object sender, EventArgs e)
        {
            DataGridViewRow dr = new DataGridViewRow();
            dr.CreateCells(dataGridView3);
            dr.Cells[0].Value = 0.0;
            dr.Cells[1].Value = 0.0;
            dr.Cells[2].Value = 0.0;
            dr.Cells[3].Value = 0.0;
            dr.Cells[4].Value = 0.0;
            dr.Cells[5].Value = 0.0;
            dr.Cells[6].Value = 0.0;
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 0.0;

            if (dataGridView3.SelectedRows.Count != 1)
            {
                dataGridView3.Rows.Insert(dataGridView3.RowCount, dr);
            }
            else
            {
                dataGridView3.Rows.Insert(dataGridView3.SelectedRows[0].Index, dr);
            }
        }

        //(界面四)十六个风向风速联合频率表新增按钮(dataGridView4)
        private void button6_Click(object sender, EventArgs e)
        {
            DataGridViewRow dr = new DataGridViewRow();
            dr.CreateCells(dataGridView4);
            dr.Cells[0].Value = 0.0;
            dr.Cells[1].Value = 0.0;
            dr.Cells[2].Value = 0.0;
            dr.Cells[3].Value = 0.0;
            dr.Cells[4].Value = 0.0;
            dr.Cells[5].Value = 0.0;
            dr.Cells[6].Value = 0.0;
            dr.Cells[7].Value = 0.0;
            dr.Cells[8].Value = 0.0;
            dr.Cells[9].Value = 0.0;
            dr.Cells[10].Value = 0.0;
            dr.Cells[11].Value = 0.0;
            dr.Cells[12].Value = 0.0;
            dr.Cells[13].Value = 0.0;
            dr.Cells[14].Value = 0.0;
            dr.Cells[15].Value = 0.0;
            dr.Cells[16].Value = 0.0;

            if (dataGridView4.SelectedRows.Count != 1)
            {
                dataGridView4.Rows.Insert(dataGridView4.RowCount, dr);
            }
            else
            {
                dataGridView4.Rows.Insert(dataGridView4.SelectedRows[0].Index, dr);
            }
        }

        //(界面四)
        private void btn_View4_Check_Click(object sender, EventArgs e)
        {
            if (" 八个风向风速联合频率表" == label34.Text)
            {
                int rowIndex = this.dataGridView3.CurrentRow.Index;
                rowIndex++;
                if (rowIndex > 0)
                {
                    View2Flag = true;
                    WindSpeed = double.Parse(dataGridView3.Rows[rowIndex - 1].Cells[0].Value.ToString());
                    for (int i = 0; i < dataGridView3.Rows.Count; i++)
                    {
                        if (WindSpeed == double.Parse(dataGridView3.Rows[i].Cells[0].Value.ToString()))
                        {
                            WindGrounpNum++;
                            for (int j = 1; j < 9; j++ )
                                Pw.Add(double.Parse(dataGridView3.Rows[i].Cells[j].Value.ToString()));
                        }
                    }
                    MessageBox.Show("选择了风速为: " + WindSpeed + " 的数据！", "提醒", MessageBoxButtons.OK);
                }
                else
                {
                    View2Flag = false;
                    MessageBox.Show("请先选择一行数据！", "提醒", MessageBoxButtons.OK);
                }
            }
            else if ("十六个风向风速联合频率表" == label34.Text)
            {
                int rowIndex = this.dataGridView4.CurrentRow.Index;
                rowIndex++;
                if (rowIndex > 0)
                {
                    View2Flag = true;
                    WindSpeed = double.Parse(dataGridView4.Rows[rowIndex - 1].Cells[0].Value.ToString());
                    for (int i = 0; i < dataGridView4.Rows.Count; i++)
                    {
                        if (WindSpeed == double.Parse(dataGridView4.Rows[i].Cells[0].Value.ToString()))
                        {
                            WindGrounpNum++;
                            for (int j = 1; j < 17; j++)
                                Pw.Add(double.Parse(dataGridView4.Rows[i].Cells[j].Value.ToString()));
                        }
                    }
                    MessageBox.Show("选择了风速为: " + WindSpeed + " 的数据！", "提醒", MessageBoxButtons.OK);
                }
                else
                {
                    View2Flag = false;
                    MessageBox.Show("请先选择一行数据！", "提醒", MessageBoxButtons.OK);
                }
            }
        }


        class XYLinesFactory
        {
            #region   画出X轴与Y轴
            /// <summary>
            /// 在任意的panel里画一个坐标，坐标所在的四边形距离panel边50像素
            /// </summary>
            /// <param name="pan"></param>
            public static void DrawXY(Graphics g, PictureBox pan)
            {
                //Graphics g = pan.CreateGraphics();
                //g = pan.CreateGraphics();
                //整体内缩move像素
                float move = 50f;
                float newX = pan.Width - move;
                float newY = pan.Height - move;

                //绘制X轴,
                PointF px1 = new PointF(move, newY);
                PointF px2 = new PointF(newX, newY);
                g.DrawLine(new Pen(Brushes.Black, 2), px1, px2);
                //绘制Y轴
                PointF py1 = new PointF(move, move);
                PointF py2 = new PointF(move, newY);

                g.DrawLine(new Pen(Brushes.Black, 2), py1, py2);
            }
            #endregion

            /// <summary>
            /// 画出Y轴上的分值线，从零开始
            /// </summary>
            /// <param name="pan"></param>
            /// <param name="maxY"></param>
            /// <param name="len"></param>
            #region   画出Y轴上的分值线，从零开始
            public static void DrawYLine(Graphics g, PictureBox pan, float maxY, int len)
            {
                float move = 50f;
                float LenX = pan.Width - 2 * move;
                float LenY = pan.Height - 2 * move;
                //Graphics g = pan.CreateGraphics();
                for (int i = 0; i <= len; i++)    //len等份Y轴
                {
                    PointF px1 = new PointF(move, LenY * i / len + move);
                    PointF px2 = new PointF(move + 4, LenY * i / len + move);
                    //刻度数字
                    string sx = (maxY - maxY * i / len).ToString();
                    g.DrawLine(new Pen(Brushes.Black, 2), px1, px2);
                    StringFormat drawFormat = new StringFormat();
                    drawFormat.Alignment = StringAlignment.Far;
                    drawFormat.LineAlignment = StringAlignment.Center;
                    g.DrawString(sx, new Font("宋体", 8f), Brushes.Black, new PointF(move / 1.2f, LenY * i / len + move * 1.1f), drawFormat);
                }
                Pen pen = new Pen(Color.Black, 1);
                g.DrawString("Y轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(move / 3, move / 2f));
            }
            #endregion

            /// <summary>
            /// 画出Y轴上的分值线，从任意值开始
            /// </summary>
            /// <param name="pan"></param>
            /// <param name="minY"></param>
            /// <param name="maxY"></param>
            /// <param name="len"></param>
            #region   画出Y轴上的分值线，从任意值开始
            public static void DrawYLine(PictureBox pan, float minY, float maxY, int len)
            {
                float move = 50f;
                float LenX = pan.Width - 2 * move;
                float LenY = pan.Height - 2 * move;
                Graphics g = pan.CreateGraphics();
                for (int i = 0; i <= len; i++)    //len等份Y轴
                {
                    PointF px1 = new PointF(move, LenY * i / len + move);
                    PointF px2 = new PointF(move + 4, LenY * i / len + move);
                    string sx = (maxY - (maxY - minY) * i / len).ToString();
                    g.DrawLine(new Pen(Brushes.Black, 2), px1, px2);
                    StringFormat drawFormat = new StringFormat();
                    drawFormat.Alignment = StringAlignment.Far;
                    drawFormat.LineAlignment = StringAlignment.Center;
                    g.DrawString(sx, new Font("宋体", 8f), Brushes.Black, new PointF(move / 1.2f, LenY * i / len + move * 1.1f), drawFormat);
                }
                Pen pen = new Pen(Color.Black, 1);
                g.DrawString("Y轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(move / 3, move / 2f));
            }

            #endregion
            /// <summary>
            /// 画出X轴上的分值线，从零开始
            /// </summary>
            /// <param name="pan"></param>
            /// <param name="maxX"></param>
            /// <param name="len"></param>
            #region   画出X轴上的分值线，从零开始
            public static void DrawXLine(Graphics g, PictureBox pan, float maxX, int len)
            {
                float move = 50f;
                float LenX = pan.Width - 2 * move;
                float LenY = pan.Height - 2 * move;
                //Graphics g = pan.CreateGraphics();
                for (int i = 1; i <= len; i++)
                {
                    PointF py1 = new PointF(LenX * i / len + move, pan.Height - move - 4);
                    PointF py2 = new PointF(LenX * i / len + move, pan.Height - move);
                    string sy = (maxX * i / len).ToString();
                    g.DrawLine(new Pen(Brushes.Black, 2), py1, py2);
                    g.DrawString(sy, new Font("宋体", 8f), Brushes.Black, new PointF(LenX * i / len + move, pan.Height - move / 1.1f));
                }
                Pen pen = new Pen(Color.Black, 1);
                g.DrawString("X轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(pan.Width - move / 1.5f, pan.Height - move / 1.5f));
            }
            #endregion

            #region   画出X轴上的分值线，从任意值开始
            /// <summary>
            /// 画出X轴上的分值线，从任意值开始
            /// </summary>
            /// <param name="pan"></param>
            /// <param name="minX"></param>
            /// <param name="maxX"></param>
            /// <param name="len"></param>
            public static void DrawXLine(PictureBox pan, float minX, float maxX, int len)
            {
                float move = 50f;
                float LenX = pan.Width - 2 * move;
                float LenY = pan.Height - 2 * move;
                Graphics g = pan.CreateGraphics();
                for (int i = 0; i <= len; i++)
                {
                    PointF py1 = new PointF(LenX * i / len + move, pan.Height - move - 4);
                    PointF py2 = new PointF(LenX * i / len + move, pan.Height - move);
                    string sy = ((maxX - minX) * i / len + minX).ToString();
                    g.DrawLine(new Pen(Brushes.Black, 2), py1, py2);
                    g.DrawString(sy, new Font("宋体", 8f), Brushes.Black, new PointF(LenX * i / len + move, pan.Height - move / 1.1f));
                }
                Pen pen = new Pen(Color.Black, 1);
                g.DrawString("X轴", new Font("宋体 ", 10f), Brushes.Black, new PointF(pan.Width - move / 1.5f, pan.Height - move / 1.5f));
            }
            #endregion
        }

        //(界面1)绘制工厂边界
        private void button3_Click_1(object sender, EventArgs e)
        {
            this.pictureBox1.Refresh();
            this.toolBarLabel_State.Text = "站场边界输入完成";
        }

        //(界面1)
        private void btn_View1_Clear_Click(object sender, EventArgs e)
        {
            if (dataGridView2.RowCount > 0)
            {
                dataGridView2.Rows.Clear();
            }
        }

        //(界面1)
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            /*
            Graphics g = e.Graphics; //创建画板,这里的画板是由Form提供的.
            Pen p = new Pen(Color.Blue, 2);//定义了一个蓝色,宽度为的画笔
            g.DrawLine(p, 10, 10, 100, 100);//在画板上画直线,起始坐标为(10,10),终点坐标为(100,100)
            g.DrawRectangle(p, 10, 10, 100, 100);//在画板上画矩形,起始坐标为(10,10),宽为,高为
            g.DrawEllipse(p, 10, 10, 100, 100);//在画板上画椭圆,起始坐标为(10,10),外接矩形的宽为,高为
            */
            float maxAllX = 100.0f;
            float maxAllY = 100.0f;
            float timesX = 0;
            float timesY = 0;
            float minSizeX = 10000;
            float minSizeY = 10000;
            float maxSizeX = 0;
            float maxSizeY = 0;
            if (dataGridView2.RowCount >= 0)
            {
                Graphics g = e.Graphics; //创建画板,这里的画板是由Form提供的.
                Pen p = new Pen(Color.Blue, 3);//定义了一个蓝色,宽度为的画笔
                float maxX = 0.0f;
                float maxY = 0.0f;
                //float maxNum = 100.0f;
                //int times = 0;
                for (int i = 0; i < dataGridView2.RowCount - 1; i++)
                {
                    float x = float.Parse(dataGridView2.Rows[i].Cells[1].Value.ToString());// + 50.0f;
                    float y = float.Parse(dataGridView2.Rows[i].Cells[2].Value.ToString());
                    if (x > maxSizeX)
                        maxSizeX = x;
                    if (y > maxSizeY)
                        maxSizeY = y;
                    if (x < minSizeX)
                        minSizeX = x;
                    if (y < minSizeY)
                        minSizeY = y;
                }
                maxX = maxSizeX;
                maxY = maxSizeY;
                //if (maxX >= maxY)
                //{
                    if (maxX <= 100)
                    {
                        maxAllX = 100.0f;
                    }
                    else if (maxX <= 200)
                    {
                        maxAllX = 200.0f;
                    }
                    else if (maxX <= 400)
                    {
                        maxAllX = 400.0f;
                    }
                    else if (maxX <= 600)
                    {
                        maxAllX = 600.0f;
                    }
                    else if (maxX <= 800)
                    {
                        maxAllX = 800.0f;
                    }
                    else if (maxX <= 1000)
                    {
                        maxAllX = 1000.0f;
                    }
                    else if (maxX <= 2000)
                    {
                        maxAllX = 2000.0f;
                    }
                    else if (maxX <= 3000)
                    {
                        maxAllX = 3000.0f;
                    }
                    else if (maxX <= 4000)
                    {
                        maxAllX = 4000.0f;
                    }
                    else if (maxX <= 5000)
                    {
                        maxAllX = 5000.0f;
                    }
                    timesX = (this.pictureBox1.Width - 100.0f) / maxAllX;
                //}
                //else
                //{
                    if (maxY <= 100)
                    {
                        maxAllY = 100.0f;
                    }
                    else if (maxY <= 200)
                    {
                        maxAllY = 200.0f;
                    }
                    else if (maxY <= 400)
                    {
                        maxAllY = 400.0f;
                    }
                    else if (maxY <= 600)
                    {
                        maxAllY = 600.0f;
                    }
                    else if (maxY <= 800)
                    {
                        maxAllY = 800.0f;
                    }
                    else if (maxY <= 1000)
                    {
                        maxAllY = 1000.0f;
                    }
                    else if (maxY <= 2000)
                    {
                        maxAllY = 2000.0f;
                    }
                    else if (maxY <= 3000)
                    {
                        maxAllY = 3000.0f;
                    }
                    else if (maxY <= 4000)
                    {
                        maxAllY = 4000.0f;
                    }
                    else if (maxY <= 5000)
                    {
                        maxAllY = 5000.0f;
                    }
                    timesY = (this.pictureBox1.Height - 100.0f) / maxAllY;
                //}
                
                for (int i = 0; i < dataGridView2.RowCount - 1; i++)
                {
                    float x1 = float.Parse(dataGridView2.Rows[i].Cells[1].Value.ToString()) * timesX + 50.0f;
                    //float x1 = float.Parse(dataGridView2.Rows[i].Cells[1].Value.ToString()) + 50.0f;
                    float y1 = 550.0f - float.Parse(dataGridView2.Rows[i].Cells[2].Value.ToString()) * timesY; //- 50.0f;
                    float x2 = float.Parse(dataGridView2.Rows[i + 1].Cells[1].Value.ToString()) * timesX + 50.0f;
                    //float x2 = float.Parse(dataGridView2.Rows[i + 1].Cells[1].Value.ToString()) + 50.0f;
                    float y2 = 550.0f - float.Parse(dataGridView2.Rows[i + 1].Cells[2].Value.ToString()) * timesY;// -50.0f;
                    g.DrawLine(p, x1, y1, x2, y2);//在画板上画直线,起始坐标为(x1, y1),终点坐标为(x2, y2)
                }
                //g.DrawLine(p, 50, 550, 50, 500);//在画板上画直线,起始坐标为(x1, y1),终点坐标为(x2, y2)
                XYLinesFactory.DrawXY(g, pictureBox1);
                XYLinesFactory.DrawXLine(g, pictureBox1, maxAllX, 5);
                XYLinesFactory.DrawYLine(g, pictureBox1, maxAllY, 5);
            }
            if (dataGridView1.RowCount > 0)
            {
                Graphics g = e.Graphics; //创建画板,这里的画板是由Form提供的.
                float move = 10f;
                for (int i = 0; i < dataGridView1.RowCount; i++)
                {
                    int num = int.Parse(dataGridView1.Rows[i].Cells[0].Value.ToString());
                    string typeStr = dataGridView1.Rows[i].Cells[1].Value.ToString();
                    float x = float.Parse(dataGridView1.Rows[i].Cells[2].Value.ToString()) * timesX + 50.0f;
                    float y = 550.0f - float.Parse(dataGridView1.Rows[i].Cells[3].Value.ToString()) * timesY;

                    //g.DrawString(".", new Font("宋体 ", 10f), Brushes.Green, new PointF(move / 3, move / 2f));
                    if (typeStr.Contains("加热炉"))
                    {
                        g.DrawString(".", new Font("宋体 ", 30f), Brushes.Red, new PointF(x - move, y - 40));
                        g.DrawString("" + num, new Font("宋体 ", 10f), Brushes.Red, new PointF(x + 5, y - 25));
                    }
                    else if (typeStr.Contains("三相分离器"))
                    {
                        g.DrawString(".", new Font("宋体 ", 30f), Brushes.ForestGreen, new PointF(x - move, y - 40));
                        g.DrawString("" + num, new Font("宋体 ", 10f), Brushes.ForestGreen, new PointF(x + 5, y - 25));
                    }
                    else if (typeStr.Contains("原油稳定塔"))
                    {
                        g.DrawString(".", new Font("宋体 ", 30f), Brushes.DarkBlue, new PointF(x - move, y - 40));
                        g.DrawString("" + num, new Font("宋体 ", 10f), Brushes.DarkBlue, new PointF(x + 5, y - 25));
                    }
                    else if (typeStr.Contains("分离器"))
                    {
                        g.DrawString(".", new Font("宋体 ", 30f), Brushes.Cyan, new PointF(x - move, y - 40));
                        g.DrawString("" + num, new Font("宋体 ", 10f), Brushes.Cyan, new PointF(x + 5, y - 25));
                    }
                    else if (typeStr.Contains("换热器"))
                    {
                        g.DrawString(".", new Font("宋体 ", 30f), Brushes.Orange, new PointF(x - move, y - 40));
                        g.DrawString("" + num, new Font("宋体 ", 10f), Brushes.Orange, new PointF(x + 5, y - 25));
                    }
                    else if (typeStr.Contains("原油储罐"))
                    {
                        g.DrawString(".", new Font("宋体 ", 30f), Brushes.Brown, new PointF(x - move, y - 40));
                        g.DrawString("" + num, new Font("宋体 ", 10f), Brushes.Brown, new PointF(x + 5, y - 25));
                    }
                    else if (typeStr.Contains(""))
                    {
                        g.DrawString(".", new Font("宋体 ", 30f), Brushes.Purple, new PointF(x - move, y - 40));
                        g.DrawString("" + num, new Font("宋体 ", 10f), Brushes.Purple, new PointF(x + 5, y - 25));
                    }
                }
            }
            if (dataGridView2.RowCount > 0)
                this.toolBarLabel_Area.Text = ((maxSizeX - minSizeX) * (maxSizeY - minSizeY)).ToString();
            else
                this.toolBarLabel_Area.Text = "0";
        }


        //(界面2)计算选择数
        private void button_View2_计算选择数_Click(object sender, EventArgs e)
        {
            Form8 Frm = new Form8(createchoosenumtable());
            Frm.ShowDialog();
            if (Frm.DialogResult == System.Windows.Forms.DialogResult.Cancel)
            {
                if (Frm.isChecked)
                {
                    View1Flag = true;
                    EDEquipID = Frm.EDEquipID;
                    EquipFrequent = Frm.EquipFrequent;
                    this.toolBarLabel_DeviceNum.Text = Frm.EDEquipNum.ToString();
                }
                else
                {
                    View1Flag = false;
                    EDEquipID = -1;
                    EquipFrequent = 0.0;
                    this.toolBarLabel_DeviceNum.Text = "0";
                }
                this.toolBarLabel_State.Text = "风险计算";
            }
        }

        private void 选择设备ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form8 Frm = new Form8(createchoosenumtable());
            Frm.ShowDialog();
            if (Frm.DialogResult == System.Windows.Forms.DialogResult.Cancel)
            {
                if (Frm.isChecked)
                {
                    View1Flag = true;
                    EDEquipID = Frm.EDEquipID;
                    EquipFrequent = Frm.EquipFrequent;
                    this.toolBarLabel_DeviceNum.Text = Frm.EDEquipNum.ToString();
                }
                else
                {
                    View1Flag = false;
                    EDEquipID = -1;
                    EquipFrequent = 0.0;
                    this.toolBarLabel_DeviceNum.Text = "0";
                }
                this.toolBarLabel_State.Text = "风险计算";
            }
        }

        //(界面2)锁定设备表
        private void button_View2_锁定设备表_Click(object sender, EventArgs e)
        {
            Equipment[] equips = renewequips();
            BoundaryPoint a;
            a.X = 0;
            a.Y = 0;
            equips[0].CommandNum();
            choosenum(a, equips[0]);
            this.toolBarLabel_State.Text = "站场设备输入完成";
        }


        //(界面3)
        private void button_View3_TypeChoose_Click(object sender, EventArgs e)
        {
            panel3.Hide();
            if (radioButton3.Checked == true)
            {
                panel_可燃气体瞬间释放.Show();
                label_View3_Title.Text = "可燃气体瞬间释放";
                textBox1_可燃气体瞬间释放.Text = EquipFrequent.ToString();
            }
            else if (radioButton4.Checked == true)
            {
                panel_可燃气体连续释放.Show();
                label_View3_Title.Text = "可燃气体连续释放";
                textBox1_可燃气体连续释放.Text = EquipFrequent.ToString();
            }
            else if (radioButton5.Checked == true)
            {
                panel_压缩液化气体瞬间释放.Show();
                label_View3_Title.Text = "压缩液化气体瞬间释放";
                textBox1_压缩气体瞬间释放.Text = EquipFrequent.ToString();
            }
            else if (radioButton6.Checked == true)
            {
                panel_压缩液化气体连续释放.Show();
                label_View3_Title.Text = "压缩液化气体连续释放";
                textBox1_压缩气体连续释放.Text = EquipFrequent.ToString();
            }
            else if (radioButton7.Checked == true)
            {
                panel_可燃液体释放.Show();
                label_View3_Title.Text = "可燃液体释放";
                textBox1_可燃液体释放.Text = EquipFrequent.ToString();
            }
        }

        //返回事件树选择4 可燃气体瞬间释放
        private void button10_Click(object sender, EventArgs e)
        {
            panel_可燃气体瞬间释放.Hide();
            panel3.Show();
            label_View3_Title.Text = "事件树";
            textBox1_可燃气体瞬间释放.Text = "";
        }

        //返回事件树选择5 可燃气体连续释放
        private void button11_Click(object sender, EventArgs e)
        {
            panel_可燃气体连续释放.Hide();
            panel3.Show();
            label_View3_Title.Text = "事件树";
            textBox1_可燃气体连续释放.Text = "";
        }

        //返回事件树选择6 压缩液化气体瞬间释放
        private void button12_Click(object sender, EventArgs e)
        {
            panel_压缩液化气体瞬间释放.Hide();
            panel3.Show();
            label_View3_Title.Text = "事件树";
            textBox1_压缩气体瞬间释放.Text = "";
        }

        //返回事件树选择7 压缩液化气体连续释放
        private void button13_Click(object sender, EventArgs e)
        {
            panel_压缩液化气体连续释放.Hide();
            panel3.Show();
            label_View3_Title.Text = "事件树";
            textBox1_压缩气体连续释放.Text = "";
        }

        //返回事件树选择8 可燃液体释放
        private void button14_Click(object sender, EventArgs e)
        {
            panel_可燃液体释放.Hide();
            panel3.Show();
            label_View3_Title.Text = "事件树";
            textBox1_可燃液体释放.Text = "";
        }

        //(界面3)可燃气体瞬间释放
        private void button_可燃气体瞬间释放_OK_Click(object sender, EventArgs e)
        {
            //火球
            if ("" != textBox1_可燃气体瞬间释放.Text && "请输入概率" != textBox1_可燃气体瞬间释放.Text &&
                "" != textBox2_可燃气体瞬间释放.Text && "请输入概率" != textBox2_可燃气体瞬间释放.Text &&
                "" != textBox4_可燃气体瞬间释放.Text && "请输入概率" != textBox4_可燃气体瞬间释放.Text)
            {
                textBox9_可燃气体瞬间释放.Text = (double.Parse(textBox1_可燃气体瞬间释放.Text) * double.Parse(textBox2_可燃气体瞬间释放.Text) * double.Parse(textBox4_可燃气体瞬间释放.Text)).ToString();
                Pi.Add(double.Parse(textBox9_可燃气体瞬间释放.Text));
            }
            //爆炸
            if ("" != textBox1_可燃气体瞬间释放.Text && "请输入概率" != textBox1_可燃气体瞬间释放.Text &&
                "" != textBox2_可燃气体瞬间释放.Text && "请输入概率" != textBox2_可燃气体瞬间释放.Text &&
                "" != textBox5_可燃气体瞬间释放.Text && "请输入概率" != textBox5_可燃气体瞬间释放.Text)
            {
                textBox10_可燃气体瞬间释放.Text = (double.Parse(textBox1_可燃气体瞬间释放.Text) * double.Parse(textBox2_可燃气体瞬间释放.Text) * double.Parse(textBox5_可燃气体瞬间释放.Text)).ToString();
                Pi.Add(double.Parse(textBox10_可燃气体瞬间释放.Text));
            }
            //闪火
            if ("" != textBox1_可燃气体瞬间释放.Text && "请输入概率" != textBox1_可燃气体瞬间释放.Text &&
                "" != textBox2_可燃气体瞬间释放.Text && "请输入概率" != textBox2_可燃气体瞬间释放.Text &&
                "" != textBox6_可燃气体瞬间释放.Text && "请输入概率" != textBox6_可燃气体瞬间释放.Text)
            {
                textBox11_可燃气体瞬间释放.Text = (double.Parse(textBox1_可燃气体瞬间释放.Text) * double.Parse(textBox2_可燃气体瞬间释放.Text) * double.Parse(textBox6_可燃气体瞬间释放.Text)).ToString();
                Pi.Add(double.Parse(textBox11_可燃气体瞬间释放.Text));
            }
            //爆炸
            if ("" != textBox1_可燃气体瞬间释放.Text && "请输入概率" != textBox1_可燃气体瞬间释放.Text &&
                "" != textBox3_可燃气体瞬间释放.Text && "请输入概率" != textBox3_可燃气体瞬间释放.Text &&
                "" != textBox7_可燃气体瞬间释放.Text && "请输入概率" != textBox7_可燃气体瞬间释放.Text &&
                "" != textBox14_可燃气体瞬间释放.Text && "请输入概率" != textBox14_可燃气体瞬间释放.Text)
            {
                textBox12_可燃气体瞬间释放.Text = (double.Parse(textBox1_可燃气体瞬间释放.Text) * double.Parse(textBox3_可燃气体瞬间释放.Text)
                    * double.Parse(textBox7_可燃气体瞬间释放.Text) * double.Parse(textBox14_可燃气体瞬间释放.Text)).ToString();
                Pi.Add(double.Parse(textBox12_可燃气体瞬间释放.Text));
            }
            //闪火
            if ("" != textBox1_可燃气体瞬间释放.Text && "请输入概率" != textBox1_可燃气体瞬间释放.Text &&
                "" != textBox3_可燃气体瞬间释放.Text && "请输入概率" != textBox3_可燃气体瞬间释放.Text &&
                "" != textBox8_可燃气体瞬间释放.Text && "请输入概率" != textBox8_可燃气体瞬间释放.Text &&
                "" != textBox14_可燃气体瞬间释放.Text && "请输入概率" != textBox14_可燃气体瞬间释放.Text)
            {
                textBox13_可燃气体瞬间释放.Text = (double.Parse(textBox1_可燃气体瞬间释放.Text) * double.Parse(textBox3_可燃气体瞬间释放.Text)
                    * double.Parse(textBox8_可燃气体瞬间释放.Text) * double.Parse(textBox14_可燃气体瞬间释放.Text)).ToString();
                Pi.Add(double.Parse(textBox13_可燃气体瞬间释放.Text));
            }
            View3Flag = true;
            this.toolBarLabel_State.Text = "事故概率计算完成";
        }

        //(界面3)可燃气体连续释放
        private void button_可燃气体连续释放_OK_Click(object sender, EventArgs e)
        {
            //喷射火
            if ("" != textBox1_可燃气体连续释放.Text && "请输入概率" != textBox1_可燃气体连续释放.Text &&
                "" != textBox2_可燃气体连续释放.Text && "请输入概率" != textBox2_可燃气体连续释放.Text)
            {
                textBox6_可燃气体连续释放.Text = (double.Parse(textBox1_可燃气体连续释放.Text) * double.Parse(textBox2_可燃气体连续释放.Text)).ToString();
                Pi.Add(double.Parse(textBox6_可燃气体连续释放.Text));
            }
            //爆炸
            if ("" != textBox1_可燃气体连续释放.Text && "请输入概率" != textBox1_可燃气体连续释放.Text &&
                "" != textBox3_可燃气体连续释放.Text && "请输入概率" != textBox3_可燃气体连续释放.Text &&
                "" != textBox4_可燃气体连续释放.Text && "请输入概率" != textBox4_可燃气体连续释放.Text &&
                "" != textBox9_可燃气体连续释放.Text && "请输入概率" != textBox9_可燃气体连续释放.Text)
            {
                textBox7_可燃气体连续释放.Text = (double.Parse(textBox1_可燃气体连续释放.Text) * double.Parse(textBox3_可燃气体连续释放.Text)
                    * double.Parse(textBox4_可燃气体连续释放.Text) * double.Parse(textBox9_可燃气体连续释放.Text)).ToString();
                Pi.Add(double.Parse(textBox7_可燃气体连续释放.Text));
            }
            //闪火
            if ("" != textBox1_可燃气体连续释放.Text && "请输入概率" != textBox1_可燃气体连续释放.Text &&
                "" != textBox3_可燃气体连续释放.Text && "请输入概率" != textBox3_可燃气体连续释放.Text &&
                "" != textBox5_可燃气体连续释放.Text && "请输入概率" != textBox5_可燃气体连续释放.Text &&
                "" != textBox9_可燃气体连续释放.Text && "请输入概率" != textBox9_可燃气体连续释放.Text)
            {
                textBox8_可燃气体连续释放.Text = (double.Parse(textBox1_可燃气体连续释放.Text) * double.Parse(textBox3_可燃气体连续释放.Text)
                    * double.Parse(textBox5_可燃气体连续释放.Text) * double.Parse(textBox9_可燃气体连续释放.Text)).ToString();
                Pi.Add(double.Parse(textBox8_可燃气体连续释放.Text));
            }
            View3Flag = true;
            this.toolBarLabel_State.Text = "事故概率计算完成";
        }

        //(界面3)压缩液化气体瞬间释放
        private void button_压缩液化气体瞬间释放_OK_Click(object sender, EventArgs e)
        {
            //BLEVE
            if ("" != textBox1_压缩气体瞬间释放.Text && "请输入概率" != textBox1_压缩气体瞬间释放.Text &&
                "" != textBox2_压缩气体瞬间释放.Text && "请输入概率" != textBox2_压缩气体瞬间释放.Text &&
            "" != textBox4_压缩气体瞬间释放.Text && "请输入概率" != textBox4_压缩气体瞬间释放.Text)
            {
                textBox9_压缩气体瞬间释放.Text = (double.Parse(textBox1_压缩气体瞬间释放.Text) * double.Parse(textBox2_压缩气体瞬间释放.Text) * double.Parse(textBox4_压缩气体瞬间释放.Text)).ToString();
                Pi.Add(double.Parse(textBox9_压缩气体瞬间释放.Text));
            }
            //爆炸
            if ("" != textBox1_压缩气体瞬间释放.Text && "请输入概率" != textBox1_压缩气体瞬间释放.Text &&
                "" != textBox2_压缩气体瞬间释放.Text && "请输入概率" != textBox2_压缩气体瞬间释放.Text &&
            "" != textBox5_压缩气体瞬间释放.Text && "请输入概率" != textBox5_压缩气体瞬间释放.Text)
            {
                textBox10_压缩气体瞬间释放.Text = (double.Parse(textBox1_压缩气体瞬间释放.Text) * double.Parse(textBox2_压缩气体瞬间释放.Text) * double.Parse(textBox5_压缩气体瞬间释放.Text)).ToString();
                Pi.Add(double.Parse(textBox10_压缩气体瞬间释放.Text));
            }
            //闪火
            if ("" != textBox1_压缩气体瞬间释放.Text && "请输入概率" != textBox1_压缩气体瞬间释放.Text &&
                "" != textBox2_压缩气体瞬间释放.Text && "请输入概率" != textBox2_压缩气体瞬间释放.Text &&
            "" != textBox6_压缩气体瞬间释放.Text && "请输入概率" != textBox6_压缩气体瞬间释放.Text)
            {
                textBox11_压缩气体瞬间释放.Text = (double.Parse(textBox1_压缩气体瞬间释放.Text) * double.Parse(textBox2_压缩气体瞬间释放.Text) * double.Parse(textBox6_压缩气体瞬间释放.Text)).ToString();
                Pi.Add(double.Parse(textBox11_压缩气体瞬间释放.Text));
            }
            //爆炸
            if ("" != textBox1_压缩气体瞬间释放.Text && "请输入概率" != textBox1_压缩气体瞬间释放.Text &&
                "" != textBox3_压缩气体瞬间释放.Text && "请输入概率" != textBox3_压缩气体瞬间释放.Text &&
                "" != textBox7_压缩气体瞬间释放.Text && "请输入概率" != textBox7_压缩气体瞬间释放.Text &&
                "" != textBox14_压缩气体瞬间释放.Text && "请输入概率" != textBox14_压缩气体瞬间释放.Text)
            {
                textBox12_压缩气体瞬间释放.Text = (double.Parse(textBox1_压缩气体瞬间释放.Text) * double.Parse(textBox3_压缩气体瞬间释放.Text)
                    * double.Parse(textBox7_压缩气体瞬间释放.Text) * double.Parse(textBox14_压缩气体瞬间释放.Text)).ToString();
                Pi.Add(double.Parse(textBox12_压缩气体瞬间释放.Text));
            }
            //闪火
            if ("" != textBox1_压缩气体瞬间释放.Text && "请输入概率" != textBox1_压缩气体瞬间释放.Text &&
                "" != textBox3_压缩气体瞬间释放.Text && "请输入概率" != textBox3_压缩气体瞬间释放.Text &&
                "" != textBox8_压缩气体瞬间释放.Text && "请输入概率" != textBox8_压缩气体瞬间释放.Text &&
                "" != textBox14_压缩气体瞬间释放.Text && "请输入概率" != textBox14_压缩气体瞬间释放.Text)
            {
                textBox13_压缩气体瞬间释放.Text = (double.Parse(textBox1_压缩气体瞬间释放.Text) * double.Parse(textBox3_压缩气体瞬间释放.Text)
                    * double.Parse(textBox8_压缩气体瞬间释放.Text) * double.Parse(textBox14_压缩气体瞬间释放.Text)).ToString();
                Pi.Add(double.Parse(textBox13_压缩气体瞬间释放.Text));
            }
            View3Flag = true;
            this.toolBarLabel_State.Text = "事故概率计算完成";
        }

        //(界面3)压缩液化气体连续释放
        private void button_压缩液化气体连续释放_OK_Click(object sender, EventArgs e)
        {
            //喷射火
            if ("" != textBox1_压缩气体连续释放.Text && "请输入概率" != textBox1_压缩气体连续释放.Text &&
                "" != textBox2_压缩气体连续释放.Text && "请输入概率" != textBox2_压缩气体连续释放.Text)
            {
                textBox6_压缩气体连续释放.Text = (double.Parse(textBox1_压缩气体连续释放.Text) * double.Parse(textBox2_压缩气体连续释放.Text)).ToString();
                Pi.Add(double.Parse(textBox6_压缩气体连续释放.Text));
            }
            //爆炸
            if ("" != textBox1_压缩气体连续释放.Text && "请输入概率" != textBox1_压缩气体连续释放.Text &&
                "" != textBox3_压缩气体连续释放.Text && "请输入概率" != textBox3_压缩气体连续释放.Text &&
                "" != textBox4_压缩气体连续释放.Text && "请输入概率" != textBox4_压缩气体连续释放.Text &&
                "" != textBox9_压缩气体连续释放.Text && "请输入概率" != textBox9_压缩气体连续释放.Text)
            {
                textBox7_压缩气体连续释放.Text = (double.Parse(textBox1_压缩气体连续释放.Text) * double.Parse(textBox3_压缩气体连续释放.Text)
                    * double.Parse(textBox4_压缩气体连续释放.Text) * double.Parse(textBox9_压缩气体连续释放.Text)).ToString();
                Pi.Add(double.Parse(textBox7_压缩气体连续释放.Text));
            }
            //闪火
            if ("" != textBox1_压缩气体连续释放.Text && "请输入概率" != textBox1_压缩气体连续释放.Text &&
                "" != textBox3_压缩气体连续释放.Text && "请输入概率" != textBox3_压缩气体连续释放.Text &&
                "" != textBox5_压缩气体连续释放.Text && "请输入概率" != textBox5_压缩气体连续释放.Text &&
                "" != textBox9_压缩气体连续释放.Text && "请输入概率" != textBox9_压缩气体连续释放.Text)
            {
                textBox8_压缩气体连续释放.Text = (double.Parse(textBox1_压缩气体连续释放.Text) * double.Parse(textBox3_压缩气体连续释放.Text)
                    * double.Parse(textBox5_压缩气体连续释放.Text) * double.Parse(textBox9_压缩气体连续释放.Text)).ToString();
                Pi.Add(double.Parse(textBox8_压缩气体连续释放.Text));
            }
            View3Flag = true;
            this.toolBarLabel_State.Text = "事故概率计算完成";
        }

        //(界面3)可燃液体释放
        private void button_可燃液体释放_OK_Click(object sender, EventArgs e)
        {
            //池火
            if ("" != textBox1_可燃液体释放.Text && "请输入概率" != textBox1_可燃液体释放.Text &&
                "" != textBox2_可燃液体释放.Text && "请输入概率" != textBox2_可燃液体释放.Text)
            {
                textBox6_可燃液体释放.Text = (double.Parse(textBox1_可燃液体释放.Text) * double.Parse(textBox2_可燃液体释放.Text)).ToString();
                Pi.Add(double.Parse(textBox6_可燃液体释放.Text));
            }
            //爆炸
            if ("" != textBox1_可燃液体释放.Text && "请输入概率" != textBox1_可燃液体释放.Text &&
                "" != textBox3_可燃液体释放.Text && "请输入概率" != textBox3_可燃液体释放.Text &&
                "" != textBox4_可燃液体释放.Text && "请输入概率" != textBox4_可燃液体释放.Text &&
                "" != textBox9_可燃液体释放.Text && "请输入概率" != textBox9_可燃液体释放.Text)
            {
                textBox7_可燃液体释放.Text = (double.Parse(textBox1_可燃液体释放.Text) * double.Parse(textBox3_可燃液体释放.Text)
                    * double.Parse(textBox4_可燃液体释放.Text) * double.Parse(textBox9_可燃液体释放.Text)).ToString();
                Pi.Add(double.Parse(textBox7_可燃液体释放.Text));
            }
            //闪火
            if ("" != textBox1_可燃液体释放.Text && "请输入概率" != textBox1_可燃液体释放.Text &&
                "" != textBox3_可燃液体释放.Text && "请输入概率" != textBox3_可燃液体释放.Text &&
                "" != textBox5_可燃液体释放.Text && "请输入概率" != textBox5_可燃液体释放.Text &&
                "" != textBox9_可燃液体释放.Text && "请输入概率" != textBox9_可燃液体释放.Text)
            {
                textBox8_可燃液体释放.Text = (double.Parse(textBox1_可燃液体释放.Text) * double.Parse(textBox3_可燃液体释放.Text)
                    * double.Parse(textBox5_可燃液体释放.Text) * double.Parse(textBox9_可燃液体释放.Text)).ToString();
                Pi.Add(double.Parse(textBox8_可燃液体释放.Text));
            }
            View3Flag = true;
            this.toolBarLabel_State.Text = "事故概率计算完成";
        }

        //(界面3)
        private void textBox1_可燃气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox1_可燃气体瞬间释放.Text)
            {
                this.textBox1_可燃气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox2_可燃气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox2_可燃气体瞬间释放.Text)
            {
                this.textBox2_可燃气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox3_可燃气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox3_可燃气体瞬间释放.Text)
            {
                this.textBox3_可燃气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox4_可燃气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox4_可燃气体瞬间释放.Text)
            {
                this.textBox4_可燃气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox5_可燃气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox5_可燃气体瞬间释放.Text)
            {
                this.textBox5_可燃气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox6_可燃气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox6_可燃气体瞬间释放.Text)
            {
                this.textBox6_可燃气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox7_可燃气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox7_可燃气体瞬间释放.Text)
            {
                this.textBox7_可燃气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox8_可燃气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox8_可燃气体瞬间释放.Text)
            {
                this.textBox8_可燃气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox14_可燃气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox14_可燃气体瞬间释放.Text)
            {
                this.textBox14_可燃气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox1_可燃气体连续释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox1_可燃气体连续释放.Text)
            {
                this.textBox1_可燃气体连续释放.Text = "";
            }
        }

        //(界面3)
        private void textBox2_可燃气体连续释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox2_可燃气体连续释放.Text)
            {
                this.textBox2_可燃气体连续释放.Text = "";
            }
        }

        //(界面3)
        private void textBox3_可燃气体连续释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox3_可燃气体连续释放.Text)
            {
                this.textBox3_可燃气体连续释放.Text = "";
            }
        }

        //(界面3)
        private void textBox4_可燃气体连续释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox4_可燃气体连续释放.Text)
            {
                this.textBox4_可燃气体连续释放.Text = "";
            }
        }

        //(界面3)
        private void textBox5_可燃气体连续释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox5_可燃气体连续释放.Text)
            {
                this.textBox5_可燃气体连续释放.Text = "";
            }
        }

        //(界面3)
        private void textBox9_可燃气体连续释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox9_可燃气体连续释放.Text)
            {
                this.textBox9_可燃气体连续释放.Text = "";
            }
        }

        //(界面3)
        private void textBox1_压缩气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox1_压缩气体瞬间释放.Text)
            {
                this.textBox1_压缩气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox2_压缩气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox2_压缩气体瞬间释放.Text)
            {
                this.textBox2_压缩气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox3_压缩气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox3_压缩气体瞬间释放.Text)
            {
                this.textBox3_压缩气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox4_压缩气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox4_压缩气体瞬间释放.Text)
            {
                this.textBox4_压缩气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox5_压缩气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox5_压缩气体瞬间释放.Text)
            {
                this.textBox5_压缩气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox6_压缩气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox6_压缩气体瞬间释放.Text)
            {
                this.textBox6_压缩气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox7_压缩气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox7_压缩气体瞬间释放.Text)
            {
                this.textBox7_压缩气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox8_压缩气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox8_压缩气体瞬间释放.Text)
            {
                this.textBox8_压缩气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox14_压缩气体瞬间释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox14_压缩气体瞬间释放.Text)
            {
                this.textBox14_压缩气体瞬间释放.Text = "";
            }
        }

        //(界面3)
        private void textBox1_压缩气体连续释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox1_压缩气体连续释放.Text)
            {
                this.textBox1_压缩气体连续释放.Text = "";
            }
        }

        //(界面3)
        private void textBox2_压缩气体连续释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox2_压缩气体连续释放.Text)
            {
                this.textBox2_压缩气体连续释放.Text = "";
            }
        }

        //(界面3)
        private void textBox3_压缩气体连续释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox3_压缩气体连续释放.Text)
            {
                this.textBox3_压缩气体连续释放.Text = "";
            }
        }

        //(界面3)
        private void textBox4_压缩气体连续释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox4_压缩气体连续释放.Text)
            {
                this.textBox4_压缩气体连续释放.Text = "";
            }
        }

        //(界面3)
        private void textBox5_压缩气体连续释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox5_压缩气体连续释放.Text)
            {
                this.textBox5_压缩气体连续释放.Text = "";
            }
        }

        //(界面3)
        private void textBox9_压缩气体连续释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox9_压缩气体连续释放.Text)
            {
                this.textBox9_压缩气体连续释放.Text = "";
            }
        }

        //(界面3)
        private void textBox1_可燃液体释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox1_可燃液体释放.Text)
            {
                this.textBox1_可燃液体释放.Text = "";
            }
        }

        //(界面3)
        private void textBox2_可燃液体释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox2_可燃液体释放.Text)
            {
                this.textBox2_可燃液体释放.Text = "";
            }
        }

        //(界面3)
        private void textBox3_可燃液体释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox3_可燃液体释放.Text)
            {
                this.textBox3_可燃液体释放.Text = "";
            }
        }

        //(界面3)
        private void textBox4_可燃液体释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox4_可燃液体释放.Text)
            {
                this.textBox4_可燃液体释放.Text = "";
            }
        }

        //(界面3)
        private void textBox5_可燃液体释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox5_可燃液体释放.Text)
            {
                this.textBox5_可燃液体释放.Text = "";
            }
        }
        
        //(界面3)
        private void textBox9_可燃液体释放_MouseDown(object sender, MouseEventArgs e)
        {
            if ("请输入概率" == this.textBox9_可燃液体释放.Text)
            {
                this.textBox9_可燃液体释放.Text = "";
            }
        }


        //(界面5)
        private void button_新增矩形区域_Click(object sender, EventArgs e)
        {
            DataGridViewRow dr = new DataGridViewRow();
            dr.CreateCells(dataGridView5);
            dr.Cells[0].Value = "";
            dr.Cells[1].Value = 0.0;
            dr.Cells[2].Value = 0.0;
            dr.Cells[3].Value = 0.0;
            dr.Cells[4].Value = 0.0;
            dr.Cells[5].Value = 0.0;
            dr.Cells[6].Value = 0.0;
            dr.Cells[7].Value = 0.0;

            if (dataGridView5.SelectedRows.Count != 1)
            {
                dataGridView5.Rows.Insert(dataGridView5.RowCount, dr);
            }
            else
            {
                dataGridView5.Rows.Insert(dataGridView5.SelectedRows[0].Index, dr);
            }
            //for (int i = 1; i < dataGridView5.RowCount + 1; i++)
            //    dataGridView5.Rows[i - 1].Cells[0].Value = i;
        }

        //(界面5)
        private void button_新增圆形区域_Click(object sender, EventArgs e)
        {
            DataGridViewRow dr = new DataGridViewRow();
            dr.CreateCells(dataGridView6);
            dr.Cells[0].Value = "";
            dr.Cells[1].Value = 0.0;
            dr.Cells[2].Value = 0.0;
            dr.Cells[3].Value = 0.0;
            dr.Cells[4].Value = 0.0;
            dr.Cells[5].Value = 0.0;
            dr.Cells[6].Value = 0.0;

            if (dataGridView6.SelectedRows.Count != 1)
            {
                dataGridView6.Rows.Insert(dataGridView6.RowCount, dr);
            }
            else
            {
                dataGridView6.Rows.Insert(dataGridView6.SelectedRows[0].Index, dr);
            }
            //for (int i = 1; i < dataGridView6.RowCount + 1; i++)
            //    dataGridView6.Rows[i - 1].Cells[0].Value = i;
        }

        //(界面5)
        private void button_人员分布_Click(object sender, EventArgs e)
        {
            if (dataGridView5.Rows.Count > 0 || dataGridView6.Rows.Count > 0)
            {
                View4Flag = true;
                if (0 != dataGridView5.Rows.Count)
                {
                    for (int i = 0; i < dataGridView5.Rows.Count; i++)
                    {
                        InputAreaRectangle area = new InputAreaRectangle();
                        area.x1 = double.Parse(dataGridView5.Rows[i].Cells[1].Value.ToString());
                        area.y1 = double.Parse(dataGridView5.Rows[i].Cells[2].Value.ToString());
                        area.x2 = double.Parse(dataGridView5.Rows[i].Cells[3].Value.ToString());
                        area.y2 = double.Parse(dataGridView5.Rows[i].Cells[4].Value.ToString());
                        area.h1 = double.Parse(dataGridView5.Rows[i].Cells[5].Value.ToString());
                        area.h2 = double.Parse(dataGridView5.Rows[i].Cells[6].Value.ToString());
                        area.pz = double.Parse(dataGridView5.Rows[i].Cells[7].Value.ToString());
                        if (area.x1 > area.x2)
                        {
                            double tempX = area.x1;
                            area.x1 = area.x2;
                            area.x2 = tempX;
                            double tempY = area.y1;
                            area.y1 = area.y2;
                            area.y2 = tempY;
                        }
                        if (area.h1 > area.h2)
                        {
                            double tempH = area.h1;
                            area.h1 = area.h2;
                            area.h2 = tempH;
                        }
                        Pzr.Add(area);
                    }
                }
                if (0 != dataGridView6.Rows.Count)
                {
                    for (int i = 0; i < dataGridView6.Rows.Count; i++)
                    {
                        InputAreaCircle area =new InputAreaCircle();
                        area.x1 = double.Parse(dataGridView6.Rows[i].Cells[1].Value.ToString());
                        area.y1 = double.Parse(dataGridView6.Rows[i].Cells[2].Value.ToString());
                        area.r = double.Parse(dataGridView6.Rows[i].Cells[3].Value.ToString());
                        area.h1 = double.Parse(dataGridView6.Rows[i].Cells[4].Value.ToString());
                        area.h2 = double.Parse(dataGridView6.Rows[i].Cells[5].Value.ToString());
                        area.pz = double.Parse(dataGridView6.Rows[i].Cells[6].Value.ToString());
                        if (area.h1 > area.h2)
                        {
                            double tempH = area.h1;
                            area.h1 = area.h2;
                            area.h2 = tempH;
                        }
                        Pzc.Add(area);
                    }
                }
            }
        }


        private double integralFun(double x)
        {
            double f = Math.Exp(-x * x / 2);
            return f;
        }

        private double CalculatePdi(double a)
        {
            int M = 1, N = 1, k = 1, m = 1;
            double b, ep, I, h;
            b = 0.0;
            ep = 0.00001;
            h = b - a;
            I = h * (integralFun(a) + integralFun(b)) / 2;
            double[,] T = new double[50, 50];
            T[1, 1] = I;
            while (1 > 0)
            {
                N = (int)Math.Pow(2, m - 1);
                if (N > 50)
                {
                    //MessageBox.Show("请缩小精度再计算!");
                    break;
                }
                else
                {
                    h = h / 2;
                    I = I / 2;
                    for (int i = 1; i <= N; i++)
                        I = I + h * integralFun(a + (2 * i - 1) * h);
                    T[m + 1, 1] = I;
                    M = 2 * N;
                    k = 1;
                    while (M > 1)
                    {
                        T[m + 1, k + 1] = (Math.Pow(4, k) * T[m + 1, k] - T[m, k]) / (Math.Pow(4, k) - 1);
                        M = M / 2;
                        k = k + 1;
                    }
                    if (Math.Abs(T[k, k] - T[k - 1, k - 1]) < ep)
                        break;
                    m = m + 1;
                }
            }
            I = T[k, k];
            //textBoxResult.Text = Convert.ToString(Math.Round(T[k, k], 15));
            return Math.Round(T[k, k], 15);
        }

        private double CheckFileNameReturnNum(string fileName)
        {
            if (Pw.Count != 0)
            {
                //8风向
                if (8 == Pw.Count)
                {
                    if (fileName.Contains("正北"))
                    {
                        return (Pw[0]);
                    }
                    else if (fileName.Contains("东北"))
                    {
                        return (Pw[1]);
                    }
                    else if (fileName.Contains("正东"))
                    {
                        return (Pw[2]);
                    }
                    else if (fileName.Contains("东南"))
                    {
                        return (Pw[3]);
                    }
                    else if (fileName.Contains("正南"))
                    {
                        return (Pw[4]);
                    }
                    else if (fileName.Contains("西南"))
                    {
                        return (Pw[5]);
                    }
                    else if (fileName.Contains("正西"))
                    {
                        return (Pw[6]);
                    }
                    else if (fileName.Contains("西北"))
                    {
                        return (Pw[7]);
                    }
                }
                //16风向
                else if (16 == Pw.Count)
                {
                    if (fileName.Contains("正北"))
                    {
                        return (Pw[0]);
                    }
                    else if (fileName.Contains("北东北"))
                    {
                        return (Pw[1]);
                    }
                    else if (fileName.Contains("东北"))
                    {
                        return (Pw[2]);
                    }
                    else if (fileName.Contains("东东北"))
                    {
                        return (Pw[3]);
                    }
                    else if (fileName.Contains("正东"))
                    {
                        return (Pw[4]);
                    }
                    else if (fileName.Contains("东东南"))
                    {
                        return (Pw[5]);
                    }
                    else if (fileName.Contains("东南"))
                    {
                        return (Pw[6]);
                    }
                    else if (fileName.Contains("南东南"))
                    {
                        return (Pw[7]);
                    }
                    else if (fileName.Contains("正南"))
                    {
                        return (Pw[8]);
                    }
                    else if (fileName.Contains("南西南"))
                    {
                        return (Pw[9]);
                    }
                    else if (fileName.Contains("西南"))
                    {
                        return (Pw[10]);
                    }
                    else if (fileName.Contains("西西南"))
                    {
                        return (Pw[11]);
                    }
                    else if (fileName.Contains("正西"))
                    {
                        return (Pw[12]);
                    }
                    else if (fileName.Contains("西西北"))
                    {
                        return (Pw[13]);
                    }
                    else if (fileName.Contains("西北"))
                    {
                        return (Pw[14]);
                    }
                    else if (fileName.Contains("北西北"))
                    {
                        return (Pw[15]);
                    }
                }
            }
            return -1;
        }

        private FFile CalculateYi(FFile dataFile)
        {
            FFile dataF = dataFile;
            dataF.value_double_List = new List<double>();
            int totalNum = dataF.XNum * dataF.YNum * dataF.ZNum;
            double quotiety_d = 1 / Math.Sqrt(2 * Math.PI);
            //热辐射文件是.nq
            if (dataF.fileName.Contains(".nqradmax") || dataF.fileName.Contains(".NQRADMAX"))
            {
                //-14.9+2.56ln
                for (int k = 0; k < totalNum; k++)
                {
                    double temp_d = Convert.ToDouble(dataF.value_List[k]);
                    if (temp_d < 0)
                    {
                        temp_d = Math.Abs(temp_d);
                    }
                    else
                    {
                        double value_d = CalculatePdi(-14.9 + 2.56 * System.Math.Log(temp_d));
                        value_d = 1.2533 - quotiety_d * value_d;
                        value_d = value_d * CheckFileNameReturnNum(dataF.fileName);
                        dataF.value_double_List.Add(value_d);
                        //dataF.value_double_List.Add(0.0);
                    }
                }
            }
            //爆炸超压文件是.np
            else if (dataF.fileName.Contains(".npmax") || dataF.fileName.Contains(".NPMAX"))
            {
                //-77.1+6.91ln
                for (int k = 0; k < totalNum; k++)
                {
                    double temp_d = Convert.ToDouble(dataF.value_List[k]);
                    if (temp_d < 0)
                    {
                        temp_d = Math.Abs(temp_d);
                    }
                    else
                    {
                        temp_d = temp_d * 100000;
                        double value_d = CalculatePdi(-77.1 + 6.91 * System.Math.Log(temp_d));
                        value_d = 1.2533 - quotiety_d * value_d;
                        value_d = value_d * CheckFileNameReturnNum(dataF.fileName);
                        dataF.value_double_List.Add(value_d);
                        //dataF.value_double_List.Add(0.0);
                    }
                }
            }
            //压力脉冲文件是.nj
            else if (dataF.fileName.Contains(".npimpmax") || dataF.fileName.Contains(".NPIMPMAX"))
            {
                //-46.1+4.82ln
                for (int k = 0; k < totalNum; k++)
                {
                    double temp_d = Convert.ToDouble(dataF.value_List[k]);
                    if (temp_d < 0)
                    {
                        temp_d = Math.Abs(temp_d);
                    }
                    double value_d = CalculatePdi(-46.1 + 4.82 * System.Math.Log(temp_d));
                    value_d = 1.2533 - quotiety_d * value_d;
                    value_d = value_d * CheckFileNameReturnNum(dataF.fileName);
                    dataF.value_double_List.Add(value_d);
                    //dataF.value_double_List.Add(0.0);
                }
            }

            return dataF;
        }

        private Decimal ChangeDataToD(string dataStr)
        {
            Decimal dData = 0.0M;
            if (dataStr.Contains("E"))
            {
                dData = Convert.ToDecimal(Decimal.Parse(dataStr.ToString(), System.Globalization.NumberStyles.Float));
            }
            return dData;
        }

        private void CheckFileNameReturnWindSpeedNum(string fileName)
        {
            string[] stringArray = fileName.Split('-');
            if (stringArray.Length > 0)
            {
                WindSpeed = double.Parse(stringArray[2]);
                if (" 八个风向风速联合频率表" == label34.Text)
                {
                    int rowIndex = this.dataGridView3.Rows.Count;
                    if (rowIndex > 0)
                    {
                        View2Flag = true;
                        for (int i = 0; i < dataGridView3.Rows.Count; i++)
                        {
                            if (WindSpeed == double.Parse(dataGridView3.Rows[i].Cells[0].Value.ToString()))
                            {
                                rowIndex = i;
                                for (int j = 1; j < 9; j++)
                                    Pw.Add(double.Parse(dataGridView3.Rows[rowIndex].Cells[j].Value.ToString()));
                            }
                        }
                        //MessageBox.Show("选择了风速为: " + WindSpeed + " 的数据！", "提醒", MessageBoxButtons.OK);
                    }
                    else
                    {
                        View2Flag = false;
                        MessageBox.Show("请检查导入的结果文件命名！", "提醒", MessageBoxButtons.OK);
                    }
                }
                else if ("十六个风向风速联合频率表" == label34.Text)
                {
                    int rowIndex = this.dataGridView4.Rows.Count;
                    if (rowIndex > 0)
                    {
                        View2Flag = true;
                        for (int i = 0; i < dataGridView4.Rows.Count; i++)
                        {
                            if (WindSpeed == double.Parse(dataGridView4.Rows[i].Cells[0].Value.ToString()))
                            {
                                rowIndex = i;
                                for (int j = 1; j < 17; j++)
                                    Pw.Add(double.Parse(dataGridView4.Rows[rowIndex].Cells[j].Value.ToString()));
                            }
                        }
                        //MessageBox.Show("选择了风速为: " + WindSpeed + " 的数据！", "提醒", MessageBoxButtons.OK);
                    }
                    else
                    {
                        View2Flag = false;
                        MessageBox.Show("请检查导入的结果文件命名！", "提醒", MessageBoxButtons.OK);
                    }
                }
            }
        }

        //List<FFile> npData;
        public void ReadNPFile(string fileName, string filePathStr)
        {
            FFile dataFile = new FFile();
            dataFile.fileName = fileName;

            StreamReader sr = new StreamReader(filePathStr, Encoding.Default);
            //List<Decimal> listRecord = new List<Decimal>();
            for (int i = 0; i < 4; i++)
                sr.ReadLine();
            //读第5行数据
            String tempData = sr.ReadLine();
            String[] tempArray = tempData.Split(' ');
            int[] temp = new int[6];
            int k = 0;
            for (int i = 0; i < tempArray.Length; i++)
            {
                if (!string.IsNullOrEmpty(tempArray[i]))
                {
                    temp[k] = int.Parse(tempArray[i]);
                    k++;
                }
            }
            dataFile.XNum = temp[1] - temp[0] + 1;
            dataFile.YNum = temp[3] - temp[2] + 1;
            dataFile.ZNum = temp[5] - temp[4] + 1;
            dataFile.totalSize = dataFile.XNum * dataFile.YNum * dataFile.ZNum;

            dataFile.valueX_List = new List<Decimal>();
            dataFile.valueY_List = new List<Decimal>();
            dataFile.valueZ_List = new List<Decimal>();
            //读第6行数据X
            tempData = sr.ReadLine();
            String lineX = "";
            int XNum = 0;
            if (dataFile.XNum % 5 == 0)
                XNum = dataFile.XNum / 5;
            else XNum = dataFile.XNum / 5 + 1;
            for (int m = 0; m < XNum; m++)
            {
                lineX = sr.ReadLine();
                string[] lineArray = lineX.Split(' ');
                for (int i = 1; i < lineArray.Length; i++)
                {
                    if (!string.IsNullOrEmpty(lineArray[i]))
                        dataFile.valueX_List.Add(ChangeDataToD(lineArray[i]));
                }
            }
            //读数据Y
            tempData = sr.ReadLine();
            String lineY = "";
            int YNum = 0;
            if (dataFile.YNum % 5 == 0)
                YNum = dataFile.YNum / 5;
            else YNum = dataFile.YNum / 5 + 1;
            for (int m = 0; m < YNum; m++)
            {
                lineY = sr.ReadLine();
                string[] lineArray = lineY.Split(' ');
                for (int i = 1; i < lineArray.Length; i++)
                {
                    if (!string.IsNullOrEmpty(lineArray[i]))
                        dataFile.valueY_List.Add(ChangeDataToD(lineArray[i]));
                }
            }
            //读数据Z
            tempData = sr.ReadLine();
            String lineZ = "";
            int ZNum = 0;
            if (dataFile.ZNum % 5 == 0)
                ZNum = dataFile.ZNum / 5;
            else ZNum = dataFile.ZNum / 5 + 1;
            for (int m = 0; m < ZNum; m++)
            {
                lineZ = sr.ReadLine();
                string[] lineArray = lineZ.Split(' ');
                for (int i = 1; i < lineArray.Length; i++)
                {
                    if (!string.IsNullOrEmpty(lineArray[i]))
                        dataFile.valueZ_List.Add(ChangeDataToD(lineArray[i]));
                }
            }

            //读数据TIME
            tempData = sr.ReadLine();
            tempData = sr.ReadLine();
            String[] tempArray1 = tempData.Split(' ');
            dataFile.bombTime = ChangeDataToD(tempArray1[1]);

            String line = "";
            dataFile.value_List = new List<Decimal>();
            while ((line = sr.ReadLine()) != null)
            {
                string[] lineArray = line.Split(' ');
                for (int i = 1; i < lineArray.Length; i++)
                {
                    //listRecord.Add(ChangeDataToD(lineArray[i]));
                    if (!string.IsNullOrEmpty(lineArray[i]))
                        dataFile.value_List.Add(ChangeDataToD(lineArray[i]));
                }
            }
            dataFile = CalculateYi(dataFile);
            npData.Add(dataFile);
            //return listRecord;
        }

        private string[] fileNames;
        //打开F文件
        //热辐射文件是.nq
        //爆炸超压文件是.np
        //压力脉冲文件是.nj
        //(界面6)
        private void button7_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "热辐射文件|*.nqradmax|爆炸超压文件|*.npmax|压力脉冲文件|*.npimpmax|所有文件|*.*";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                MessageBox.Show("导入过程需要一定的时间，请耐心等待！", "提示", MessageBoxButtons.OK);
                string selectfile = ofd.FileName;
                fileNames = ofd.FileNames;
                string[] pathArray = selectfile.Split(Path.DirectorySeparatorChar);
                FileInfo finfo = new FileInfo(selectfile);
                string filePath = finfo.Directory.ToString() + @"\" + finfo.Name;
                label37.Text = "文件路径：" + filePath;
                int fileNum = fileNames.Length;
                for (int i = 0; i < fileNum; i++)
                {
                    pathArray = fileNames[i].Split(Path.DirectorySeparatorChar);
                    int strLength = pathArray.Length;
                    if (0 == i)
                    {
                        CheckFileNameReturnWindSpeedNum((pathArray[strLength - 1]));
                    }
                    ReadNPFile((pathArray[strLength - 1]), fileNames[i]);
                }
                this.button7.Enabled = false;
                this.btn_View6_Calculate.Enabled = true;
                View5Flag = true;
                MessageBox.Show("导入F文件数据成功！", "提示", MessageBoxButtons.OK);
            }
            ofd.Dispose();
        }


        public Microsoft.Office.Interop.Excel.Application app;
        public Microsoft.Office.Interop.Excel.Workbooks wbs;
        public Microsoft.Office.Interop.Excel.Workbook wb;
        public Microsoft.Office.Interop.Excel.Worksheet ws;

        public void OpenExcel(string filePath)
        {
            try
            {
                app = new Microsoft.Office.Interop.Excel.Application();
                wb = app.Workbooks.Add(Type.Missing);
                ws = wb.Worksheets[1] as Microsoft.Office.Interop.Excel.Worksheet;
                ws.SaveAs(filePath, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                wb.Save();
                //app.Visible = false;

                wbs = app.Workbooks;
                //wb = wbs.Add(filePath);
                wb = wbs.Open(filePath, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            }
            catch 
            {
            }
        }

        public Microsoft.Office.Interop.Excel.Range GetSheet(string sheetName, string cellsName)
        {
            Microsoft.Office.Interop.Excel.Range r = ((Microsoft.Office.Interop.Excel.Worksheet)wb.Worksheets[sheetName]).get_Range(cellsName, Type.Missing);
            return r;
        }

        public void SetSheetValue(string sheetName, string cellsName, object cellsValue)
        {
            Microsoft.Office.Interop.Excel.Range r = ((Microsoft.Office.Interop.Excel.Worksheet)wb.Worksheets[sheetName]).get_Range(cellsName, Type.Missing);
            r.Value = cellsValue;
        }

        public Microsoft.Office.Interop.Excel.Worksheet AddSheet(string sheetName)
        {
            Microsoft.Office.Interop.Excel.Worksheet s = (Microsoft.Office.Interop.Excel.Worksheet)wb.Worksheets.Add(Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            s.Name = sheetName;
            return s;
        }

        public void DeleteSheet(string sheetName)
        {
            ((Microsoft.Office.Interop.Excel.Worksheet)wb.Worksheets[sheetName]).Delete();
        }

        public Boolean SaveExcel()
        {
            try
            {
                wb.Save();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void CloseExcel()
        {
            wb.Close(Type.Missing, Type.Missing, Type.Missing);
            wbs.Close();
            app.Quit();
            wb = null;
            wbs = null;
            app = null;
            //GC.Collect();
        }

        //(界面6)
        private void btn_View6_CheckValue_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(this.txt_View6_XNum.Text))
            {
                if (!string.IsNullOrEmpty(this.txt_View6_YNum.Text))
                {
                    if (!string.IsNullOrEmpty(this.txt_View6_ZNum.Text))
                    {
                        this.txt_View6_Value.Text = "";
                        if (npData.Count > 0)
                        {
                            double xP = double.Parse(this.txt_View6_XNum.Text);
                            double yP = double.Parse(this.txt_View6_YNum.Text);
                            double zP = double.Parse(this.txt_View6_ZNum.Text);
                            int xNum = -1;
                            int yNum = -1;
                            int zNum = -1;
                            for (int i = 0; i < npData[0].valueX_List.Count; i++)
                            {
                                if (double.Parse(npData[0].valueX_List[i].ToString()) == xP)
                                {
                                    xNum = i;
                                    break;
                                }
                            }
                            for (int j = 0; j < npData[0].valueY_List.Count; j++)
                            {
                                if (double.Parse(npData[0].valueY_List[j].ToString()) == yP)
                                {
                                    yNum = j;
                                    break;
                                }
                            }
                            for (int k = 0; k < npData[0].valueZ_List.Count; k++)
                            {
                                if (double.Parse(npData[0].valueZ_List[k].ToString()) == zP)
                                {
                                    zNum = k;
                                    break;
                                }
                            }
                            if (xNum > -1 && yNum > -1 && zNum > -1)
                            {
                                this.txt_View6_Value.Text = npData[0].value_double_List[(xNum + 1) * (yNum + 1) * (zNum + 1) - 1].ToString();
                            }
                            else
                            {
                                this.txt_View6_Value.Text = "0";
                                MessageBox.Show("未找到该坐标处的风险数值！", "提醒", MessageBoxButtons.OK);
                            }
                        }
                        else
                        {
                            MessageBox.Show("请先导入结果文件，计算后再进行查询！", "提醒", MessageBoxButtons.OK);
                        }
                    }
                    else
                    {
                        this.txt_View6_Value.Text = "";
                        MessageBox.Show("请输入Z坐标再进行查询！", "提醒", MessageBoxButtons.OK);
                    }
                }
                else
                {
                    this.txt_View6_Value.Text = "";
                    MessageBox.Show("请输入Y坐标再进行查询！", "提醒", MessageBoxButtons.OK);
                }
            }
            else
            {
                this.txt_View6_Value.Text = "";
                MessageBox.Show("请输入X坐标再进行查询！", "提醒", MessageBoxButtons.OK);
            }
        }

        //(界面6)
        private void btn_View6_Save_XLS_Click(object sender, EventArgs e)
        {
            string filePath = System.Windows.Forms.Application.StartupPath + "\\风险数据.xlsx";
            //File.Create(filePath);
            if (npData.Count > 0)
            {
                MessageBox.Show("数据保存过程需要较长时间，请耐心等待！", "提示", MessageBoxButtons.OK);
                OpenExcel(filePath);

                FFile dataF = npData[0];
                //int totalNum = dataF.XNum * dataF.YNum * dataF.ZNum;
                int totalNum = 0;
                for (int k = 0; k < dataF.ZNum; k++) 
                {
                    for (int j = 0; j < dataF.YNum; j++)
                    {
                        for (int i = 0; i < dataF.XNum; i++)
                        {
                            SetSheetValue("Sheet1", "A" + (totalNum + 1), dataF.valueX_List[i]);
                            SetSheetValue("Sheet1", "B" + (totalNum + 1), dataF.valueY_List[j]);
                            SetSheetValue("Sheet1", "C" + (totalNum + 1), dataF.valueZ_List[k]);
                            SetSheetValue("Sheet1", "D" + (totalNum + 1), dataF.value_double_List[totalNum]);
                            totalNum++;
                        }
                    }
                }
                SaveExcel();
                CloseExcel();
                btn_View6_Save_XLS.Enabled = false;
                btn_View6_Draw_Matlab.Enabled = true;
                MessageBox.Show("数据成功保存到 风险数据.xlsx！", "提示", MessageBoxButtons.OK);
            }
            else
                MessageBox.Show("请先打开输入的风险文件！", "提醒", MessageBoxButtons.OK);
        }

        private void 保存计算结果到ExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string filePath = System.Windows.Forms.Application.StartupPath + "\\风险数据.xlsx";
            //File.Create(filePath);
            if (npData.Count > 0)
            {
                MessageBox.Show("数据保存过程需要较长时间，请耐心等待！", "提示", MessageBoxButtons.OK);
                OpenExcel(filePath);

                FFile dataF = npData[0];
                //int totalNum = dataF.XNum * dataF.YNum * dataF.ZNum;
                int totalNum = 0;
                for (int k = 0; k < dataF.ZNum; k++)
                {
                    for (int j = 0; j < dataF.YNum; j++)
                    {
                        for (int i = 0; i < dataF.XNum; i++)
                        {
                            SetSheetValue("Sheet1", "A" + (totalNum + 1), dataF.valueX_List[i]);
                            SetSheetValue("Sheet1", "B" + (totalNum + 1), dataF.valueY_List[j]);
                            SetSheetValue("Sheet1", "C" + (totalNum + 1), dataF.valueZ_List[k]);
                            SetSheetValue("Sheet1", "D" + (totalNum + 1), dataF.value_double_List[totalNum]);
                            totalNum++;
                        }
                    }
                }
                SaveExcel();
                CloseExcel();
                btn_View6_Save_XLS.Enabled = false;
                btn_View6_Draw_Matlab.Enabled = true;
                MessageBox.Show("数据成功保存到 风险数据.xlsx！", "提示", MessageBoxButtons.OK);
            }
            else
                MessageBox.Show("请先打开输入的风险文件！", "提醒", MessageBoxButtons.OK);
        }

        //(界面6)
        private void btn_View6_Draw_Matlab_Click(object sender, EventArgs e)
        {

            DrawPointDemo.DrawPointClass drawPoint = new DrawPointDemo.DrawPointClass();
            double[] xArray = new double[npData[0].totalSize];
            for (int i = 0; i < npData[0].totalSize; i++)
            {
                xArray[i] = double.Parse(npData[0].valueX_List[i % npData[0].XNum].ToString());
            }
            double[] yArray = new double[npData[0].totalSize];
            for (int i = 0; i < npData[0].totalSize; i++)
            {
                int pos = i / npData[0].XNum;
                if (pos >= npData[0].YNum)
                {
                    pos = pos % npData[0].YNum;
                }
                yArray[i] = double.Parse(npData[0].valueY_List[pos].ToString());
            }
            double[] zArray = new double[npData[0].totalSize];
            for (int i = 0; i < npData[0].totalSize; i++)
            {
                int pos = i / (npData[0].XNum * npData[0].YNum);
                zArray[i] = double.Parse(npData[0].valueZ_List[pos].ToString());
            }
            double[] vArray = new double[npData[0].totalSize];
            for (int i = 0; i < npData[0].totalSize; i++)
            {
                vArray[i] = npData[0].value_double_List[i];
            }
            drawPoint.DrawPointDemo((MathWorks.MATLAB.NET.Arrays.MWNumericArray)xArray, (MathWorks.MATLAB.NET.Arrays.MWNumericArray)yArray, (MathWorks.MATLAB.NET.Arrays.MWNumericArray)zArray, (MathWorks.MATLAB.NET.Arrays.MWNumericArray)vArray);
        }

        private void 通过Matlab绘图ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DrawPointDemo.DrawPointClass drawPoint = new DrawPointDemo.DrawPointClass();
            double[] xArray = new double[npData[0].totalSize];
            for (int i = 0; i < npData[0].totalSize; i++)
            {
                xArray[i] = double.Parse(npData[0].valueX_List[i % npData[0].XNum].ToString());
            }
            double[] yArray = new double[npData[0].totalSize];
            for (int i = 0; i < npData[0].totalSize; i++)
            {
                int pos = i / npData[0].XNum;
                if (pos >= npData[0].YNum)
                {
                    pos = pos % npData[0].YNum;
                }
                yArray[i] = double.Parse(npData[0].valueY_List[pos].ToString());
            }
            double[] zArray = new double[npData[0].totalSize];
            for (int i = 0; i < npData[0].totalSize; i++)
            {
                int pos = i / (npData[0].XNum * npData[0].YNum);
                zArray[i] = double.Parse(npData[0].valueZ_List[pos].ToString());
            }
            double[] vArray = new double[npData[0].totalSize];
            for (int i = 0; i < npData[0].totalSize; i++)
            {
                vArray[i] = npData[0].value_double_List[i];
            }
            drawPoint.DrawPointDemo((MathWorks.MATLAB.NET.Arrays.MWNumericArray)xArray, (MathWorks.MATLAB.NET.Arrays.MWNumericArray)yArray, (MathWorks.MATLAB.NET.Arrays.MWNumericArray)zArray, (MathWorks.MATLAB.NET.Arrays.MWNumericArray)vArray);
        }


        private Boolean View1Flag;
        private int EDEquipID;
        private double EquipFrequent;

        private Boolean View2Flag;
        //风向风速
        private double WindSpeed;
        private int WindGrounpNum;
        private List<double> Pw;

        private Boolean View3Flag;
        //事件概率
        private List<double> Pi;

        private Boolean View4Flag;
        //人员分布
        private List<InputAreaRectangle> Pzr;
        private List<InputAreaCircle> Pzc;
        List<FFile> npData;
        private Boolean View5Flag;

        //Final
        private void 计算风险分布ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (View1Flag && View2Flag && View3Flag && View4Flag && View5Flag)
            {
                MessageBox.Show("计算过程需要一定的时间，请耐心等待几分钟！", "提示", MessageBoxButtons.OK);
                //Form3 Frm = new Form3();
                //Frm.Show();
                for (int i = 0; i < npData.Count; i++)
                {
                    for (int k = 0; k < npData[i].valueZ_List.Count; k++)
                    {
                        for (int n = 0; n < npData[i].valueY_List.Count; n++)
                        {
                            for (int m = 0; m < npData[i].valueX_List.Count; m++)
                            {
                                for (int r = 0; r < Pzr.Count; r++)
                                {
                                    if (Pzr[r].isPointInRectangle(Convert.ToDouble(npData[i].valueX_List[m]), Convert.ToDouble(npData[i].valueY_List[n]), Convert.ToDouble(npData[i].valueZ_List[k])))
                                    {
                                        if (npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] > 0)
                                        {
                                            //npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] = EquipFrequent * Pzr[r].pz * npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1];
                                            npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] = Pzr[r].pz * npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1];
                                            if (i > 0)
                                            {
                                                npData[0].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] += npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1];
                                            }
                                        }
                                    }
                                }
                                for (int c = 0; c < Pzc.Count; c++)
                                {
                                    if (Pzc[c].isPointInCircle(Convert.ToDouble(npData[i].valueX_List[m]), Convert.ToDouble(npData[i].valueY_List[n]), Convert.ToDouble(npData[i].valueZ_List[k])))
                                    {
                                        if (npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] > 0)
                                        {
                                            //npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] = EquipFrequent * Pzr[r].pz * npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1];
                                            npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] = Pzc[c].pz * npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1];
                                            if (i > 0)
                                            {
                                                npData[0].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] += npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1];
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                this.toolBarLabel_State.Text = "风险显示";
                this.btn_View6_Calculate.Enabled = false;
                this.btn_View6_CheckValue.Enabled = true;
                this.btn_View6_Save_XLS.Enabled = true;
                this.计算风险分布ToolStripMenuItem.Enabled = false;
                this.保存计算结果到ExcelToolStripMenuItem.Enabled = true;
                MessageBox.Show("计算已完成！", "提示", MessageBoxButtons.OK);
            }
            if (!View1Flag)
            {
                MessageBox.Show("请选择一个设备并输入失效频率！", "提醒", MessageBoxButtons.OK);
            }
            if (!View2Flag)
            {
                MessageBox.Show("请选择一行风速风向数据！", "提醒", MessageBoxButtons.OK);
            }
            if (!View3Flag)
            {
                MessageBox.Show("请选择一种事故类型！", "提醒", MessageBoxButtons.OK);
            }
            if (!View4Flag)
            {
                MessageBox.Show("请选择至少一个区域的人员分布概率！", "提醒", MessageBoxButtons.OK);
            }
            if (!View5Flag)
            {
                MessageBox.Show("请先导入一组F文件作为输入！", "提醒", MessageBoxButtons.OK);
            }
            //this.Hide();
        }

        //(界面6)
        private void btn_View6_Calculate_Click(object sender, EventArgs e)
        {
            if (View1Flag && View2Flag && View3Flag && View4Flag && View5Flag)
            {
                MessageBox.Show("计算过程需要一定的时间，请耐心等待几分钟！", "提示", MessageBoxButtons.OK);
                //Form3 Frm = new Form3();
                //Frm.Show();
                for (int i = 0; i < npData.Count; i++)
                {
                    for (int k = 0; k < npData[i].valueZ_List.Count; k++)
                    {
                        for (int n = 0; n < npData[i].valueY_List.Count; n++)
                        {
                            for (int m = 0; m < npData[i].valueX_List.Count; m++)
                            {
                                for (int r = 0; r < Pzr.Count; r++)
                                {
                                    if (Pzr[r].isPointInRectangle(Convert.ToDouble(npData[i].valueX_List[m]), Convert.ToDouble(npData[i].valueY_List[n]), Convert.ToDouble(npData[i].valueZ_List[k])))
                                    {
                                        if (npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] > 0)
                                        {
                                            npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] = EquipFrequent * Pzr[r].pz * npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1];
                                            if (i > 0)
                                            {
                                                npData[0].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] += npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1];
                                            }
                                        }
                                    }
                                }
                                for (int c = 0; c < Pzc.Count; c++)
                                {
                                    if (Pzc[c].isPointInCircle(Convert.ToDouble(npData[i].valueX_List[m]), Convert.ToDouble(npData[i].valueY_List[n]), Convert.ToDouble(npData[i].valueZ_List[k])))
                                    {
                                        if (npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] > 0)
                                        {
                                            npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] = EquipFrequent * Pzc[c].pz * npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1];
                                            if (i > 0)
                                            {
                                                npData[0].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1] += npData[i].value_double_List[(m + 1) * (n + 1) * (k + 1) - 1];
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                this.toolBarLabel_State.Text = "风险显示";
                this.btn_View6_Calculate.Enabled = false;
                this.btn_View6_CheckValue.Enabled = true;
                this.btn_View6_Save_XLS.Enabled = true;
                this.计算风险分布ToolStripMenuItem.Enabled = false;
                this.保存计算结果到ExcelToolStripMenuItem.Enabled = true;
                MessageBox.Show("计算已完成！", "提示", MessageBoxButtons.OK);
            }
            if (!View1Flag)
            {
                MessageBox.Show("请选择一个设备并输入失效频率！", "提醒", MessageBoxButtons.OK);
            }
            if (!View2Flag)
            {
                MessageBox.Show("请选择一行风速风向数据！", "提醒", MessageBoxButtons.OK);
            }
            if (!View3Flag)
            {
                MessageBox.Show("请选择一种事故类型！", "提醒", MessageBoxButtons.OK);
            }
            if (!View4Flag)
            {
                MessageBox.Show("请选择至少一个区域的人员分布概率！", "提醒", MessageBoxButtons.OK);
            }
            if (!View5Flag)
            {
                MessageBox.Show("请先导入一组F文件作为输入！", "提醒", MessageBoxButtons.OK);
            }
            //this.Hide();
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}
