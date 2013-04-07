using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Tester
{
    public partial class frmMain : Form
    {
        frmHorizontal frmDataGridDock = new frmHorizontal();
        frmVertical frmRightDock = new frmVertical();
        frmVertical frmLeftDock = new frmVertical();
        frmHorizontal frmBottomDock = new frmHorizontal();
        frmHorizontal frmTopDock = new frmHorizontal();
        //frmShape frmRightDock = new frmShape();

        public frmMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            frmDataGridDock.DockedControl = dataGridView1;
            frmBottomDock.DockingPosition = DockingWindows.DockableWindow.DockingPositions.BottomInterior;
            frmLeftDock.DockingPosition = DockingWindows.DockableWindow.DockingPositions.LeftExterior;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (frmTopDock.Visible == false)
                frmTopDock.ShowDialog(this);
            else
                frmTopDock.Hide();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (frmDataGridDock.Visible == false)
                frmDataGridDock.ShowDialog(this);
            else
                frmDataGridDock.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (frmBottomDock.Visible == false)
            {
                frmBottomDock.Show(this);
            }
            else
                frmBottomDock.Hide();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (frmLeftDock.Visible == false)
                frmLeftDock.Show(this);
            else
                frmLeftDock.Hide();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (frmRightDock.Visible == false)
                frmRightDock.Show(this);
            else
                frmRightDock.Hide();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            frmTopDock.ShowDialog(this);
        }
    }
}
