using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;
using tofy;
using System.Net;
using System.Collections.Generic;
using System.Linq;

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

			ToList.UpdateUI += (ToList lst, int index) => {
					InvokeOnMainThread(() => {
						int listIndex = Catalog.Instance.Lists.FindLastIndex(l => l.Name == lst.Name);
						if (listIndex > -1)
							table.ReloadRows(new NSIndexPath[] { NSIndexPath.FromRowSection(listIndex,0)},UITableViewRowAnimation.Automatic);
						else
							table.ReloadData();
					});
			};

//			UIRefreshControl RefreshControl = new UIRefreshControl();
//			RefreshControl.AddTarget (delegate(object sender, EventArgs e) {
//				Catalog.Instance.Refresh(RefreshControl);
//			}, UIControlEvent.ValueChanged);
//
//			table.AddSubview (RefreshControl);
		}

		void CreateList (object sender, EventArgs args)
		{
			UIAlertView alert = new UIAlertView ();
			alert.AddButton ("Create List");
			alert.AddButton ("Cancel");
			alert.Message = "Enter the name of the list you want to create. If you want to protect the list, type also a password";
			alert.AlertViewStyle = UIAlertViewStyle.LoginAndPasswordInput;
			alert.Clicked += CreateAccepted; 
			alert.GetTextField (0).AutocapitalizationType = UITextAutocapitalizationType.Words;
			alert.GetTextField (0).Placeholder = "List Name";
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
								AddList(new ToList(parent_alert.GetTextField (0).Text,parent_alert.GetTextField (1).Text));
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

		string lastListName="";
		void OpenList (object sender, EventArgs args)
		{
			UIAlertView alert = new UIAlertView ();
			alert.AddButton ("Open List");
			alert.AddButton ("Cancel");
			alert.Message = "Enter the name of the list you want to open and its password";
			alert.AlertViewStyle = UIAlertViewStyle.LoginAndPasswordInput;
			alert.GetTextField (0).AutocapitalizationType = UITextAutocapitalizationType.Words;
			if (lastListName != "")
				alert.GetTextField (0).Text = lastListName;
			alert.GetTextField (0).Placeholder = "List Name";
			alert.Clicked += OpenAccepted; 
			alert.Show ();
		}

		void OpenAccepted (object sender, UIButtonEventArgs args)
		{
			UIAlertView parent_alert = (UIAlertView)sender;
			if (args.ButtonIndex == 0) {
				if (parent_alert.GetTextField (0).Text != "") {
					lastListName = parent_alert.GetTextField (0).Text;
					loadingOverlay = new LoadingOverlay (UIScreen.MainScreen.Bounds);
					View.Add (loadingOverlay);
					Communication.GetList (parent_alert.GetTextField (0).Text, parent_alert.GetTextField (1).Text, delegate(Communication.Response response) {
						InvokeOnMainThread (() => {
							switch (response.Status) {
							case HttpStatusCode.OK:
								response.List.Password = parent_alert.GetTextField (1).Text;
								AddList(response.List);
								lastListName = "";
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
				Communication.RegisterForNotifications(Catalog.Instance[indexPath].List.Name,Catalog.Instance[indexPath].List.Password);
			}
		}

		public void ShowAlert(string text,EventHandler<UIButtonEventArgs> action){
			UIAlertView erAlert = new UIAlertView ();
			erAlert.AddButton ("OK");
			erAlert.Message = text;
			erAlert.Clicked += action;
			erAlert.Show();
		}

		void AddList (ToList list)
		{
			Catalog.Instance.Lists.Insert (0, list);
			Catalog.Instance.Save();
			using (var indexPath = NSIndexPath.FromRowSection (0, 0)) {
				table.InsertRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
			}
			Communication.RegisterForNotifications(list.Name,list.Password);
		}

		public void RemoveList (ToList list)
		{
			int listIndex = Catalog.Instance.Lists.IndexOf (list);
			if (listIndex > -1) {
				Catalog.Instance.Lists.Remove (list);
				table.DeleteRows (new NSIndexPath[] { NSIndexPath.FromRowSection (listIndex, 0) }, UITableViewRowAnimation.Automatic);
			}
		}
	}
}
