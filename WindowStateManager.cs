/*
WindowStateManager - Remembers window size, location and state across changes in resolution, and allows serialization to XML.


Copyright (c) 2005 Benjamin Hollis

(The MIT License)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Drawing;
using System.Collections;
using System.Xml.Serialization;
using System.Diagnostics;
using System.IO;

namespace Brh.Forms
{
	/// <summary>
	/// Remembers window size, location and state across changes in resolution, and allows serialization to XML.
	/// </summary>
	[XmlRoot("WindowState")]
	public class WindowStateManager
	{
		public WindowStateManager()
		{
			_stateByResolution = new Hashtable();
		}

		/// <summary>
		/// Use this to set the parent form, which will have its state saved.
		/// </summary>
		[XmlIgnore]
		public Form Parent
		{
			//Note: setting another parent after this will be bad
			set
			{
				_parent = value;

				// subscribe to parent form's events
				_parent.Closing += new System.ComponentModel.CancelEventHandler(OnClosing);
				_parent.Resize += new System.EventHandler(OnResize);
				_parent.Move += new System.EventHandler(OnMove);
				_parent.Load += new System.EventHandler(OnLoad);
				SystemEvents.DisplaySettingsChanged += new EventHandler(OnDisplaySettingsChanged);

				// get initial width and height in case form is never resized
				System.Drawing.Size screenSize = Screen.FromRectangle(new Rectangle(_parent.Location, _parent.Size)).Bounds.Size;

				_thisState = new WindowSizeLocation(_parent.Size, _parent.Location, screenSize);

				if(_stateByResolution.ContainsKey(_thisState.ScreenSize)) 
				{
					_parent.StartPosition = FormStartPosition.Manual;
					//This seems to be setting the window location too early!
					_loadedWindowState = _windowState;
					
					_thisState = (WindowSizeLocation)_stateByResolution[_thisState.ScreenSize];

					SetWindowState();
				}

				_stateByResolution[_thisState.ScreenSize] = _thisState;
			}
			get
			{
				return _parent;
			}
		}

		/// <summary>
		/// The table of sizes for each resolution. As far as I can tell this needs to be public to be persisted in XML. Bummer.
		/// </summary>
		public WindowSizeLocation[] Resolutions 
		{
			get 
			{ 
				WindowSizeLocation[] output = new WindowSizeLocation[_stateByResolution.Count];
				int i=0;
				foreach(WindowSizeLocation wsl in _stateByResolution.Values) 
				{
					output[i] = wsl;
					++i;
				}

				return output; 
			}
			set 
			{
				foreach(WindowSizeLocation wsl in value) 
				{
					_stateByResolution[wsl.ScreenSize] = wsl;
				}
			}
		}

		/// <summary>
		/// Whether or not the form should save the "Minimized" state.
		/// </summary>
		public bool AllowSaveMinimized
		{
			get {return _allowSaveMinimized;}
			set {_allowSaveMinimized = value;}
		}

		/// <summary>
		/// The current state of the window (Minimized, Normal, Maximized)
		/// </summary>
		public FormWindowState WindowState 
		{
			get 
			{
				return _windowState; 
			}
			set 
			{
				_windowState = value; 
			}
		}

		private void SetWindowState() 
		{
			settingState = true;
			Application.DoEvents();
			_parent.Location = _thisState.Location;
			_parent.Size = _thisState.Size;
			_parent.WindowState = _windowState;
			settingState = false;
		}

		private void OnResize(object sender, System.EventArgs e)
		{
			// save width and height
			if(!settingState && _parent.WindowState == FormWindowState.Normal)
			{
				_thisState.Size = _parent.Size;

				Debug.Assert(_thisState.Size == ((WindowSizeLocation)_stateByResolution[_thisState.ScreenSize]).Size);
			}
		}

		private void OnMove(object sender, System.EventArgs e)
		{
			if(!settingState) 
			{
				System.Drawing.Size screenSize = Screen.FromRectangle(new Rectangle(_parent.Location, _parent.Size)).Bounds.Size;
				if(screenSize == _thisState.ScreenSize) 
				{
					//Debug.WriteLine("Moved");

					// save position
					if(_parent.WindowState == FormWindowState.Normal)
					{
						_thisState.Location = _parent.Location;

						Debug.Assert(_thisState.Location == ((WindowSizeLocation)_stateByResolution[_thisState.ScreenSize]).Location);
					}
					// save state
					if(!(_parent.WindowState == FormWindowState.Minimized && !_allowSaveMinimized))
						_windowState = _parent.WindowState;
				}
			}
		}

		private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
		}

		private void OnLoad(object sender, System.EventArgs e)
		{
			_parent.WindowState = _loadedWindowState;
		}

		private void OnDisplaySettingsChanged(object sender, EventArgs e)
		{
			System.Drawing.Size screenSize = Screen.FromRectangle(new Rectangle(_parent.Location, _parent.Size)).Bounds.Size;

			Debug.WriteLine("DisplaySettingsChanged: " + screenSize.ToString());

			if(_stateByResolution.ContainsKey(screenSize)) 
			{
				Debug.WriteLine("Back in familiar territory.");
				_thisState = (WindowSizeLocation)_stateByResolution[screenSize];
			}
			else
			{
				Debug.WriteLine("This is a new frontier.");

				WindowSizeLocation newState = new WindowSizeLocation(_parent.Size, _parent.Location, screenSize);

				if(_parent.WindowState != FormWindowState.Normal)
				{
					newState.Location = _thisState.Location;
					newState.Size = _thisState.Size;
				}

				_thisState = newState;

				_stateByResolution[_thisState.ScreenSize] = _thisState;
			}

			//If the form is offscreen, move it back on.
			Rectangle windowRect = new Rectangle(_thisState.Location, _thisState.Size);
			Rectangle workingArea = Screen.FromRectangle(windowRect).WorkingArea;
			if(!workingArea.IntersectsWith(windowRect)) 
			{
				Point newLoc = new Point(_thisState.Location.X, _thisState.Location.Y);

				if(_thisState.Location.X < workingArea.X)
					newLoc.X = workingArea.X;
				else if(_thisState.Location.X > workingArea.Right)
					newLoc.X = workingArea.Right - _thisState.Size.Width;

				if(_thisState.Location.Y < workingArea.Y)
					newLoc.Y = workingArea.Y;
				else if(_thisState.Location.Y > workingArea.Bottom)
					newLoc.Y = workingArea.Bottom - _thisState.Size.Height;

				_thisState.Location = newLoc;
			}

			SetWindowState();
		}

		public void Save(string filename) 
		{
			XmlSerializer s = new XmlSerializer( typeof( WindowStateManager ) );
			TextWriter w = new StreamWriter( filename );
			s.Serialize( w, this );
			w.Close();
		}

		public static WindowStateManager Load(string filename) 
		{
			WindowStateManager output;

			if(File.Exists(filename)) 
			{
				TextReader r = null;

				try 
				{
					XmlSerializer s = new XmlSerializer( typeof( WindowStateManager ) );
				
					r = new StreamReader( filename );
					output = (WindowStateManager)s.Deserialize( r );
					r.Close();
				}
				catch(InvalidOperationException) 
				{
					if(r != null)
						r.Close();

					File.Delete(filename);
					output = new WindowStateManager();
				}
			}
			else 
			{
				output = new WindowStateManager();
			}

			return output;
		}

		private Form _parent;
		private FormWindowState _windowState;
		private FormWindowState _loadedWindowState;
		private bool _allowSaveMinimized = false;
		private bool _dockToScreenEdges = true;
		private int _dockDistance = 5;//px
		private Hashtable/*<WindowSizeLocation>*/ _stateByResolution;
		private WindowSizeLocation _thisState;
		bool settingState = false;

		[XmlRoot("Resolution")]
			public class WindowSizeLocation 
		{
			public WindowSizeLocation() 
			{
			}

			public WindowSizeLocation(System.Drawing.Size windowSize, System.Drawing.Point windowPos, System.Drawing.Size screenSize)
			{
				_windowSize = windowSize;
				_windowPos = windowPos;
				_screenSize = screenSize;
			}

			public System.Drawing.Size Size 
			{
				get { return _windowSize; }
				set { _windowSize = value; }
			}

			public System.Drawing.Point Location 
			{
				get { return _windowPos; }
				set { _windowPos = value; }
			}

			public System.Drawing.Size ScreenSize 
			{
				get { return _screenSize; }
				set { _screenSize = value; }
			}

			private System.Drawing.Size _windowSize;
			private System.Drawing.Point _windowPos;
			private System.Drawing.Size _screenSize;
		}
	}
}
