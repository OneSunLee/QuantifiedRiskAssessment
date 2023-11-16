using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _3dQRA
{
    public partial class Form16 : Form
    {
        public Form16()
        {
            InitializeComponent();
        }

        private List<String> readFile()
        {
            List<String> listRecord = new List<String>();
            string filePath = System.Windows.Forms.Application.StartupPath + "\\点火源在1min内的点火概率.txt";
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

        private void Form16_Load(object sender, EventArgs e)
        {
            this.dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            DataGridViewRow dr = new DataGridViewRow();
            List<String> fileData = readFile();
            for (int i = 0; i < fileData.Count; i++)
            {
                dr = new DataGridViewRow();
                dr.CreateCells(dataGridView1);
                String[] dataArray = fileData[i].Split('@');
                dr.Cells[0].Value = dataArray[0];
                dr.Cells[1].Value = dataArray[1];
                dr.Cells[2].Value = dataArray[2];
                dataGridView1.Rows.Insert(i, dr);
            }
        }
    }
}
