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
    public partial class Form8 : Form
    {
        public int[] EDEquip;
        public int EDEquipNum = 0;
        public int EDEquipID = -1;
        public double EquipFrequent = 0.0;
        public Boolean isChecked = false;

        public Form8(ChoosenumTable[] choosenumtable)
        {
            InitializeComponent();
            DataGridViewTextBoxColumn dc;    
            int i = 1;
            foreach (double onenum in choosenumtable[0].choosenum)
            {
                dc = new DataGridViewTextBoxColumn();
                {
                    dc.HeaderText ="设备序号"+ i;
                    dc.Name = "Column"+i;
                }
                dataGridView1.Columns.Insert( 2+i , dc);
                i++;
            }

             dc = new DataGridViewTextBoxColumn();
            {
                dc.HeaderText = "被选择的设备序号" ;
                dc.Name = "Column" + i;
                dc.Width = 200;
            }
            dataGridView1.Columns.Insert(2 + i, dc);

            int j = 1;
            foreach (ChoosenumTable choosenumrow in choosenumtable)
            {
                DataGridViewRow dr = new DataGridViewRow();
                dr.CreateCells(dataGridView1);
                dr.Cells[0].Value = j;
                dr.Cells[1].Value = choosenumrow.X;
                dr.Cells[2].Value = choosenumrow.Y;
                i = 3;
                foreach (double onenum in choosenumrow.choosenum)
                {
                    dr.Cells[i].Value = onenum;
                    i++;
                }
                dr.Cells[i].Value = choosenumrow.selectedequip;
                dataGridView1.Rows.Insert((j++)-1,dr);
            }

            EDEquip = new int[dataGridView1.RowCount];
            for (int k = 0; k < dataGridView1.RowCount; k++)
                EDEquip[k] = -1;
            for (int m = 0; m < dataGridView1.RowCount - 1; m++)
            {
                string selectEquipStr = dataGridView1.Rows[m].Cells[i].Value.ToString();
                string[] selectEquipArray = selectEquipStr.Split(',');
                Boolean isFindFlag = false;
                for (int n = 0; n < selectEquipArray.Length; n++)
                {
                    isFindFlag = false;
                    int k = 0;
                    for (k = 0; k < EDEquipNum; k++)
                    {
                        if (-1 == EDEquip[k])
                            break;
                        else if (EDEquip[k] == int.Parse(selectEquipArray[n]))
                        {
                            isFindFlag = true;
                            break;
                        }
                    }
                    if (!isFindFlag)
                    {
                        EDEquip[EDEquipNum] = int.Parse(selectEquipArray[n]);
                        EDEquipNum++;
                    }
                }
            }
            MessageBox.Show("共有" + EDEquipNum + "个被选择设备！", "提醒", MessageBoxButtons.OK);
        }

        private void Form8_Load(object sender, EventArgs e)
        {
            dataGridView1.Enabled = true;
            dataGridView1.DefaultCellStyle.Format = "0.00";
            dataGridView1.Columns[0].DefaultCellStyle.Format = "0";
            dataGridView1.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void btn_ChooseDevice_Click(object sender, EventArgs e)
        {
            Boolean isRight = false;
            if (!string.IsNullOrEmpty(txt_DeviceNum.Text))
            {
                EDEquipID = int.Parse(txt_DeviceNum.Text);
                for (int i = 0; i < EDEquipNum; i++)
                {
                    if (EDEquip[i] == int.Parse(txt_DeviceNum.Text.ToString()))
                        isRight = true;
                }
                if (!isRight)
                {
                    isChecked = false;
                    MessageBox.Show("请选择一个计算出来的危险设备！", "提醒", MessageBoxButtons.OK);
                }
                else if (!string.IsNullOrEmpty(this.txt_DeviceFrequent.Text))
                {
                    try
                    {
                        EquipFrequent = double.Parse(this.txt_DeviceFrequent.Text);
                    }
                    catch
                    {
                    }
                    isChecked = true;
                    MessageBox.Show("选择" + txt_DeviceNum.Text.ToString() + "号设备！", "提示", MessageBoxButtons.OK);
                    this.Close();
                }
            }
        }
    }
}
