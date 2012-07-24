# Brh.Forms .NET Controls

This is a collection of rather old .NET controls that I wrote as parts of random projects back in the day. They're from well before I had much programming experience, but they've been useful to some people over the years, so I've continued to make them available on my website, but I realized they'd have a better home on GitHub. Pull requests are welcome.

To use these controls, simply download the file you want and include it in your project. All of the controls are licensed under the MIT License.

## PopupNotify

This Windows Forms control is a "toast" popup notifier, similar to the ones seen in the old MSN Messenger. I developed it for [XBList](http://xblist.com), based on the [NotifyWindow on CodeProject](http://www.codeproject.com/cs/miscctrl/RobMisNotifyWindow.asp). It improves upon the original by using the native `AnimateWindow` API to animate, which is much faster and smoother. It allows for an image on the left of the popup, and an alpha-blended image in the background of the popup. It will properly locate the system tray and pop in the correct direction no matter where the user's taskbar has been moved. 

There are two versions - an old version for use with .NET 1.1, and a newer implementation for .NET 2.0+. The newer implementation makes less P/Invoke calls, is better with memory, has improved drawing code, and a more modern look.

![Example of the popup](http://benhollis.net/software/xblist/images/xblistscreen_vistat-e1a6e3ef.jpg)

Usage example:

```csharp
Brh.Forms.PopupNotify n = new Brh.Forms.PopupNotify();
n.Title = "Popup Title";
n.Message = "A short message.";
n.GradientColor = Color.Green;
n.BackColor = Color.WhiteSmoke;
n.Width = 330;
n.Height = 100;
n.IconImage = myImage;
n.IconHeight = 64;
n.IconWidth = 64;
n.BackgroundImage = myBackground;

n.Show();
```

## WindowStateManager

`WindowStateManager` is a simple class that handles persisting window size, location, and state between sessions of an application. It's meant for use with Windows Forms applications. It was inspired by the post "[The fractal nature of UI design problems](http://miksovsky.blogs.com/flowstate/2005/10/the_fractal_nat.html)" at [flow | state](http://miksovsky.blogs.com/flowstate/) (this class gets to point 7, as far as I know), and was implemented based on Joel Matthias' [PersistWindowState class](http://www.codeproject.com/csharp/restoreformstate.asp) on CodeProject.net.

To use `WindowStateManager`, simply instantiate it and set its `Parent` property to the form you want to save. Then you have a choice: you can either load and save the `WindowStateManager` instance to an XML file by calling its `Load` and `Save` methods, or you can include it in another class that's already being serialized back and forth from XML, like the settings file for your app (that's what I do). `WindowStateManager` does the rest! It saves the position of the form, the size of the form, and the state it was in (`Minimized`, `Maximized`, or `Normal`). Furthermore, it stores a separate set of sizes and positions for each resolution, so that the window doesn't get screwed up when the user changes resolutions (for example, when docking and undocking a laptop). 

## WebLinkLabel

The `WebLinkLabel` is a wrapper around the standard .NET Windows Forms [`LinkLabel`](http://msdn2.microsoft.com/en-us/library/897fcdkf.aspx). It makes it easy to put different links in a label, like you would in a web page, and clicking on those links will open them in the user's default browser.

It basically works just like a `LinkLabel`. You can drag it from your toolkit onto a form, and set all the same properties. Setting the `Text` or `LinkText` to a specially-formatted string will automatically insert links. Just write out your links like this: "Hello, this is text <url=http://benhollis.net>with a link in it</url>." You can have as many links as you want.

If you want to access the "source" text, with the "url" tags in it, use the `LinkText` property. If you want to just get the text out, without the "url" tags, use the standard `Text` property. 