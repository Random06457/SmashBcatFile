using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmahBcatPopupFile
{
    public partial class Form1 : Form
    {
        SmashBcatFile file;
        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                file = new SmashBcatFile(openFileDialog1.FileName);
                InitControls();
            }
        }

        private void InitControls()
        {
            textBox_name1.Text = file.Name1;
            textBox_name2.Text = file.Name2;
            textBox_url.Text = file.Url;
            textBox_text.Clear();

            pictureBox1.Image = file.Img;
            listBox_items.Items.Clear();
            comboBox1.SelectedIndex = 0;
            comboBox1_SelectedIndexChanged(null, null);
            listBox_items.SelectedIndex = 0;

            comboBox1.Enabled = true;
            listBox_items.Enabled = true;
        }

        private void splitPopupFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FolderSelectDialog fbox = new FolderSelectDialog();
                if (fbox.ShowDialog() == DialogResult.OK)
                {
                    SmashBcatFile.SplitFile(openFileDialog1.FileName, fbox.SelectedPath);
                }
            }
        }

        private void listBox_items_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox_items.SelectedIndex != -1)
            {
                var msbt = file.TextFile[comboBox1.SelectedIndex];
                int index = listBox_items.SelectedIndex;
                textBox_text.Text = msbt.TextEntries[index].Value;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != -1)
            {
                int sel = listBox_items.SelectedIndex;
                listBox_items.Items.Clear();
                foreach (var entry in file.TextFile[comboBox1.SelectedIndex].TextEntries)
                    listBox_items.Items.Add(entry.Label);

                listBox_items.SelectedIndex = sel;
            }
        }
    }
}