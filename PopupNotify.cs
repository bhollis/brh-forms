/*
PopupNotify - A MSN-style tray popup notification control. Compatible with .NET 2.0+.


Copyright (c) 2005 Benjamin Hollis

(The MIT License)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;

namespace Brh.Forms
{
	/// <summary>
	/// Summary description for PopupNotify.
	/// Note: No properties may be changed after Show - don't do it! It won't cause an error but it'll mess you up.
	/// </summary>
	public class PopupNotify : System.Windows.Forms.Form
	{
		#region Public Variables
		/// <summary>
		/// Gets or sets the title text to be displayed in the NotifyWindow.
		/// </summary>
		public string Title 
		{
			get { return NotifyTitle.Text; }
			set { NotifyTitle.Text = value; }
		}

		/// <summary>
		/// Gets or sets the message text to be displayed in the NotifyWindow.
		/// </summary>
		public string Message
		{
			get { return NotifyMessage.Text; }
			set { NotifyMessage.Text = value; }
		}

		/// <summary>
		/// Gets or sets a value specifiying whether or not the window should continue to be displayed if the mouse cursor is inside the bounds
		/// of the NotifyWindow.
		/// </summary>
		public bool WaitOnMouseOver;
		/// <summary>
		/// An EventHandler called when the NotifyWindow main text is clicked.
		/// </summary>
		//public event System.EventHandler TextClicked;
		/// <summary>
		/// An EventHandler called when the NotifyWindow title text is clicked.
		/// </summary>
		//public event System.EventHandler TitleClicked;
		
		/// <summary>
		/// Gets or sets the gradient color which will be blended in drawing the background.
		/// </summary>
		public System.Drawing.Color GradientColor; 

		/// <summary>
		/// Gets or sets the amount of milliseconds to display the NotifyWindow for.
		/// </summary>
		public int WaitTime;
		/// <summary>
		/// Gets or sets the amount of time the slide in/out animations take, in ms.
		/// </summary>
		public int AnimateTime;

		public Image IconImage 
		{
			get { return iconBox.Image; }
			set { iconBox.Image = new Bitmap(value); }
		}
		public int IconWidth = 48;
		public int IconHeight = 48;
		
		#endregion

		#region Private Members
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.PictureBox iconBox;
		private System.Windows.Forms.Label NotifyTitle;
		private System.Windows.Forms.Label NotifyMessage;
		private System.Windows.Forms.Timer displayTimer;
		private System.Windows.Forms.PictureBox closeButton;

		private enum SystemTrayLocation { BottomLeft, BottomRight, TopRight };
		private System.Drawing.Drawing2D.LinearGradientBrush bBackground = null;

		private static Bitmap closeCold = null;
		private static Bitmap closeHot = null;
		private static Bitmap closeDown = null;
		private SystemTrayLocation sysLoc;
        private static List<PopupNotify> openPopups = new List<PopupNotify>();
		#endregion

		public PopupNotify() : this("", "")
		{
		}

		public PopupNotify(string titleText, string messageText)
		{			
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

            foreach (Control c in Controls)
            {
                c.Font = SystemFonts.MessageBoxFont;
            }
            this.NotifyTitle.Font = new Font(SystemFonts.MessageBoxFont.Name, 12f, FontStyle.Regular, GraphicsUnit.Point);
            //this.NotifyMessage.Font = new Font(SystemFonts.MessageBoxFont.Name, 10f, FontStyle.Regular, GraphicsUnit.Point);

			Title = titleText;
			Message = messageText;
			
			if(closeCold == null) 
			{
				closeCold = DrawCloseButton(CloseButtonState.Normal);
			}
			if(closeHot == null)
			{
				closeHot = DrawCloseButton(CloseButtonState.Hot);
			}
			if(closeDown == null)
			{
				closeDown = DrawCloseButton(CloseButtonState.Pushed);
			}

			closeButton.Image = closeCold;

			// Default values
			BackColor = Color.SkyBlue;
			GradientColor = Color.WhiteSmoke;
			WaitOnMouseOver = true;
			WaitTime = 4000;
			AnimateTime = 250;
		}

		private void SetLayout() 
		{
			int padding = 8;
			int iconRightPadding = 0;
			int border = 1;

			iconBox.Left = padding + border;
			iconBox.Top = padding + border;
			iconBox.Width = IconWidth;
			iconBox.Height = IconHeight;

            this.Height = iconBox.Height + 2 * padding + 2 * border;

            closeButton.Left = Width - padding - border - closeButton.Width + 3;
            closeButton.Top = padding + border - 3;

            NotifyTitle.Top = iconBox.Top - 5; //fudge factor
			NotifyTitle.Left = iconBox.Right + iconRightPadding;
			
            NotifyMessage.Left = NotifyTitle.Left + 1; //fudgy
			NotifyMessage.Width = Width - NotifyMessage.Left - padding - border;
			NotifyMessage.Top = NotifyTitle.Bottom;
			NotifyMessage.Height = Height - NotifyMessage.Top - padding - border;
		}

		#region Animation and Notification
		private void Notify()
		{
			if(IconImage == null)
				iconBox.Visible = false;

			SetLayout();

			Rectangle rScreen = Screen.PrimaryScreen.WorkingArea;
	
			sysLoc = FindSystemTray(rScreen);
			
			if(sysLoc == SystemTrayLocation.BottomRight) 
			{
				Top = rScreen.Bottom - Height;
				Left = rScreen.Right - Width;
				MakeRoom();
				SetWindowPos (Handle, HWND_TOPMOST, Left, Top, Width, Height, SWP_NOACTIVATE);
				AnimateWindow(false, false);
			}
			else if(sysLoc == SystemTrayLocation.TopRight) 
			{
				Top = rScreen.Top;
				Left = rScreen.Right - Width;
				MakeRoom();
				SetWindowPos (Handle, HWND_TOPMOST, Left, Top, Width, Height, SWP_NOACTIVATE);
				AnimateWindow(true, false);
			}
			else if(sysLoc == SystemTrayLocation.BottomLeft) 
			{
				Top = rScreen.Bottom - Height;
				Left = rScreen.Left;
				MakeRoom();
				SetWindowPos (Handle, HWND_TOPMOST, Left, Top, Width, Height, SWP_NOACTIVATE);
				AnimateWindow(false, false);
			}

			lock(openPopups) 
			{
				openPopups.Add(this);
			}

			displayTimer.Interval = WaitTime;
			displayTimer.Start();
		}

		private void UnNotify()
		{
			if(sysLoc == SystemTrayLocation.BottomRight || sysLoc == SystemTrayLocation.BottomLeft) 
			{
				AnimateWindow(true, true);
			}
			else if(sysLoc == SystemTrayLocation.TopRight) 
			{
				AnimateWindow(false, true);
			}
			
			this.Close();
		}

		private void AnimateWindow(bool positive, bool hide)
		{
			AnimateWindowFlags flags = AnimateWindowFlags.AW_SLIDE;

			if(positive) 
			{
				flags |= AnimateWindowFlags.AW_VER_POSITIVE;
			}
			else 
			{
				flags |= AnimateWindowFlags.AW_VER_NEGATIVE;
			}

			if(hide) 
			{
				flags |= AnimateWindowFlags.AW_HIDE;
			}

            //Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);

            try
            {

//#warning this locking is still not correct
//                lock (iconBox.Image)
//                {
                    AnimateWindow(Handle, AnimateTime, flags);
//                }
            }
            catch (ArgumentException ae)
            {
                Debug.WriteLine("Got an exception: " + ae.Message);
            }
		}

		private void MakeRoom() 
		{
            int spaceBetweenPopups = 1;
			lock(openPopups) 
			{
				foreach(PopupNotify popup in openPopups) 
				{
					if(sysLoc == SystemTrayLocation.BottomLeft || sysLoc == SystemTrayLocation.BottomRight) 
					{
                        popup.Top -= Height + spaceBetweenPopups;
					}
					else 
					{
                        popup.Top += Height + spaceBetweenPopups;
					}
				}
			}
		}

		private void Collapse() 
		{
			lock(openPopups) 
			{
				int thisIndex = openPopups.IndexOf(this);

				for(int i=thisIndex-1;i >= 0;i--) 
				{
					PopupNotify popup = (PopupNotify)openPopups[i];

					if(sysLoc == SystemTrayLocation.BottomLeft || sysLoc == SystemTrayLocation.BottomLeft) 
					{
						popup.Top += Height;
					}
					else 
					{
						popup.Top -= Height;
					}				
				}	
	
				openPopups.RemoveAt(thisIndex);
			}
		}
		#endregion

		private static SystemTrayLocation FindSystemTray(System.Drawing.Rectangle rcWorkArea) 
		{
			APPBARDATA appBarData = APPBARDATA.Create();
			if(SHAppBarMessage(ABM_GETTASKBARPOS, ref appBarData) != IntPtr.Zero) 
			{
				RECT taskBarLocation = appBarData.rc;

				int TaskBarHeight = taskBarLocation.Bottom - taskBarLocation.Top;
				int TaskBarWidth = taskBarLocation.Right - taskBarLocation.Left;

				if( TaskBarHeight > TaskBarWidth )
				{
					//	Taskbar is vertical
					if( taskBarLocation.Right > rcWorkArea.Right )
						return SystemTrayLocation.BottomRight;
					else
						return SystemTrayLocation.BottomLeft;
				}
				else
				{
					//	Taskbar is horizontal
					if( taskBarLocation.Bottom > rcWorkArea.Bottom )
						return SystemTrayLocation.BottomRight;
					else
						return SystemTrayLocation.TopRight;
				}
			}
			else 
			{
				return SystemTrayLocation.BottomRight; //oh well, let's just go default
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
            {
				if(components != null)
				{
					components.Dispose();
                    components = null;
				}

                if (this.iconBox.Image != null)
                {
                    this.iconBox.Image.Dispose();
                    this.iconBox.Image = null;
                }

                if (bBackground != null)
                {
                    bBackground.Dispose();
                    bBackground = null;
                }                
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            this.closeButton = new System.Windows.Forms.PictureBox();
            this.iconBox = new System.Windows.Forms.PictureBox();
            this.NotifyTitle = new System.Windows.Forms.Label();
            this.NotifyMessage = new System.Windows.Forms.Label();
            this.displayTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.closeButton)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.iconBox)).BeginInit();
            this.SuspendLayout();
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.BackColor = System.Drawing.Color.Transparent;
            this.closeButton.Location = new System.Drawing.Point(280, 8);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(16, 16);
            this.closeButton.TabIndex = 0;
            this.closeButton.TabStop = false;
            this.closeButton.MouseLeave += new System.EventHandler(this.closeButton_MouseLeave);
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            this.closeButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.closeButton_MouseDown);
            this.closeButton.MouseEnter += new System.EventHandler(this.closeButton_MouseEnter);
            // 
            // iconBox
            // 
            this.iconBox.BackColor = System.Drawing.Color.Transparent;
            this.iconBox.Location = new System.Drawing.Point(8, 8);
            this.iconBox.Name = "iconBox";
            this.iconBox.Size = new System.Drawing.Size(50, 50);
            this.iconBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.iconBox.TabIndex = 1;
            this.iconBox.TabStop = false;
            // 
            // NotifyTitle
            // 
            this.NotifyTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.NotifyTitle.AutoEllipsis = true;
            this.NotifyTitle.AutoSize = true;
            this.NotifyTitle.BackColor = System.Drawing.Color.Transparent;
            this.NotifyTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(68)))), ((int)(((byte)(135)))));
            this.NotifyTitle.Location = new System.Drawing.Point(72, 8);
            this.NotifyTitle.Name = "NotifyTitle";
            this.NotifyTitle.Size = new System.Drawing.Size(23, 13);
            this.NotifyTitle.TabIndex = 2;
            this.NotifyTitle.Text = "title";
            // 
            // NotifyMessage
            // 
            this.NotifyMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.NotifyMessage.BackColor = System.Drawing.Color.Transparent;
            this.NotifyMessage.Location = new System.Drawing.Point(72, 40);
            this.NotifyMessage.Name = "NotifyMessage";
            this.NotifyMessage.Size = new System.Drawing.Size(224, 10);
            this.NotifyMessage.TabIndex = 3;
            // 
            // displayTimer
            // 
            this.displayTimer.Tick += new System.EventHandler(this.displayTimer_Tick);
            // 
            // PopupNotify
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(304, 66);
            this.ControlBox = false;
            this.Controls.Add(this.NotifyMessage);
            this.Controls.Add(this.NotifyTitle);
            this.Controls.Add(this.iconBox);
            this.Controls.Add(this.closeButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PopupNotify";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "PopupNotify";
            this.TopMost = true;
            this.Closing += new System.ComponentModel.CancelEventHandler(this.PopupNotify_Closing);
            this.Load += new System.EventHandler(this.PopupNotify_Load);
            ((System.ComponentModel.ISupportInitialize)(this.closeButton)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.iconBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		#region Drawing
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if(bBackground == null) 
			{	
				Rectangle rBackground = new Rectangle(0, 0, this.Width, this.Height);
				bBackground = new System.Drawing.Drawing2D.LinearGradientBrush(rBackground, BackColor, GradientColor, 90f);
			}

			// Getting the graphics object
			Graphics g = e.Graphics;

			Rectangle windowRect = new Rectangle(0,0,Width,Height);
			windowRect.Inflate(-1,-1);

			// Draw the gradient onto the form
			g.FillRectangle(bBackground, windowRect);

			if(BackgroundImage != null) 
			{
				Rectangle DestRect = new Rectangle(windowRect.Right - BackgroundImage.Width, windowRect.Bottom - BackgroundImage.Height, BackgroundImage.Width, BackgroundImage.Height);
				e.Graphics.DrawImage(BackgroundImage, DestRect);
			}

			// Next draw borders...
			DrawBorder (e.Graphics);
		}

		protected virtual void DrawBorder (Graphics fx)
		{
			fx.DrawRectangle(Pens.Black, 0, 0, Width-1, Height-1);
		}

		
		protected Bitmap DrawCloseButton (CloseButtonState state)
		{
			if (VisualStyleRenderer.IsSupported)
				return DrawThemeCloseButton(state);
			else
				return DrawLegacyCloseButton(state);
		}

		/// <summary>
		/// Draw a Windows XP style close button.
		/// </summary>
		protected Bitmap DrawThemeCloseButton (CloseButtonState state)
		{
			Bitmap output = new Bitmap(closeButton.Width, closeButton.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			Graphics fx = Graphics.FromImage(output);
            Rectangle rect = closeButton.ClientRectangle;

            VisualStyleElement vse = null;

            switch (state)
            {
                case CloseButtonState.Hot:
                    vse = VisualStyleElement.Window.CloseButton.Hot;
                    break;
                case CloseButtonState.Normal:
                    vse = VisualStyleElement.Window.CloseButton.Normal;
                    break;
                case CloseButtonState.Pushed:
                    vse = VisualStyleElement.Window.CloseButton.Pressed;
                    break;
            }

            VisualStyleRenderer vsr = new VisualStyleRenderer(vse);
            vsr.DrawBackground(fx, rect);

            fx.Dispose();

			return output;
		}

		/// <summary>
		/// Draw a Windows 95 style close button.
		/// </summary>
        protected Bitmap DrawLegacyCloseButton(CloseButtonState state)
		{
			Bitmap output = new Bitmap(closeButton.Width, closeButton.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			Graphics fx = Graphics.FromImage(output);

			Rectangle rClose = new Rectangle(0, 0, closeButton.Width, closeButton.Height);
			
			ButtonState bState;
			if (state == CloseButtonState.Pushed)
				bState = ButtonState.Pushed;
			else // the Windows 95 theme doesn't have a "hot" button
				bState = ButtonState.Normal;
			ControlPaint.DrawCaptionButton (fx, rClose, CaptionButton.Close, bState);

			fx.DrawImage(output, rClose);
			fx.Dispose();

			return output;
		}
        protected enum CloseButtonState
        {
            Normal,
            Hot,
            Pushed
        }
		#endregion

		#region P/Invoke
		[StructLayout (LayoutKind.Explicit)]
        private struct RECT
		{
			[FieldOffset (0)] public Int32 Left;
			[FieldOffset (4)] public Int32 Top;
			[FieldOffset (8)] public Int32 Right;
			[FieldOffset (12)] public Int32 Bottom;

			public RECT (System.Drawing.Rectangle bounds)
			{
				Left = bounds.Left;
				Top = bounds.Top;
				Right = bounds.Right;
				Bottom = bounds.Bottom;
			}

			public static implicit operator Rectangle( RECT rect ) 
			{
				return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
			}

			public static implicit operator RECT( Rectangle rect ) 
			{
				return new RECT(rect.Left, rect.Top, rect.Right, rect.Bottom);
			} 

			public RECT(int left_, int top_, int right_, int bottom_) 
			{
				Left = left_;
				Top = top_;
				Right = right_;
				Bottom = bottom_;
			}

			public int Height { get { return Bottom - Top + 1; } }
			public int Width { get { return Right - Left + 1; } }
			public Size Size { get { return new Size(Width, Height); } }

			public Point Location { get { return new Point(Left, Top); } }

			// Handy method for converting to a System.Drawing.Rectangle
			public Rectangle ToRectangle() 
			{
				return Rectangle.FromLTRB(Left, Top, Right, Bottom); }

			public static RECT FromRectangle(Rectangle rectangle) 
			{
				return new RECT(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
			}

			public override int GetHashCode() 
			{
				return Left ^ ((Top << 13) | (Top >> 0x13))
					^ ((Width << 0x1a) | (Width >> 6))
					^ ((Height << 7) | (Height >> 0x19));
			} 
		}

		// SetWindowPos()
        private const Int32 HWND_TOPMOST = -1;
        private const Int32 SWP_NOACTIVATE = 0x0010;

		// ShowWindow()
        private const Int32 SW_SHOWNOACTIVATE = 4;

		// user32.dll
		[DllImport ("user32.dll")]
		private static extern bool ShowWindow (IntPtr hWnd, Int32 flags);
		[DllImport ("user32.dll")]
		private static extern bool SetWindowPos (IntPtr hWnd, Int32 hWndInsertAfter, Int32 X, Int32 Y, Int32 cx, Int32 cy, uint uFlags);
		[DllImport("user32.dll")]
		private static extern bool AnimateWindow(IntPtr hwnd, int time, AnimateWindowFlags flags);

		// Shell32.dll
		[DllImport("shell32.dll")]
		private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

		[StructLayout(LayoutKind.Sequential)]
		private struct APPBARDATA 
		{
			public static APPBARDATA Create() 
			{
				APPBARDATA appBarData = new APPBARDATA();
				appBarData.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
				return appBarData;
			}
			private int cbSize;
            private IntPtr hWnd;
            private uint uCallbackMessage;
            private uint uEdge;
            public RECT rc;
            private int lParam;
		}

		[Flags]
        private enum AnimateWindowFlags
		{
			AW_HOR_POSITIVE = 0x00000001,
			AW_HOR_NEGATIVE = 0x00000002,
			AW_VER_POSITIVE = 0x00000004,
			AW_VER_NEGATIVE = 0x00000008,
			AW_CENTER       = 0x00000010,
			AW_HIDE     = 0x00010000,
			AW_ACTIVATE     = 0x00020000,
			AW_SLIDE    = 0x00040000,
			AW_BLEND    = 0x00080000
		}

        private const int ABM_QUERYPOS = 0x00000002, ABM_GETTASKBARPOS = 5;
        private const int ABE_LEFT = 0;
        private const int ABE_TOP = 1;
        private const int ABE_RIGHT = 2;
        private const int ABE_BOTTOM = 3;
		#endregion

		#region Event Handlers
		private void closeButton_MouseEnter(object sender, System.EventArgs e)
		{
			closeButton.Image = closeHot;
		}

		private void closeButton_MouseLeave(object sender, System.EventArgs e)
		{
			closeButton.Image = closeCold;
		}

		private void closeButton_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			closeButton.Image = closeDown;
		}

		private void PopupNotify_Load(object sender, System.EventArgs e)
		{
			Notify();
		}

		private void displayTimer_Tick(object sender, System.EventArgs e)
		{
			if(this.WaitOnMouseOver && this.Bounds.Contains(Cursor.Position)) 
			{
				displayTimer.Interval = 1000; //try every second, now
			}
			else 
			{
				displayTimer.Stop();
				UnNotify();
			}
		}

		private void PopupNotify_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Collapse();
		}

		private void closeButton_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}
		#endregion
	}
}
