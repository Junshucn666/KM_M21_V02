using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KM_M21
{
    public partial class KM_View : Form
    {
        public KM_View()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
            this.MinimumSize = this.Size;
            this.MaximumSize = this.Size;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        private void KM_View_Load(object sender, EventArgs e)
        {
            this.textBox1.Focus();
            this.textBox1.SelectAll();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            //https://msdn.microsoft.com/zh-cn/library/hkkb40tf
            //this.button_Activate.PerformClick();

            if (this.textBox1.Text == null) return;           
            KM_M21.ScanNum = int.Parse(this.textBox1.Text);
            this.Close();
            this.Dispose();
        }


    }
}
