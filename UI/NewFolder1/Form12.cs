using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _3dQRA
{
    public partial class Form12 : Form
    {
        public Form12()
        {
            InitializeComponent();
        }

        private List<String> readFile()
        {
            List<String> listRecord = new List<String>();
            string filePath = System.Windows.Forms.Application.StartupPath + "\\泵的LOC情景.txt";
            try
            {
                StreamReader sr = new StreamReader(filePath, Encoding.Default);
                String line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    listRecord.Add(line);
                }
                sr.Close();
            }
            catch 
            {
            }
            return listRecord;
        }

        private void Form12_Load(object sender, EventArgs e)
        {
            this.dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            DataGridViewRow dr = new DataGridViewRow();
            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = "";
            dr.Cells[1].Value = "无多余部件的泵";
            dr.Cells[2].Value = "具有锻钢密封的泵";
            dr.Cells[3].Value = "屏蔽水泵";
            dr.Cells[0].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dr.Cells[1].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dr.Cells[2].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dr.Cells[3].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.Rows.Insert(0, dr);

            List<String> fileData = readFile();
            for (int i = 0; i < fileData.Count; i++)
            {
                dr = new DataGridViewRow();
                dr.CreateCells(dataGridView1);
                String[] dataArray = fileData[i].Split('@');
                dr.Cells[0].Value = dataArray[0];
                dr.Cells[1].Value = dataArray[1];
                dr.Cells[2].Value = dataArray[2];
                dr.Cells[3].Value = dataArray[3];
                dataGridView1.Rows.Insert(i + 1, dr);
            }
        }
    }
}
