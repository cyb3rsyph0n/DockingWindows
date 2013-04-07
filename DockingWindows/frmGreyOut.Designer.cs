namespace DockingWindows
{
    internal partial class frmGreyOut
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // frmGreyOut
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmGreyOut";
            this.Opacity = 0.01;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "frmGreyOut";
            this.Shown += new System.EventHandler(this.frmGreyOut_Shown);
            this.VisibleChanged += new System.EventHandler(this.frmGreyOut_VisibleChanged);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.frmGreyOut_MouseDown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmGreyOut_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion
    }
}