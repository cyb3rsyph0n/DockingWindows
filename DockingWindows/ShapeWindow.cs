using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DockingWindows
{
    public class ShapeWindow : DockableWindow
    {
        public Bitmap ShapeImage { get; set; }
        public Color TransparentColor { get; set; }
        public Region mRegion = null;

        public ShapeWindow()
        {
            this.FormBorderStyle = FormBorderStyle.None;
        }

        protected override void OnLoad(EventArgs e)
        {
            if (ShapeImage != null)
            {
                this.Size = ShapeImage.Size;

                using (Region tmpRegion = new Region())
                {
                    tmpRegion.MakeEmpty();
                    using (Graphics g = Graphics.FromImage(ShapeImage))
                    {
                        bool isMatch = false;
                        Rectangle tmpRect = new Rectangle(0, 0, 0, 0);

                        for (int x = 0; x < ShapeImage.Width; x++)
                        {
                            for (int y = 0; y < ShapeImage.Height; y++)
                            {
                                if (!isMatch)
                                {
                                    if (ShapeImage.GetPixel(x, y).ToArgb() != TransparentColor.ToArgb())
                                    {
                                        isMatch = true;
                                        tmpRect.X = x;
                                        tmpRect.Y = y;
                                        tmpRect.Width = 1;
                                    }
                                }
                                else
                                {
                                    if (ShapeImage.GetPixel(x, y).ToArgb() == TransparentColor.ToArgb())
                                    {
                                        isMatch = false;
                                        tmpRect.Height = y - tmpRect.Y;
                                        tmpRegion.Union(tmpRect);
                                    }
                                }
                            }
                            if (isMatch)
                            {
                                isMatch = false;
                                tmpRect.Height = ShapeImage.Height - tmpRect.Y;
                                tmpRegion.Union(tmpRect);
                            }
                        }

                        this.Region = tmpRegion.Clone();
                    }
                }
            }
            base.OnLoad(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //base.OnPaint(e);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ShapeWindow
            // 
            this.ClientSize = new System.Drawing.Size(300, 300);
            this.Name = "ShapeWindow";
            this.Load += new System.EventHandler(this.ShapeWindow_Load);
            this.ResumeLayout(false);

        }

        private void ShapeWindow_Load(object sender, EventArgs e)
        {

        }
    }
}
