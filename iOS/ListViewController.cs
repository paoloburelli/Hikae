using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;
using tofy;
using System.Threading;
using System.Net;

namespace Hikae
{
	partial class ListViewController : UIViewController
	{
		ListSource tableSource;
		UIAlertView alert;
		LoadingOverlay loadingOverlay;

		public ListViewController (IntPtr handle) : base (handle)
		{
		}

		public void SetSource (ListSource ls)
		{
			this.tableSource = ls;
			ConfigureView ();
		}

		void ConfigureView ()
		{
			// Update the user interface for the detail item
			if (IsViewLoaded && tableSource != null && tableSource.List != null) {

				table.Source = tableSource;

				Title = tableSource.List.ToString ();
				loadingOverlay = new LoadingOverlay (View.Frame);
				View.Add (loadingOverlay);

				Communication.GetList (tableSource.List.Name,tableSource.List.Password,delegate(Communication.Response response){
					InvokeOnMainThread ( () => {
						switch(response.Status) {
						case HttpStatusCode.OK:
							tableSource.List.Synch(response.List);
							table.ReloadData();
							loadingOverlay.Hide ();
							break;
						case HttpStatusCode.NotFound: 
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
						case HttpStatusCode.Unauthorized:
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
							alert = new UIAlertView ();
							table.ReloadData();
							alert.Message = "No connection, working offline";
							alert.AlertViewStyle = UIAlertViewStyle.Default;
							alert.Show (); 

							ThreadPool.QueueUserWorkItem(delegate(object state) {
								Thread.Sleep(1000);
								InvokeOnMainThread(() => {
									loadingOverlay.Hide ();
									alert.DismissWithClickedButtonIndex(-1,true);
								});
							});


							break;
						}
					});
				}
				);
			}
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
		}

		void DeleteList (object sender, EventArgs args)
		{
			UIAlertView alert = new UIAlertView ();
			alert.AddButton ("Delete List");
			alert.AddButton ("Cancel");
			alert.Clicked += (object parent_sender, UIButtonEventArgs e) => {
				if (e.ButtonIndex == 0) {
					View.Add (loadingOverlay);
					Communication.DeleteList(tableSource.List.Name,tableSource.List.Password,delegate(Communication.Response response) {
						InvokeOnMainThread(() => {
							loadingOverlay.Hide();
							if (response.Status == HttpStatusCode.NoContent) {
								((MainViewController)NavigationController.ParentViewController).RemoveList(tableSource.List);
								NavigationController.PopViewControllerAnimated(true);
							} else
								ShowAlert("List could not be deleted, error: "+response.Status);
						});
					});
				} 
			}; 
			alert.Show ();
		}

		void ChangePassword(object sender, EventArgs e){
			UIAlertView alert = new UIAlertView ();
			alert.Message = "Set new password, leave empty to remove the password";
			alert.AddButton ("OK");
			alert.AddButton ("Cancel");
			alert.AlertViewStyle = UIAlertViewStyle.LoginAndPasswordInput;
			alert.GetTextField (0).SecureTextEntry = true;
			alert.GetTextField(0).Placeholder = "Password";
			alert.GetTextField(1).Placeholder = "Repeat Password";

			alert.Clicked += (object parent_sender, UIButtonEventArgs be) => {
				if (be.ButtonIndex == 0) {
					if (alert.GetTextField (0).Text == alert.GetTextField (1).Text){
						Communication.ChangePassword(tableSource.List.Name,tableSource.List.Password,alert.GetTextField(0).Text,delegate(Communication.Response response) {
							InvokeOnMainThread(() => {
								if (response.Status == HttpStatusCode.Created){
									tableSource.List.Password = alert.GetTextField (0).Text;
									ShowAlert("Password changed successfuly");
								} else {
									ShowAlert("Password not set, error: "+response.Status);
								}
							});
						});
					} else ShowAlert("The passwords do not match");
				}
			}; 
			alert.Show ();
		}

		void refresh (UIRefreshControl refreshControl=null){
			Communication.GetList(tableSource.List.Name,tableSource.List.Password,delegate(Communication.Response response) {
				InvokeOnMainThread(() => {
					if (response.Status == HttpStatusCode.OK){
						tableSource.List.Synch(response.List);
						table.ReloadData();
					}
					if (refreshControl != null)
						refreshControl.EndRefreshing();
				});
			});
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			UIBarButtonItem addButton = new UIBarButtonItem (UIBarButtonSystemItem.Add, AddNewItem);
			UIBarButtonItem deleteButton = new UIBarButtonItem(UIBarButtonSystemItem.Trash, DeleteList);
			UIBarButtonItem emptySpace = new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace);

			UIImage lockIcon = new UIImage ("lock.png");
			UIBarButtonItem changePassword = new UIBarButtonItem (lockIcon, UIBarButtonItemStyle.Plain,  ChangePassword);


			table.BackgroundView = new UIImageView (new UIImage ("paper.jpg"));

			deleteButton.TintColor = UIColor.White;
			changePassword.TintColor = UIColor.White;
			addButton.TintColor = UIColor.White;

			toolbar.SetItems(new UIBarButtonItem[] {
				emptySpace,
				deleteButton,
				emptySpace,
				changePassword,
				emptySpace,
				addButton,
				emptySpace
			},true);
				
			UIRefreshControl RefreshControl = new UIRefreshControl();
			RefreshControl.AddTarget (delegate(object sender, EventArgs e) {
				refresh(RefreshControl);
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
			alert.Show ();
		}

		void AddConfirmed(object sender, UIButtonEventArgs args) {
			UIAlertView parent_alert = (UIAlertView)sender;

			if (args.ButtonIndex == 0) {
				if (parent_alert.GetTextField (0).Text != "") {
					if (!tableSource.contains (parent_alert.GetTextField (0).Text)) {
						tableSource.addItem (parent_alert.GetTextField (0).Text, table);
						Communication.AddItem (tableSource.List.Name, parent_alert.GetTextField (0).Text, tableSource.List.Password, delegate(Communication.Response response) {
							if (response.Status == HttpStatusCode.Created) {
								InvokeOnMainThread (() => {
									tableSource.updateItem (parent_alert.GetTextField (0).Text, true, table);
								});
							}
						});
					}
				} else ShowAlert ("The item's name can't be empty", AddNewItem);
			}
		}

		public void ShowAlert(string text,EventHandler<UIButtonEventArgs> action=null){
			UIAlertView erAlert = new UIAlertView ();
			erAlert.AddButton ("OK");
			erAlert.Message = text;
			if (action != null)
				erAlert.Clicked += action;
			erAlert.Show();
		}
	}
}
