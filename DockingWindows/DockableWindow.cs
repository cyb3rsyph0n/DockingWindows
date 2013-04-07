using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Reflection;

namespace DockingWindows
{
    public class DockableWindow : Form
    {
        private const int CS_DROPSHADOW = 0x00020000;
        private BackgroundWorker mShowAnimations = new BackgroundWorker();
        private BackgroundWorker mHideAnimations = new BackgroundWorker();
        private Size mOriginalSize;
        private bool mAnimating = false;
        private bool mClosing = false;
        private List<frmGreyOut> mBlockers = new List<frmGreyOut>();

        private DockingPositions mDockingPosition = DockingPositions.LeftExterior;
        [Description("Determines where this form should dock to its owner")]
        public DockingPositions DockingPosition
        {
            get
            {
                return mDockingPosition;
            }
            set
            {
                mDockingPosition = value;
                Parent_Move(null, null);
            }
        }

        private Border3DStyle mBorderStyle = Border3DStyle.Flat;
        [Description("Determine border style of the window")]
        public Border3DStyle BorderStyle
        {
            get
            {
                return mBorderStyle;
            }
            set
            {
                mBorderStyle = value;
                this.Invalidate();
            }
        }

        [Description("Should the window create a drop shadow?")]
        public bool DropShadow { get; set; }

        [Description("Forces the window to become modal if it is docked to the top of another window.")]
        public bool ForceModal { get; set; }

        [Description("Duration of Sliding animation sequence.")]
        private double mAnimationDuration = .25;
        public double AnimationDuration
        {
            get
            {
                return mAnimationDuration;
            }
            set
            {
                mAnimationDuration = value;
            }
        }

        [Description("Control to dock to instead of a window")]
        [Browsable(false)]
        public Control DockedControl { get; set; }

        private double mAspectX = 0;
        private double mAspectY = 0;
        [Description("Should window maintain size ratio to owner?  This feature is still quirky.")]
        public bool MaintainAspectRatio { get; set; }

        [DesignOnly(true)]
        [Browsable(false)]
        protected override CreateParams CreateParams
        {
            //CREATE A DROP SHADOW ON THE WINDOW
            get
            {
                CreateParams p = base.CreateParams;
                if (!DesignMode && this.DropShadow)
                    p.ClassStyle |= CS_DROPSHADOW;
                return p;
            }
        }

        [Description("Used to determine opacity of overlay window")]
        private double mModalOpacity = 50;
        public double ModalOpacity
        {
            get
            {
                return mModalOpacity;
            }
            set
            {
                if (value > 0 && value < 100)
                    mModalOpacity = value;
                else
                    if (DesignMode)
                        MessageBox.Show("Values Must be between 1 and 100");
                    else
                        throw (new Exception("Invalid value assignment"));
            }
        }

        [Description("Color to overlay when modal window is displayed")]
        public Color ModalOverlayColor { get; set; }

        public DockableWindow()
        {
            //SETUP EVENTS FOR SHOWING ANIMATION
            mShowAnimations.DoWork += new DoWorkEventHandler(mShowAnimations_DoWork);
            mShowAnimations.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mShowAnimations_RunWorkerCompleted);

            //SETUP EVENTS FOR HIDING ANIMATION
            mHideAnimations.DoWork += new DoWorkEventHandler(mHideAnimations_DoWork);
            mHideAnimations.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mHideAnimations_RunWorkerCompleted);

            //THESE CAUSE CHOPPY ANIMATION SO WE ATTEMPT TO REMOVE NOW PRIOR TO BEING SHOWN
            SetupForm();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            //IF WE ARE CURRENTLY ANIMATING THEN BREAK OUT SO THIS DOES NOT RUN AGAIN
            if (mAnimating)
                return;

            //IF NOT IN A DESIGNER THEN BEGIN
            if (!DesignMode && this.Owner != null)
            {
                if (this.Visible && !mShowAnimations.IsBusy && this.mOriginalSize.IsEmpty)
                {
                    //IF WE ARE SUPPOSED TO FORCE MODAL THEN WE NEED TO DISPLAY THE BLOCKER
                    if (this.ForceModal)
                    {
                        BlockAllOpenWindows();
                        this.Activate();
                    }

                    //IF WE ARE TO MAINTAIN ASPECT RATIO WE NEED TO KEEP TRACK OF WINDOW SIZING IN RELATION TO OUR PARENT
                    if (this.MaintainAspectRatio && mAspectX == 0 && mAspectY == 0)
                    {
                        mAspectX = (double)this.Width / (double)this.Owner.Width;
                        mAspectY = (double)this.Height / (double)this.Owner.Height;
                    }

                    //STORE THE ORIGINAL SIZE THEN SHRINK THIS IN PREP FOR THE ANIMATION
                    UpdateControlAnchors();
                    mOriginalSize = new Size(this.Width, this.Height);

                    //RESET ITS SIZE
                    this.Height = 0;
                    this.Width = 0;

                    //PLAY THE ANIMATION TO SHOW THE WINDOW
                    mShowAnimations.RunWorkerAsync();
                }
                else if (!this.Visible && !mHideAnimations.IsBusy && this.mOriginalSize.IsEmpty)
                {
                    //TAKE NOTE OF THE ORIGINAL SIZE BEFORE CALLING THE HIDE ANIMATION
                    mHideAnimations.RunWorkerAsync();
                }
            }
            base.OnVisibleChanged(e);
        }

        protected override void OnShown(EventArgs e)
        {
            if (!DesignMode)
            {
                //MAKE SURE THE FORM DOES NOT HAVE A BORDER
                SetupForm();
                //IF THIS FORM DOES NOT HAVE AN OWNER THEN THROW AN EXCEPTION ELSE SETUP THE HANDLERS
                if (this.Owner == null)
                    throw new Exception("Owner must be set as part of Show Method");
                else
                {
                    this.Owner.Move += new EventHandler(Parent_Move);
                    this.Owner.Resize += new EventHandler(Parent_Resize);

                    Parent_Move(null, null);
                }
            }
            base.OnShown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //DRAW A BORDER AROUND THIS FORM
            Rectangle tmpRect = new Rectangle();
            int borderWidth = SystemInformation.Border3DSize.Width;
            int borderHeight = SystemInformation.Border3DSize.Height;

            switch (this.DockingPosition)
            {
                case DockingPositions.BottomExterior:
                case DockingPositions.TopInterior:
                    tmpRect = new Rectangle(0, -borderHeight * 2, this.ClientSize.Width, this.ClientSize.Height + (borderHeight * 2));
                    break;
                case DockingPositions.LeftExterior:
                case DockingPositions.RightInterior:
                    tmpRect = new Rectangle(0, 0, this.ClientSize.Width + (borderWidth * 2), this.ClientSize.Height);
                    break;
                case DockingPositions.RightExterior:
                case DockingPositions.LeftInterior:
                    tmpRect = new Rectangle(-borderWidth * 2, 0, this.ClientSize.Width + (borderWidth * 2), this.ClientSize.Height);
                    break;
                case DockingPositions.BottomInterior:
                case DockingPositions.TopExterior:
                    tmpRect = new Rectangle(0, 0, this.ClientSize.Width, this.ClientSize.Height + (borderHeight * 2));
                    break;
            }

            tmpRect.Inflate(-borderWidth, -borderHeight);
            ControlPaint.DrawBorder3D(e.Graphics, tmpRect, this.BorderStyle);

            //CALL BASE ON PAINT
            base.OnPaint(e);
        }

        protected override void OnResize(EventArgs e)
        {
            //INVALIDATE TO ERASE THE BORDER
            this.Invalidate();
            Parent_Move(null, null);

            //CALL THE BASE ON RESIZE
            base.OnResize(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            //DO NOT LET THE WINDOW CLOSE WHILE IT IS ANIMATING
            if (mAnimating || this.Visible)
                e.Cancel = true;

            //IF THE FORM IS STILL VISIBLE IT MEANS IT WAS JUST CLOSED SO HIDE IT FIRST TO PLAY THE ANIMATION
            if (this.Visible == true)
            {
                mClosing = true;
                this.Hide();
            }

            base.OnClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        void Parent_Move(object sender, EventArgs e)
        {
            try
            {
                Point tmpPoint;

                //DETERMINE WHERE TO DOCK THIS WINDOW TO THE OWNER WINDOW
                switch (this.DockingPosition)
                {
                    case DockingPositions.BottomExterior:
                        if (this.DockedControl != null)
                        {
                            //CALCULATE OUR TOP BASED ON THE TOP OF THE CONTROL WE ARE DOCKED TO
                            tmpPoint = new Point(this.DockedControl.Left + (this.DockedControl.Width / 2) - (this.Width / 2), this.DockedControl.Bottom);
                            tmpPoint = this.Owner.PointToScreen(tmpPoint);
                            this.Location = tmpPoint;
                        }
                        else
                        {
                            this.Location = new Point(this.Owner.Left + (this.Owner.Width / 2) - (this.Width / 2), this.Owner.Bottom);
                        }
                        break;
                    case DockingPositions.LeftExterior:
                        tmpPoint = this.Owner.PointToScreen(new Point(0, (this.Owner.ClientRectangle.Height / 2) - (this.ClientRectangle.Height / 2)));
                        tmpPoint.X = this.Owner.Left - this.Width;
                        this.Location = tmpPoint;
                        break;
                    case DockingPositions.LeftInterior:
                        tmpPoint = this.Owner.PointToScreen(new Point(0, (this.Owner.ClientRectangle.Height / 2) - (this.ClientRectangle.Height / 2)));
                        //tmpPoint.X = this.Owner.Left;
                        this.Location = tmpPoint;
                        break;
                    case DockingPositions.RightExterior:
                        tmpPoint = this.Owner.PointToScreen(new Point(0, (this.Owner.ClientRectangle.Height / 2) - (this.ClientRectangle.Height / 2)));
                        tmpPoint.X = this.Owner.Right;
                        this.Location = tmpPoint;
                        break;
                    case DockingPositions.RightInterior:
                        tmpPoint = this.Owner.PointToScreen(new Point(this.Owner.ClientRectangle.Right - this.Width, (this.Owner.ClientRectangle.Height / 2) - (this.ClientRectangle.Height / 2)));
                        this.Location = tmpPoint;
                        break;
                    case DockingPositions.TopInterior:
                        if (this.DockedControl != null)
                        {
                            //CALCULATE OUR TOP BASED ON THE TOP OF THE CONTROL WE ARE DOCKED TO
                            tmpPoint = new Point(this.DockedControl.Left + (this.DockedControl.Width / 2) - (this.Width / 2), this.DockedControl.Top);
                            tmpPoint = this.Owner.PointToScreen(tmpPoint);
                            this.Location = tmpPoint;
                        }
                        else
                        {
                            //CALCULATE OUR TOP BASED ON THE TOP OF THE WINDOW
                            this.Location = new Point(this.Owner.Left + (this.Owner.Width / 2) - (this.Width / 2), this.Owner.PointToScreen(this.Owner.ClientRectangle.Location).Y);
                        }
                        break;
                    case DockingPositions.BottomInterior:
                        if (this.DockedControl != null)
                        {
                            //CALCULATE OUR TOP BASED ON THE TOP OF THE CONTROL WE ARE DOCKED TO
                            tmpPoint = new Point(this.DockedControl.Left + (this.DockedControl.Width / 2) - (this.Width / 2), this.DockedControl.Bottom - this.Height);
                            tmpPoint = this.Owner.PointToScreen(tmpPoint);
                            this.Location = tmpPoint;
                        }
                        else
                        {
                            //CALCULATE OUR TOP BASED ON THE TOP OF THE WINDOW
                            this.Location = new Point(this.Owner.Left + (this.Owner.Width / 2) - (this.Width / 2), this.Owner.PointToScreen(new Point(0,this.Owner.ClientRectangle.Bottom)).Y - this.Height);
                        }
                        break;
                    case DockingPositions.TopExterior:
                        if (this.DockedControl != null)
                        {
                            //CALCULATE OUR TOP BASED ON THE TOP OF THE CONTROL WE ARE DOCKED TO
                            tmpPoint = new Point(this.DockedControl.Left + (this.DockedControl.Width / 2) - (this.Width / 2), this.DockedControl.Top - this.Height);
                            tmpPoint = this.Owner.PointToScreen(tmpPoint);
                            this.Location = tmpPoint;
                        }
                        else
                        {
                            //CALCULATE OUR TOP BASED ON THE TOP OF THE WINDOW
                            this.Location = new Point(this.Owner.Left + (this.Owner.Width / 2) - (this.Width / 2), this.Owner.Top - this.Height);
                        }
                        break;
                }
            }
            catch
            {
                //THIS WAS SIMPLER THEN MAKING SURE IT WAS VISIBLE, NOT MINIMIZED ETC ETC....
            }
        }

        void Parent_Resize(object sender, EventArgs e)
        {
            //IF WE ARE SUPPOSED TO ATTEMPT TO MAINTAIN ASPECT RATIO THEN MONITOR THE RESIZING OF THE PARENT
            if (this.MaintainAspectRatio)
            {
                switch (this.DockingPosition)
                {
                    case DockingPositions.LeftExterior:
                    case DockingPositions.RightExterior:
                        //CALCULATE OUR NEW HEIGHT FROM THE PARENTS NEW HEIGHT
                        this.Height = (int)((double)this.Owner.Height * mAspectY);
                        break;
                    case DockingPositions.BottomExterior:
                    case DockingPositions.TopInterior:
                        //DEPENDING IF WE ARE DOCKED TO A CONTROL OR WINDOW WE NEED TO RESIZE OURSELF TO THE PARENTS SIZE
                        if (this.DockedControl != null)
                            this.Width = (int)((double)this.DockedControl.Width * mAspectX);
                        else
                            this.Width = (int)((double)this.Owner.Width * mAspectX);
                        break;
                }
            }
            Parent_Move(null, null);
        }

        void Parent_Activated(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                System.Media.SystemSounds.Beep.Play();
                this.Activate();
            }
        }

        void mShowAnimations_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //CLEAR THE ORIGINAL SILZE FOR FUTURE ANIMATIONS
            mOriginalSize.Height = 0;
            mOriginalSize.Width = 0;

            //START TRACKING SHOW / HIDE AGAIN
            mAnimating = false;

            //ACTIVATE THE OWNER FORM FOR FOCUS
            this.Activate();
        }

        void mShowAnimations_DoWork(object sender, DoWorkEventArgs e)
        {
            TimeSpan startTime = new TimeSpan(DateTime.Now.Ticks);
            TimeSpan finishTime = startTime.Add(TimeSpan.FromSeconds(this.AnimationDuration));

            //DETERMINE HOW MUCH EACH STEP SHOULD BE DURING THIS ANIMATION
            double xStep = (double)mOriginalSize.Width / (double)(finishTime.Ticks - startTime.Ticks);
            double yStep = (double)mOriginalSize.Height / (double)(finishTime.Ticks - startTime.Ticks);

            Screen currentScreen = null;

            //KEEP TRACK WE ARE CURRENTLY ANIMATING SO WE WILL NOT LAUNCH A NEW ANIMATION
            mAnimating = true;

            try
            {
                switch (this.DockingPosition)
                {
                    case DockingPositions.LeftExterior:
                    case DockingPositions.RightExterior:
                    case DockingPositions.LeftInterior:
                    case DockingPositions.RightInterior:

                        //RESET THE HEIGHT OF THE FORM BECAUSE WE ARE ONLY ANIMATING ITS WIDTH
                        this.Invoke((MethodInvoker)delegate { this.Height = this.mOriginalSize.Height; this.Width = 0; currentScreen = Screen.FromHandle(this.Owner.Handle); });
                        Application.DoEvents();

                        //CREATE THE ANIMATION WHICH APPEARS TO SLIDE OUT OF THE OWNER WINDOW
                        while (finishTime.Ticks > DateTime.Now.Ticks)
                        {
                            int newWidth = (int)((DateTime.Now.Ticks - startTime.Ticks) * xStep);
                            this.Invoke((MethodInvoker)delegate { this.Width = newWidth; });

                            System.Threading.Thread.Sleep(10);
                        }

                        //ADJUST THE SIZE INCASE IT DIDN'T QUITE MAKE IT
                        this.Invoke((MethodInvoker)delegate { this.Height = this.mOriginalSize.Height; this.Width = this.mOriginalSize.Width; });
                        break;
                    case DockingPositions.TopInterior:
                    case DockingPositions.BottomExterior:
                    case DockingPositions.BottomInterior:
                    case DockingPositions.TopExterior:

                        //RESET THE WIDTH OF THE FORM BECAUSE WE ARE ONLY ANIMATING ITS HEIGHT
                        this.Invoke((MethodInvoker)delegate { this.Width = this.mOriginalSize.Width; this.Height = 0; });
                        Application.DoEvents();

                        //CREATE THE ANIMATION WHICH APPEARS TO SLIDE OUT OF THE OWNER WINDOW
                        while (finishTime.Ticks > DateTime.Now.Ticks)
                        {
                            int newHeight = (int)((DateTime.Now.Ticks - startTime.Ticks) * yStep);
                            this.Invoke((MethodInvoker)delegate { this.Height = newHeight; });
                            System.Threading.Thread.Sleep(10);
                        }

                        //ADJUST THE SIZE INCASE IT DIDN'T QUITE MAKE IT
                        this.Invoke((MethodInvoker)delegate { this.Height = this.mOriginalSize.Height; this.Width = this.mOriginalSize.Width; });
                        break;
                }
            }
            catch
            {
            }
        }

        void mHideAnimations_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //RESET WINDOW SIZES IN PREP TO BE SHOWN AGAIN
            this.Visible = false;
            this.Width = mOriginalSize.Width;
            this.Height = mOriginalSize.Height;

            //CLEAN UP VARIABLES
            mOriginalSize.Height = 0;
            mOriginalSize.Width = 0;

            //KEEP TRACK THE ANIMATION IS COMPLETE
            mAnimating = false;

            //ACTIVATE THE OWNER FORM FOR FOCUS
            if (this.Owner != null)
                this.Owner.Activate();

            //IF IT WAS THE USERS INTENTION TO CLOSE THEN FINISH THE CLOSE NOW
            if (mClosing)
                this.Close();

            //IF THE BLOCKER IS VISIBLE THEN HIDE IT
            if (mBlockers.Count != 0)
            {
                for (int i = 0; i < mBlockers.Count; i++)
                {
                    mBlockers[i].Close();
                    mBlockers[i].Dispose();
                    mBlockers[i] = null;
                }
                mBlockers.Clear();
                mBlockers = new List<frmGreyOut>();
            }
        }

        void mHideAnimations_DoWork(object sender, DoWorkEventArgs e)
        {
            TimeSpan startTime = new TimeSpan(DateTime.Now.Ticks);
            TimeSpan finishTime = startTime.Add(TimeSpan.FromSeconds(this.AnimationDuration));

            //STORE THE SIZE OF THE WINDOW
            mOriginalSize = new Size(this.Width, this.Height);

            //DETERMINE HOW MUCH EACH STEP SHOULD BE DURING THIS ANIMATION
            double xStep = (double)mOriginalSize.Width / (double)(finishTime.Ticks - startTime.Ticks);
            double yStep = (double)mOriginalSize.Height / (double)(finishTime.Ticks - startTime.Ticks);

            //KEEP TRACK WE ARE CURRENTLY ANIMATING SO WE WILL NOT LAUNCH A NEW ANIMATION
            mAnimating = true;

            try
            {
                switch (this.DockingPosition)
                {
                    case DockingPositions.LeftExterior:
                    case DockingPositions.RightExterior:
                    case DockingPositions.LeftInterior:
                    case DockingPositions.RightInterior:
                        this.Invoke((MethodInvoker)delegate { this.Height = this.mOriginalSize.Height; this.Show(this.Owner); });

                        //CREATE THE ANIMATION WHICH APPEARS TO SLIDE INTO THE OWNER WINDOW
                        while (finishTime.Ticks > DateTime.Now.Ticks)
                        {
                            int newWidth = (int)((DateTime.Now.Ticks - startTime.Ticks) * xStep);
                            this.Invoke((MethodInvoker)delegate { this.Width = this.mOriginalSize.Width - newWidth; });
                            System.Threading.Thread.Sleep(10);
                        }

                        //ADJUST THE SIZE INCASE IT DIDN'T QUITE MAKE IT
                        this.Invoke((MethodInvoker)delegate { this.Height = 0; this.Width = 0; });
                        break;
                    case DockingPositions.TopInterior:
                    case DockingPositions.BottomExterior:
                    case DockingPositions.BottomInterior:
                    case DockingPositions.TopExterior:
                        this.Invoke((MethodInvoker)delegate { this.Width = mOriginalSize.Width; this.Visible = true; });

                        //CREATE THE ANIMATION WHICH APPEARS TO SLIDE INTO THE OWNER WINDOW
                        while (finishTime.Ticks > DateTime.Now.Ticks)
                        {
                            int newHeight = (int)((DateTime.Now.Ticks - startTime.Ticks) * yStep);
                            this.Invoke((MethodInvoker)delegate { this.Height = this.mOriginalSize.Height - newHeight; });
                            System.Threading.Thread.Sleep(10);
                        }

                        //ADJUST THE SIZE INCASE IT DIDN'T QUITE MAKE IT
                        this.Invoke((MethodInvoker)delegate { this.Height = 0; this.Width = 0; });
                        break;
                }
            }
            catch
            {
            }
        }

        public new DialogResult ShowDialog(IWin32Window Owner)
        {
            //DO A NORMAL HIDE THEN CALL WAIT FOR MODAL TO COMPLETE
            bool tmpForceModal = this.ForceModal;
            this.ForceModal = true;
            this.Show(Owner);

            //HACK: DISABLE ALL TOOLSTRIPS THEN RE-ENABLE THEM TO FIX A KNOWN BUG
            foreach (Control tmpCtl in this.Owner.Controls)
                if (tmpCtl is ToolStrip)
                {
                    //REVERSE IT TWICE TO SET IT BACK TO ITS OLD STATE BE IT ENABLED OR DISABLED
                    tmpCtl.Enabled = !tmpCtl.Enabled;
                    tmpCtl.Enabled = !tmpCtl.Enabled;
                }

            this.WaitForModal();

            //RESET FORCE MODAL TO THE WAY IT WAS BEFORE
            this.ForceModal = tmpForceModal;

            //RETURN THE DIALOG RESULT TO THE CALLING FUNCTION
            return this.DialogResult;
        }

        private void SetupForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.ControlBox = true;
        }

        private void UpdateControlAnchors()
        {
            switch (this.DockingPosition)
            {
                case DockingPositions.RightExterior:
                case DockingPositions.LeftInterior:
                    //ANCHOR CONTROLS TO THE RIGHT SO THEY APPEAR TO SLIDE CORRECTLY
                    foreach (Control tmpControl in this.Controls)
                        if ((tmpControl.Anchor & AnchorStyles.Left) == AnchorStyles.Left)
                        {
                            tmpControl.Anchor ^= AnchorStyles.Left | AnchorStyles.Right;
                        }
                    break;
                case DockingPositions.LeftExterior:
                case DockingPositions.RightInterior:
                    //ANCHOR CONTROLS TO THE LEFT SO THEY APPEAR TO SLIDE CORRECTLY
                    foreach (Control tmpControl in this.Controls)
                        if ((tmpControl.Anchor & AnchorStyles.Right) == AnchorStyles.Right)
                        {
                            tmpControl.Anchor ^= AnchorStyles.Right | AnchorStyles.Left;
                        }
                    break;
                case DockingPositions.TopInterior:
                case DockingPositions.BottomExterior:
                    //ANCHOR CONTROLS TO THE BOTTOM SO THEY APPEAR TO SLIDE CORRECTLY
                    foreach (Control tmpControl in this.Controls)
                        if ((tmpControl.Anchor & AnchorStyles.Top) == AnchorStyles.Top)
                        {
                            tmpControl.Anchor ^= AnchorStyles.Top | AnchorStyles.Bottom;
                        }
                    break;
                case DockingPositions.TopExterior:
                case DockingPositions.BottomInterior:
                    //ANCHOR CONTROLS TO THE BOTTOM SO THEY APPEAR TO SLIDE CORRECTLY
                    foreach (Control tmpControl in this.Controls)
                        if ((tmpControl.Anchor & AnchorStyles.Bottom) == AnchorStyles.Bottom)
                        {
                            tmpControl.Anchor ^= AnchorStyles.Bottom | AnchorStyles.Top;
                        }
                    break;
            }
        }

        private void WaitForModal()
        {
            //JUST LOOP WHILE WE WAIT FOR THE USER TO EXIT / CLOSE THE WINDOW
            while (this.Visible)
            {
                System.Threading.Thread.Sleep(10);
                Application.DoEvents();
            }
        }

        private void BlockAllOpenWindows()
        {
            //CONVERT THIS TO AN ARRAY BECAUSE AS WE START OPENING NEW WINDOWS THE OLD ARRAY BECOMES INVALID
            List<Form> tmpList = new List<Form>((from Form a in Application.OpenForms select a).ToArray());

            //LOOP THROUGH EACH OPEN WINDOW
            foreach (Form tmpForm in tmpList)
            {
                //IF THE FORM IS NOT A GREY OUT FORM, NOT THIS FORM, AND NOT AN INVISIBLE FORM THEN GREY IT OUT AND KEEP TRACK OF IT FOR LATER
                if (!(tmpForm is frmGreyOut) && tmpForm != this && tmpForm.Visible)
                {
                    frmGreyOut tmpBlocker = new frmGreyOut(this);
                    tmpBlocker.BackColor = this.ModalOverlayColor;
                    tmpBlocker.Opacity = ((double)this.ModalOpacity / 100D);
                    tmpBlocker.Show(tmpForm);

                    mBlockers.Add(tmpBlocker);
                }
            }
        }

        public enum DockingPositions
        {
            LeftInterior = 0,
            LeftExterior = 1,
            RightInterior = 2,
            RightExterior = 3,
            TopInterior = 4,
            TopExterior = 5,
            BottomInterior = 6,
            BottomExterior = 7
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // DockableWindow
            // 
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Name = "DockableWindow";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.ResumeLayout(false);
        }
    }
}
