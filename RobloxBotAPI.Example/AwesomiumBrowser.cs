using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobloxBotAPI.Example
{
    public partial class AwesomiumBrowser : Form
    {
        public AwesomiumBrowser()
        {
            InitializeComponent();
        }

        private void writeHTMLToDiskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog di = new SaveFileDialog();
            di.Filter = "HTML File|*.html";
            if(di.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                using (StreamWriter writer = new StreamWriter(di.OpenFile()))
                    writer.Write(webControl1.HTML);
                Process.Start(di.FileName);
            }
        }
    }
}
