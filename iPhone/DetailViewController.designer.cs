// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;

namespace iPhone
{
	[Register ("DetailViewController")]
	partial class DetailViewController
	{
		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UITableView table { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIToolbar toolbar { get; set; }

		void ReleaseDesignerOutlets ()
		{
			if (table != null) {
				table.Dispose ();
				table = null;
			}
			if (toolbar != null) {
				toolbar.Dispose ();
				toolbar = null;
			}
		}
	}
}
