using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;
using tofy;
using System.Net;
using System.Collections.Generic;

namespace Hikae
{
	partial class MainViewController : UIViewController
	{
		LoadingOverlay loadingOverlay;

		public MainViewController (IntPtr handle) : base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			NavigationItem.TitleView = new UIImageView (new UIImage ("logo-toolbar.png"));
			table.BackgroundView = new UIImageView (new UIImage ("paper.jpg"));

			UIBarButtonItem addButton = new UIBarButtonItem (UIBarButtonSystemItem.Add, CreateList);
			UIBarButtonItem openButton = new UIBarButtonItem(UIBarButtonSystemItem.Organize, OpenList);
			UIBarButtonItem emptySpace = new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace);
			openButton.TintColor = UIColor.White;
			addButton.TintColor = UIColor.White;

			toolbar.SetItems(new UIBarButtonItem[] {
				emptySpace,
				openButton,
				emptySpace,
				addButton,
				emptySpace
			},true);


			table.Source = Catalog.Instance;
		}

		void CreateList (object sender, EventArgs args)
		{
			UIAlertView alert = new UIAlertView ();
			alert.AddButton ("Create List");
			alert.AddButton ("Cancel");
			alert.Message = "Enter the name and the password for the list you want to create";
			alert.AlertViewStyle = UIAlertViewStyle.LoginAndPasswordInput;
			alert.Clicked += CreateAccepted; 
			alert.Show ();
		}

		void CreateAccepted (object sender, UIButtonEventArgs args)
		{
			UIAlertView parent_alert = (UIAlertView)sender;
			if (args.ButtonIndex == 0) {
				if (parent_alert.GetTextField (0).Text != "") {
					loadingOverlay = new LoadingOverlay (UIScreen.MainScreen.Bounds);
					View.Add (loadingOverlay);
					Communication.AddList (parent_alert.GetTextField (0).Text, parent_alert.GetTextField (1).Text, delegate(Communication.Response response) {
						InvokeOnMainThread (() => {
							switch (response.Status) {
							case HttpStatusCode.Created:
								Catalog.Instance.Lists.Insert (0, new ToList(parent_alert.GetTextField (0).Text,new List<ToItem>(),parent_alert.GetTextField (1).Text));
								Catalog.Instance.Save();
								using (var indexPath = NSIndexPath.FromRowSection (0, 0)) {
									table.InsertRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
								}
								break;
							case HttpStatusCode.Conflict:
								ShowAlert("A list with this name already exists, please choose another name",CreateList);
								break;
							default:
								ShowAlert("Connetion error: "+response.Status,CreateList);
								break;
							}
							loadingOverlay.Hide();
						});
					});
				} else {
					ShowAlert ("The lists's name can't be empty", CreateList);
				} 
			}
		}

		void OpenList (object sender, EventArgs args)
		{
			UIAlertView alert = new UIAlertView ();
			alert.AddButton ("Open List");
			alert.AddButton ("Cancel");
			alert.Message = "Enter the name and the password for the list you want to open";
			alert.AlertViewStyle = UIAlertViewStyle.LoginAndPasswordInput;
			alert.Clicked += OpenAccepted; 
			alert.Show ();
		}

		void OpenAccepted (object sender, UIButtonEventArgs args)
		{
			UIAlertView parent_alert = (UIAlertView)sender;
			if (args.ButtonIndex == 0) {
				if (parent_alert.GetTextField (0).Text != "") {
					loadingOverlay = new LoadingOverlay (UIScreen.MainScreen.Bounds);
					View.Add (loadingOverlay);
					Communication.GetList (parent_alert.GetTextField (0).Text, parent_alert.GetTextField (1).Text, delegate(Communication.Response response) {
						InvokeOnMainThread (() => {
							switch (response.Status) {
							case HttpStatusCode.OK:
								Catalog.Instance.Lists.Insert (0, response.List);
								Catalog.Instance.Save();
								using (var indexPath = NSIndexPath.FromRowSection (0, 0)) {
									table.InsertRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
								}
								break;
							case HttpStatusCode.Unauthorized:
							case HttpStatusCode.NotFound:
								ShowAlert("Wrong list name or password",OpenList);
								break;
							default:
								ShowAlert("Connetion error: "+response.Status,OpenList);
								break;
							}
							loadingOverlay.Hide();
						});
					});
				} else {
					ShowAlert ("The lists's name can't be empty", CreateList);
				} 
			}
		}

		public override void PrepareForSegue (UIStoryboardSegue segue, NSObject sender)
		{
			if (segue.Identifier == "showList") {
				NSIndexPath indexPath =  table.IndexPathForSelectedRow;
				((ListViewController)segue.DestinationViewController).SetSource (Catalog.Instance[indexPath]);
			}
		}

		public void ShowAlert(string text,EventHandler<UIButtonEventArgs> action){
			UIAlertView erAlert = new UIAlertView ();
			erAlert.AddButton ("OK");
			erAlert.Message = text;
			erAlert.Clicked += action;
			erAlert.Show();
		}

		public void RemoveList (ToList list)
		{
			Catalog.Instance.Lists.Remove (list);
			table.ReloadData ();
		}
	}
}
