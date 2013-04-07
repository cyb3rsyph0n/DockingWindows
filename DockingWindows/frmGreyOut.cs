using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DockingWindows
{
    internal partial class frmGreyOut : Form
    {
        private System.EventHandler moveHandler;

        public DockableWindow Host { get; set; }

        public frmGreyOut(DockableWindow host)
        {
            Host = host;
            InitializeComponent();

            //CREATE AN EVENT HANDLER SO WE CAN REMOVE IT UPON OUR CLOSE
            moveHandler = new System.EventHandler(Parent_Move);
        }

        void Parent_Move(object sender, EventArgs e)
        {
            //MOVE EVERY TIME OUR PARENT MOVES
            this.Location = this.Owner.Location;
            this.Size = this.Owner.Size;
        }

        private void frmGreyOut_Shown(object sender, EventArgs e)
        {
            //ADD THE EVENT HANDLER TO THE PARENT FORM
            this.Owner.Move += moveHandler;
            this.Owner.Resize += moveHandler;
        }

        private void frmGreyOut_VisibleChanged(object sender, EventArgs e)
        {
            //IF WE ARE BEING MADE VISIBLE THEN SETUP OUR SIZE AND MOVE US OVER OUR PARENT
            if (this.Visible)
            {
                this.Location = this.Owner.Location;
                this.Size = this.Owner.Size;
                this.Region = this.Owner.Region;
            }
        }

        private void frmGreyOut_FormClosing(object sender, FormClosingEventArgs e)
        {
            //REMOVE THE EVENT HANDLER BECAUSE WE ARE CLOSING
            this.Owner.Move -= moveHandler;
            this.Owner.Resize -= moveHandler;
        }

        private void frmGreyOut_MouseDown(object sender, MouseEventArgs e)
        {
            //CAUSE A BEEP AND RE-ACTIVATE THE HOST WINDOW
            System.Media.SystemSounds.Beep.Play();
            Host.Activate();
        }
    }
}
