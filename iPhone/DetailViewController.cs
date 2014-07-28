using System;
using System.Drawing;
using System.Collections.Generic;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Core;

namespace iPhone
{
	public partial class DetailViewController : UIViewController
	{
		ToFy.List tofyList;
		UIAlertView alert;

		public DetailViewController (IntPtr handle) : base (handle)
		{
		}

		public void SetDetailItem (ToFy.List newDetailItem)
		{
			if (tofyList != newDetailItem) {
				tofyList = newDetailItem;

				// Update the view
				ConfigureView ();
			}
		}

		void ConfigureView ()
		{
			// Update the user interface for the detail item
			if (IsViewLoaded && tofyList != null) {
				Title = tofyList.ToString ();
				LoadingOverlay loadingOverlay = new LoadingOverlay (UIScreen.MainScreen.Bounds);
				View.Add (loadingOverlay);

				ToFy.GetList (tofyList.name,tofyList.password,delegate(ToFy.Response response){
					InvokeOnMainThread ( () => {
						switch(response.status) {
						case ToFy.Status.Ok:
							table.Source = new TableSource (response.list.items);
							table.ReloadData();
							loadingOverlay.Hide ();
							break;
						case ToFy.Status.NotFound: 
							alert = new UIAlertView ();

							alert.AddButton ("Ok");
							alert.Message = "The list does not exist anymore";
							alert.AlertViewStyle = UIAlertViewStyle.Default;
							alert.Clicked += (object s, UIButtonEventArgs e) => {
								loadingOverlay.Hide ();
								NavigationController.PopViewControllerAnimated(true);
							}; 
							alert.Show ();
							break;
						case ToFy.Status.Unauthorized:
							alert = new UIAlertView ();

							alert.AddButton ("Ok");
							alert.Message = "The list's password was changed";
							alert.AlertViewStyle = UIAlertViewStyle.Default;
							alert.Clicked += (object s, UIButtonEventArgs e) => {
								loadingOverlay.Hide ();
								NavigationController.PopViewControllerAnimated(true);
							}; 
							alert.Show (); 
							break;
						default: 
							table.Source = new TableSource (tofyList.items);

							alert = new UIAlertView ();

							alert.AddButton ("Ok");
							alert.Message = "Connection error, using offline list";
							alert.AlertViewStyle = UIAlertViewStyle.Default;
							alert.Clicked += (object s, UIButtonEventArgs e) => {
								loadingOverlay.Hide ();
							}; 
							alert.Show (); 
							break;
						}
					});
				}
				);
			}
		}

		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			ConfigureView ();
		}
	}
}

