namespace Tester
{
    partial class frmShape
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmShape));
            this.SuspendLayout();
            // 
            // frmShape
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(353, 390);
            this.DockingPosition = DockingWindows.DockableWindow.DockingPositions.RightExterior;
            this.Name = "frmShape";
            this.ShapeImage = ((System.Drawing.Bitmap)(resources.GetObject("$this.ShapeImage")));
            this.Text = "frmShape";
            this.TransparentColor = System.Drawing.Color.White;
            this.Load += new System.EventHandler(this.frmShape_Load);
            this.ResumeLayout(false);
        }

        #endregion
    }
}