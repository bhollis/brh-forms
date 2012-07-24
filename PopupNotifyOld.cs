/*
PopupNotify - A MSN-style tray popup notification control. Compatible with .NET 1.1.


Copyright (c) 2005 Benjamin Hollis

(The MIT License)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

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
		/// Gets or sets the gradient color which will be blended in drawing the background. Use BackgroundColor for the other gradient color.
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

		/// <summary>
		/// Gets or sets the image shown on the left of the popup.
		/// </summary>
		public Image IconImage 
		{
			get { return iconBox.Image; }
			set { iconBox.Image = value; }
		}

		/// <summary>
		/// Gets or sets the width of the image on the left of the popup.
		/// </summary>
		public int IconWidth = 48;
		/// <summary>
		/// Gets or sets the width of the image on the right of the popup.
		/// </summary>
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
		private static ArrayList openPopups = new ArrayList();
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

			Title = titleText;
			Message = messageText;
			
			if(closeCold == null) 
			{
				closeCold = drawCloseButton(CBS_NORMAL);
			}
			if(closeHot == null)
			{
				closeHot = drawCloseButton(CBS_HOT);
			}
			if(closeDown == null)
			{
				closeDown = drawCloseButton(CBS_PUSHED);
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
			int padding2 = 8;
			int border = 4;

			closeButton.Left = Width - padding - border - closeButton.Width + 4;
			closeButton.Top = padding + border - 3;

			iconBox.Left = padding + border;
			iconBox.Top = padding + border;
			iconBox.Width = IconWidth;
			iconBox.Height = IconHeight;

			NotifyTitle.Top = padding + border - 3;
			NotifyTitle.Left = iconBox.Right + padding2;
			NotifyTitle.Width = closeButton.Left - NotifyTitle.Left - padding2;
			NotifyTitle.Height = 16;

			NotifyMessage.Left = iconBox.Right + padding2;
			NotifyMessage.Width = closeButton.Left - NotifyMessage.Left - padding2;
			NotifyMessage.Top = NotifyTitle.Bottom + padding2;
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
			Rectangle rScreen = Screen.PrimaryScreen.WorkingArea;
			
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

			Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
			lock(iconBox.Image) 
			{
				AnimateWindow(Handle, AnimateTime, flags);
			}
			Application.ThreadException -= new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
		}

		private void MakeRoom() 
		{
			lock(openPopups) 
			{
				foreach(PopupNotify popup in openPopups) 
				{
					if(sysLoc == SystemTrayLocation.BottomLeft || sysLoc == SystemTrayLocation.BottomRight) 
					{
						popup.Top -= Height;
					}
					else 
					{
						popup.Top += Height;
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

		private SystemTrayLocation FindSystemTray(System.Drawing.Rectangle rcWorkArea) 
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
			this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
			this.closeButton.MouseEnter += new System.EventHandler(this.closeButton_MouseEnter);
			this.closeButton.MouseLeave += new System.EventHandler(this.closeButton_MouseLeave);
			this.closeButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.closeButton_MouseDown);
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
			this.NotifyTitle.BackColor = System.Drawing.Color.Transparent;
			this.NotifyTitle.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.NotifyTitle.Location = new System.Drawing.Point(72, 8);
			this.NotifyTitle.Name = "NotifyTitle";
			this.NotifyTitle.Size = new System.Drawing.Size(192, 16);
			this.NotifyTitle.TabIndex = 2;
			// 
			// NotifyMessage
			// 
			this.NotifyMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.NotifyMessage.BackColor = System.Drawing.Color.Transparent;
			this.NotifyMessage.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.NotifyMessage.Location = new System.Drawing.Point(72, 32);
			this.NotifyMessage.Name = "NotifyMessage";
			this.NotifyMessage.Size = new System.Drawing.Size(224, 70);
			this.NotifyMessage.TabIndex = 3;
			// 
			// displayTimer
			// 
			this.displayTimer.Tick += new System.EventHandler(this.displayTimer_Tick);
			// 
			// PopupNotify
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
			this.ClientSize = new System.Drawing.Size(304, 112);
			this.ControlBox = false;
			this.Controls.Add(this.NotifyMessage);
			this.Controls.Add(this.NotifyTitle);
			this.Controls.Add(this.iconBox);
			this.Controls.Add(this.closeButton);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
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
			this.ResumeLayout(false);

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
			windowRect.Inflate(-4,-4);

			// Draw the gradient onto the form
			g.FillRectangle(bBackground, windowRect);

			if(BackgroundImage != null) 
			{
				Rectangle DestRect = new Rectangle(windowRect.Right - BackgroundImage.Width, windowRect.Bottom - BackgroundImage.Height, BackgroundImage.Width, BackgroundImage.Height);
				e.Graphics.DrawImage(BackgroundImage, DestRect);
			}

			// Next draw borders...
			drawBorder (e.Graphics);
		}

		protected virtual void drawBorder (Graphics fx)
		{
			fx.DrawRectangle (Pens.Silver, 2, 2, Width - 4, Height - 4);
			
			// Top border
			fx.DrawLine (Pens.Silver, 0, 0, Width, 0);
			fx.DrawLine (Pens.White, 0, 1, Width, 1);
			fx.DrawLine (Pens.DarkGray, 3, 3, Width - 4, 3);
			fx.DrawLine (Pens.DimGray, 4, 4, Width - 5, 4);

			// Left border
			fx.DrawLine (Pens.Silver, 0, 0, 0, Height);
			fx.DrawLine (Pens.White, 1, 1, 1, Height);
			fx.DrawLine (Pens.DarkGray, 3, 3, 3, Height - 4);
			fx.DrawLine (Pens.DimGray, 4, 4, 4, Height - 5);

			// Bottom border
			fx.DrawLine (Pens.DarkGray, 1, Height - 1, Width - 1, Height - 1);
			fx.DrawLine (Pens.White, 3, Height - 3, Width - 3, Height - 3);
			fx.DrawLine (Pens.Silver, 4, Height - 4, Width - 4, Height - 4);

			// Right border
			fx.DrawLine (Pens.DarkGray, Width - 1, 1, Width - 1, Height - 1);
			fx.DrawLine (Pens.White, Width - 3, 3, Width - 3, Height - 3);
			fx.DrawLine (Pens.Silver, Width - 4, 4, Width - 4, Height - 4);
		}

		
		protected Bitmap drawCloseButton (Int32 state)
		{
			if (visualStylesEnabled())
				return drawThemeCloseButton (state);
			else
				return drawLegacyCloseButton (state);
		}

		/// <summary>
		/// Draw a Windows XP style close button.
		/// </summary>
		protected Bitmap drawThemeCloseButton (Int32 state)
		{
			Bitmap output = new Bitmap(closeButton.Width, closeButton.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			Graphics fx = Graphics.FromImage(output);
            
			IntPtr hTheme = OpenThemeData (Handle, "Window");
			
			if (hTheme == IntPtr.Zero)
			{
				fx.Dispose();
				return drawLegacyCloseButton (state);
			}
			
			Rectangle rClose = new Rectangle(0, 0, closeButton.Width, closeButton.Height);
			RECT reClose = rClose;
			RECT reClip = reClose; // should fx.VisibleClipBounds be used here?
			IntPtr hDC = fx.GetHdc();
			DrawThemeBackground (hTheme, hDC, WP_CLOSEBUTTON, state, ref reClose, ref reClip);
			fx.ReleaseHdc (hDC);
			fx.DrawImage(output,rClose);
			CloseThemeData (hTheme);
			fx.Dispose();

			return output;
		}

		/// <summary>
		/// Draw a Windows 95 style close button.
		/// </summary>
		protected Bitmap drawLegacyCloseButton (Int32 state)
		{
			Bitmap output = new Bitmap(closeButton.Width, closeButton.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			Graphics fx = Graphics.FromImage(output);

			Rectangle rClose = new Rectangle(0, 0, closeButton.Width, closeButton.Height);
			
			ButtonState bState;
			if (state == CBS_PUSHED)
				bState = ButtonState.Pushed;
			else // the Windows 95 theme doesn't have a "hot" button
				bState = ButtonState.Normal;
			ControlPaint.DrawCaptionButton (fx, rClose, CaptionButton.Close, bState);

			fx.DrawImage(output, rClose);
			fx.Dispose();

			return output;
		}

		/// <summary>
		/// Determine whether or not XP Visual Styles are active.  Compatible with pre-UxTheme.dll versions of Windows.
		/// </summary>
		protected bool visualStylesEnabled()
		{
			try
			{
				if (IsThemeActive() == 1)
					return true;
				else
					return false;
			}
			catch (System.DllNotFoundException)  // pre-XP systems which don't have UxTheme.dll
			{
				return false;
			}
		}
		#endregion

		#region P/Invoke
		// DrawThemeBackground()
		protected const Int32 WP_CLOSEBUTTON = 18;
		protected const Int32 CBS_NORMAL = 1;
		protected const Int32 CBS_HOT = 2;
		protected const Int32 CBS_PUSHED = 3;

		[StructLayout (LayoutKind.Explicit)]
			public struct RECT
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
		protected const Int32 HWND_TOPMOST = -1;
		protected const Int32 SWP_NOACTIVATE = 0x0010;

		// ShowWindow()
		protected const Int32 SW_SHOWNOACTIVATE = 4;

		// UxTheme.dll
		[DllImport ("UxTheme.dll")]
		protected static extern Int32 IsThemeActive();
		[DllImport ("UxTheme.dll")]
		protected static extern IntPtr OpenThemeData (IntPtr hWnd, [MarshalAs (UnmanagedType.LPTStr)] string classList);
		[DllImport ("UxTheme.dll")]
		protected static extern void CloseThemeData (IntPtr hTheme);
		[DllImport ("UxTheme.dll")]
		protected static extern void DrawThemeBackground (IntPtr hTheme, IntPtr hDC, Int32 partId, Int32 stateId, ref RECT rect, ref RECT clipRect);

		// user32.dll
		[DllImport ("user32.dll")]
		protected static extern bool ShowWindow (IntPtr hWnd, Int32 flags);
		[DllImport ("user32.dll")]
		protected static extern bool SetWindowPos (IntPtr hWnd, Int32 hWndInsertAfter, Int32 X, Int32 Y, Int32 cx, Int32 cy, uint uFlags);
		[DllImport("user32.dll")]
		protected static extern bool AnimateWindow(IntPtr hwnd, int time, AnimateWindowFlags flags);

		// Shell32.dll
		[DllImport("shell32.dll")]
		protected static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

		[StructLayout(LayoutKind.Sequential)]
			public struct APPBARDATA 
		{
			public static APPBARDATA Create() 
			{
				APPBARDATA appBarData = new APPBARDATA();
				appBarData.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
				return appBarData;
			}
			public int cbSize;
			public IntPtr hWnd;
			public uint uCallbackMessage;
			public uint uEdge;
			public RECT rc;
			public int lParam;
		}

		[Flags]
			public enum AnimateWindowFlags
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

		public const int ABM_QUERYPOS = 0x00000002, ABM_GETTASKBARPOS=5;
		public const int ABE_LEFT = 0;
		public const int ABE_TOP = 1;
		public const int ABE_RIGHT = 2;
		public const int ABE_BOTTOM = 3;
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
			bBackground.Dispose();
		}

		private void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
		{
			//do nothing
		}

		private void closeButton_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}
		#endregion
	}
}