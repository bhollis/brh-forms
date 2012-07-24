/*
WebLinkLabel - A LinkLabel that can contain clickable hyperlinks.


Copyright (c) 2005 Benjamin Hollis

(The MIT License)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.ComponentModel;

namespace Brh.Forms
{
    /// <summary>
    /// A LinkLabel that can have its text set in an HTML-like way, and opens clicked links
    /// in the default web browser. 
    /// </summary>
    [ToolboxItem(true)]
    public class WebLinkLabel : LinkLabel
    {
        private static Regex markupRegex = new Regex(
            @"<url=(?<url>[^>]+)>(?<link>[^<]+)</url>",
            RegexOptions.ExplicitCapture
            | RegexOptions.CultureInvariant
            | RegexOptions.Compiled
            );

        private string rawText = String.Empty;

        /// <summary>
        /// Set the text. Use bbcode blocks like &lt;url=http://stuff.com&gt;link text&lt;/url&gt; to signify links that should load in the web browser.
        /// </summary>
        [Category("Appearance")]
        [RefreshProperties(RefreshProperties.All)]
        [Localizable(true)]
        public string LinkText
        {   
            get
            {
                return rawText;
            }
            set
            {
                this.SuspendLayout();

                this.rawText = value;

                this.Links.Clear();

                StringBuilder text = new StringBuilder();
                int pos = 0;

                foreach (Match m in markupRegex.Matches(value))
                {
                    text.Append(this.rawText.Substring(pos, m.Index - pos));
                    pos = m.Index + m.Length;
                    
                    string link = m.Groups["link"].Value;
                    string url = m.Groups["url"].Value;

                    int startIndex = text.Length;
                    text.Append(link);
                    this.Links.Add(new Link(startIndex, link.Length, url));
                }

                text.Append(this.rawText.Substring(pos));

                base.Text = text.ToString();
                this.ResumeLayout();
             }
        }

        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                this.LinkText = value;
            }
        }
        
        protected override void OnLinkClicked(LinkLabelLinkClickedEventArgs e)
        {
            base.OnLinkClicked(e);

            BrowserHelper.LaunchUrl((String)e.Link.LinkData);
        }
    }
}
