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
    public partial class Form13 : Form
    {
        public Form13()
        {
            InitializeComponent();
        }

        private List<String> readFile()
        {
            List<String> listRecord = new List<String>();
            string filePath = System.Windows.Forms.Application.StartupPath + "\\换热器的LOC情景.txt";
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

        private void Form13_Load(object sender, EventArgs e)
        {
            this.dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            DataGridViewRow dr = new DataGridViewRow();
            dr = new DataGridViewRow();
            dr.CreateCells(dataGridView1);
            dr.Cells[0].Value = "";
            dr.Cells[1].Value = "危险物在管道外面的换热器";
            dr.Cells[2].Value = "危险物在管道内，外壳设计压力小于危险物压力的换热器";
            dr.Cells[3].Value = "危险物在管道内，外壳设计压力大于危险物压力的换热器";
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
                dataGridView1.Rows.Insert(i+1, dr);
            }
        }
    }
}
