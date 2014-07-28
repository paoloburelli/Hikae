using System;
using System.Drawing;
using System.Collections.Generic;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Core;
using System.Threading;

namespace iPhone
{
	public partial class DetailViewController : UIViewController
	{
		ToFy.List tofyList;
		UIAlertView alert;
		LoadingOverlay loadingOverlay;

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
							tofyList.items = response.list.items;
							table.Source = new TableSource (tofyList.items);
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
			var addButton = new UIBarButtonItem (UIBarButtonSystemItem.Add, AddNewItem);
			NavigationItem.RightBarButtonItem = addButton;
			ConfigureView ();
		}

		void AddNewItem (object sender, EventArgs args)
		{
			UIAlertView alert = new UIAlertView ();
			alert.AddButton ("Ok");
			alert.AddButton ("Cancel");
			alert.Message = "Enter item's name";
			alert.AlertViewStyle = UIAlertViewStyle.PlainTextInput;
			alert.Clicked += AlertButtonClicked; 
			alert.Show ();
		}

		void AlertButtonClicked(object sender, UIButtonEventArgs args) {
			UIAlertView parent_alert = (UIAlertView)sender;

			if (args.ButtonIndex == 0) {
				if (!tofyList.items.Contains (parent_alert.GetTextField (0).Text)) {

					table.BeginUpdates ();
					tofyList.items.Add (parent_alert.GetTextField (0).Text + " (uploading...)");

					table.InsertRows (new NSIndexPath[] { 
						NSIndexPath.FromRowSection (table.NumberOfRowsInSection (0), 0) 
					}, UITableViewRowAnimation.Fade);
					table.EndUpdates ();

					ToFy.AddItem (tofyList.name, parent_alert.GetTextField (0).Text, tofyList.password, delegate(ToFy.Response response) {
						if (response.status == ToFy.Status.ConnectionFailed){
							Thread.Sleep(1000);
							AlertButtonClicked(parent_alert,args);
						} else if (response.status == ToFy.Status.Ok){
							InvokeOnMainThread(() =>{
								int index = tofyList.items.IndexOf(parent_alert.GetTextField (0).Text + " (uploading...)");
								table.BeginUpdates ();
								tofyList.items[index] = parent_alert.GetTextField (0).Text;
								table.EndUpdates ();
								table.ReloadData();
							});
						}
					});
				}
			}
		}
	}
}

