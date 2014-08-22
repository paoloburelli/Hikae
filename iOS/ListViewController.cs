using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;
using tofy;
using System.Threading;
using System.Net;
using System.Linq;

namespace Hikae
{
	partial class ListViewController : UIViewController
	{
		ListSource tableSource;
		UIAlertView alert;
		LoadingOverlay loadingOverlay;
		private UINavigationController controller;

		public ListViewController (IntPtr handle) : base (handle)
		{
		}

		public void SetSource (ListSource ls)
		{
			this.tableSource = ls;
		}

		void ConfigureView ()
		{
			controller = NavigationController;

			// Update the user interface for the detail item
			if (IsViewLoaded && tableSource != null && tableSource.List != null) {

				table.Source = tableSource;

				Title = tableSource.List.ToString ();
				loadingOverlay = new LoadingOverlay (View.Frame);
				View.Add (loadingOverlay);

				Communication.GetList (tableSource.List.Name, tableSource.List.Password, delegate(Communication.Response response) {
					InvokeOnMainThread (() => {
						switch (response.Status) {
						case HttpStatusCode.OK:
							tableSource.List.Synch (response.List);
							loadingOverlay.Hide ();
							break;
						case HttpStatusCode.NotFound: 
							ShowAlert (tableSource.List.Name + " has been deleted", (object s, UIButtonEventArgs e) => {
								((MainViewController)NavigationController.ChildViewControllers [0]).RemoveList (tableSource.List);
								NavigationController.PopViewControllerAnimated (true);
								loadingOverlay.Hide ();
							});
							break;
						case HttpStatusCode.Unauthorized:
							UIAlertView alert = new UIAlertView ();
							alert.Message = "Wrong password, please enter the correct one";
							alert.AddButton ("OK");
							alert.AddButton ("Cancel");
							alert.AlertViewStyle = UIAlertViewStyle.SecureTextInput;
							alert.GetTextField (0).Placeholder = "Password";
							alert.Clicked += (object sender, UIButtonEventArgs e) => {
								loadingOverlay.Hide ();
								if (e.ButtonIndex == 0) {
									tableSource.List.Password = alert.GetTextField (0).Text;
								} else {
									NavigationController.PopViewControllerAnimated (true);
								}
							};
							alert.Show ();
							break;
						default: 
							ShowAlertWithTimout ("No connection, working off-line", 1000, () => {
								loadingOverlay.Hide ();
							});
							break;
						}
					});
				}
				);

				ToList.UpdateUI += HandleUiChanges;
			}
		}

		public void HandleUiChanges (ToList lst, int index)
		{
			if (tableSource.List != null) {
				InvokeOnMainThread (() => {
					if (index == -1) {
						if (Title == lst.Name && Catalog.Instance.Lists.Count (l => l.Name == lst.Name) == 0) {
							if (IsViewLoaded) {
								ToList.UpdateUI -= HandleUiChanges;
								ShowAlert (lst.Name + " has been deleted", (object s, UIButtonEventArgs e) => {
									controller.PopViewControllerAnimated (true);
								});
							}
						} else if (lst.Password == ToList.PASSWORD_CHANGED_FLAG) {
							if (IsViewLoaded) {
								ShowAlert ("The password on " + lst.Name + " has been changed by someone", (object s, UIButtonEventArgs e) => {
									controller.PopViewControllerAnimated (true);
								});
								lst.Password = null;
							}
						} else if (lst.Name.ToLower () == tableSource.List.Name.ToLower ())
							table.ReloadData ();
					} else if (lst.Name.ToLower () == tableSource.List.Name.ToLower ()) {
						if (table.NumberOfRowsInSection (0) < lst.Count)
							table.InsertRows (new NSIndexPath[] { NSIndexPath.FromRowSection (index, 0) }, UITableViewRowAnimation.Automatic);
						else if (table.NumberOfRowsInSection (0) > lst.Count)
							table.DeleteRows (new NSIndexPath[] { NSIndexPath.FromRowSection (index, 0) }, UITableViewRowAnimation.Automatic);
						else
							table.ReloadRows (new NSIndexPath[] { NSIndexPath.FromRowSection (index, 0) }, UITableViewRowAnimation.Automatic);
					} 
					Title = tableSource.List.ToString ();
				});
			}
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
		}

		void DeleteList (object sender, EventArgs args)
		{
			UIAlertView alert = new UIAlertView ();
			alert.Message = "Delete list " + tableSource.List.Name;
			alert.AddButton ("Ok");
			alert.AddButton ("Cancel");
			alert.Clicked += (object parent_sender, UIButtonEventArgs e) => {
				if (e.ButtonIndex == 0) {
					View.Add (loadingOverlay);
					Communication.DeleteList (tableSource.List.Name, tableSource.List.Password, delegate(Communication.Response response) {
						InvokeOnMainThread (() => {
							loadingOverlay.Hide ();
							if (response.Status == HttpStatusCode.NoContent) {
								((MainViewController)NavigationController.ChildViewControllers [0]).RemoveList (tableSource.List);
								NavigationController.PopViewControllerAnimated (true);
							} else
								ShowAlert ("List could not be deleted, error: " + response.Status);
						});
					});
				} 
			}; 
			alert.Show ();
		}

		void ChangePassword (object sender, EventArgs e)
		{
			UIAlertView alert = new UIAlertView ();
			alert.Message = "Set new password, leave empty to remove the password";
			alert.AddButton ("OK");
			alert.AddButton ("Cancel");
			alert.AlertViewStyle = UIAlertViewStyle.LoginAndPasswordInput;
			alert.GetTextField (0).SecureTextEntry = true;
			alert.GetTextField (0).Placeholder = "Password";
			alert.GetTextField (1).Placeholder = "Repeat Password";

			alert.Clicked += (object parent_sender, UIButtonEventArgs be) => {
				if (be.ButtonIndex == 0) {
					if (alert.GetTextField (0).Text == alert.GetTextField (1).Text) {
						Communication.ChangePassword (tableSource.List.Name, tableSource.List.Password, alert.GetTextField (0).Text, delegate(Communication.Response response) {
							InvokeOnMainThread (() => {
								if (response.Status == HttpStatusCode.Created) {
									tableSource.List.Password = alert.GetTextField (0).Text;
									ShowAlert ("Password changed successfuly");
								} else {
									ShowAlert ("Password not set, error: " + response.Status);
								}
							});
						});
					} else
						ShowAlert ("The passwords do not match");
				}
			}; 
			alert.Show ();
		}

		void refresh (UIRefreshControl refreshControl = null)
		{
			Communication.GetList (tableSource.List.Name, tableSource.List.Password, delegate(Communication.Response response) {
				InvokeOnMainThread (() => {
					if (response.Status == HttpStatusCode.OK) {
						tableSource.List.Synch (response.List);
					}
					if (refreshControl != null)
						refreshControl.EndRefreshing ();
				});
			});
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			UIBarButtonItem addButton = new UIBarButtonItem (UIBarButtonSystemItem.Add, AddNewItem);
			UIBarButtonItem deleteButton = new UIBarButtonItem (UIBarButtonSystemItem.Trash, DeleteList);
			UIBarButtonItem emptySpace = new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace);


			NavigationItem.RightBarButtonItem = new UIBarButtonItem (UIImage.FromFile ("observer@2x.png"), UIBarButtonItemStyle.Plain, ShowObservers);

			UIImage lockIcon = UIImage.FromFile ("lock@2x.png");

			UIBarButtonItem changePassword = new UIBarButtonItem (lockIcon, UIBarButtonItemStyle.Plain, ChangePassword);
			changePassword.Width = addButton.Width;


			table.BackgroundView = new UIImageView (new UIImage ("paper.jpg"));

			deleteButton.TintColor = UIColor.White;
			changePassword.TintColor = UIColor.White;
			addButton.TintColor = UIColor.White;

			toolbar.SetItems (new UIBarButtonItem[] {
				emptySpace,
				deleteButton,
				emptySpace,
				changePassword,
				emptySpace,
				addButton,
				emptySpace
			}, true);
				
			UIRefreshControl RefreshControl = new UIRefreshControl ();
			RefreshControl.AddTarget (delegate(object sender, EventArgs e) {
				refresh (RefreshControl);
			}, UIControlEvent.ValueChanged);

			table.AddSubview (RefreshControl);

			ConfigureView ();
		}

		void AddNewItem (object sender, EventArgs args)
		{
			UIAlertView alert = new UIAlertView ();
			alert.AddButton ("Ok");
			alert.AddButton ("Cancel");
			alert.Message = "Enter item's name";
			alert.AlertViewStyle = UIAlertViewStyle.PlainTextInput;
			alert.Clicked += AddConfirmed; 
			alert.GetTextField (0).AutocapitalizationType = UITextAutocapitalizationType.Sentences;
			alert.Show ();
		}

		void ShowObservers (object sender, EventArgs args)
		{
			UIAlertView alert = new UIAlertView ();
			alert.AddButton ("Ok");

			alert.Title = "List Users";

			alert.Message = "";

			int unknown = 0;
			foreach (string obs in tableSource.List.Observers) {
				string observer = obs.Trim ('"');
				if (observer != Device.Name && observer.ToLower () != "someone")
					alert.Message += observer + "\n";

				if (observer.ToLower () == "someone")
					unknown++;
			}

			if (unknown > 0)
				alert.Message += "Unknown users: " + unknown;

			if (alert.Message.Length == 0)
				alert.Message = "No one else is watching this list at the moment";


			alert.AlertViewStyle = UIAlertViewStyle.Default;
			alert.Show ();
		}

		void AddConfirmed (object sender, UIButtonEventArgs args)
		{
			UIAlertView parent_alert = (UIAlertView)sender;

			if (args.ButtonIndex == 0) {
				if (parent_alert.GetTextField (0).Text != "") {
					if (!tableSource.contains (parent_alert.GetTextField (0).Text)) {
						tableSource.List.Insert (0, new ToItem (parent_alert.GetTextField (0).Text));
						Communication.AddItem (tableSource.List.Name, parent_alert.GetTextField (0).Text, tableSource.List.Password, delegate(Communication.Response response) {
							if (response.Status == HttpStatusCode.Created) {
								InvokeOnMainThread (() => {
									ToItem it = tableSource.List [parent_alert.GetTextField (0).Text];
									it.Synchronized = true;
								});
							}
						});
					}
				} else
					ShowAlert ("The item's name can't be empty", AddNewItem);
			}
		}

		public void ShowAlert (string text, EventHandler<UIButtonEventArgs> action = null)
		{
			UIAlertView erAlert = new UIAlertView ();
			erAlert.AddButton ("OK");
			erAlert.Message = text;
			if (action != null)
				erAlert.Clicked += action;
			erAlert.Show ();
		}

		public void ShowAlertWithTimout (string text, int timeout, Action action = null)
		{
			alert = new UIAlertView ();
			alert.Message = text;
			alert.AlertViewStyle = UIAlertViewStyle.Default;
			alert.Show (); 

			ThreadPool.QueueUserWorkItem (delegate(object state) {
				Thread.Sleep (timeout);
				InvokeOnMainThread (() => {
					action ();
					alert.DismissWithClickedButtonIndex (-1, true);
				});
			});
		}

		public override void ViewWillDisappear (bool animated)
		{
			base.ViewWillDisappear (animated);
			if (tableSource.List != null)
				tableSource.List.ResetChanges ();
		}
	}
}
