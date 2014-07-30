using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;
using tofy;
using System.Threading;

namespace Hikae
{
	partial class ListViewController : UIViewController
	{
		TableSource tableSource;
		UIAlertView alert;
		LoadingOverlay loadingOverlay;

		public ListViewController (IntPtr handle) : base (handle)
		{
		}

		public void SetDetailItem (ref ToList newDetailItem)
		{

			if (tableSource == null){
				if (newDetailItem != null)
					tableSource = new TableSource (ref newDetailItem);
			} else if(tableSource.list != newDetailItem) {
				tableSource = new TableSource(ref newDetailItem);
			}

			ConfigureView ();
		}

		void ConfigureView ()
		{
			// Update the user interface for the detail item
			if (IsViewLoaded && tableSource != null && tableSource.list != null) {

				table.Source = tableSource;

				Title = tableSource.list.ToString ();
				loadingOverlay = new LoadingOverlay (View.Frame);
				View.Add (loadingOverlay);

				Communication.GetList (tableSource.list.name,tableSource.list.password,delegate(Communication.Response response){
					InvokeOnMainThread ( () => {
						switch(response.status) {
						case Communication.Status.Ok:
							tableSource.list.items = response.list.items;
							table.ReloadData();
							loadingOverlay.Hide ();
							break;
						case Communication.Status.NotFound: 
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
						case Communication.Status.Unauthorized:
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
			alert.AddButton ("Empty List");
			alert.AddButton ("Cancel");
			alert.Clicked += (object parent_sender, UIButtonEventArgs e) => {
				if (e.ButtonIndex == 0) {
					View.Add (loadingOverlay);
					Communication.DeleteList(tableSource.list.name,tableSource.list.password,delegate(Communication.Response response) {
						InvokeOnMainThread(() => {
							loadingOverlay.Hide();
							if (response.status == Communication.Status.Ok) {
								MasterViewController.RemoveList(tableSource.list);
								NavigationController.PopViewControllerAnimated(true);
							}
						});
					});
				} else if (e.ButtonIndex == 1) {
					View.Add (loadingOverlay);
					Communication.DeleteAllItems (tableSource.list.name, tableSource.list.password, delegate(Communication.Response response) {
						InvokeOnMainThread(() => {
							if (response.status == Communication.Status.Ok) {
								tableSource.list.items.Clear();
								table.ReloadData ();
								MasterViewController.SaveChanges ();
							} else {
								alert = new UIAlertView ();
								table.ReloadData();
								alert.AddButton("Ok");
								alert.Message = "Connection error, the list can't be cleared";
								alert.AlertViewStyle = UIAlertViewStyle.Default;
								alert.Show (); 
							}
							loadingOverlay.Hide();
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
						Communication.ChangePassword(tableSource.list.name,tableSource.list.password,alert.GetTextField(0).Text,delegate(Communication.Response response) {
							InvokeOnMainThread(() => {
								if (response.status == Communication.Status.Ok){
									tableSource.list.password = alert.GetTextField (0).Text;

									UIAlertView okAlert = new UIAlertView ();
									okAlert.AddButton ("OK");
									okAlert.Message = "Password changed successfuly";
									okAlert.Show();

									MasterViewController.SaveChanges();
								} else {
									UIAlertView noAlert = new UIAlertView ();
									noAlert.AddButton ("OK");
									noAlert.Message = "Password not set, error: "+response.status;
									noAlert.Show();
								}
							});
						});
					} else {
						UIAlertView erAlert = new UIAlertView ();
						erAlert.AddButton ("OK");
						erAlert.Message = "The passwords do not match";
						erAlert.Show();
					}
				}
			}; 
			alert.Show ();
		}

		void refresh (UIRefreshControl refreshControl=null){
			Communication.GetList(tableSource.list.name,tableSource.list.password,delegate(Communication.Response response) {
				InvokeOnMainThread(() => {
					if (response.status == Communication.Status.Ok){
						tableSource.list.items = response.list.items;
						table.ReloadData();
						MasterViewController.SaveChanges();
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

			UIImage lockIcon = new UIImage ("lock-22.png");
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

			//			NavigationItem.RightBarButtonItem = refreshButton;


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
			alert.Clicked += AlertButtonClicked; 
			alert.Show ();
		}

		void AlertButtonClicked(object sender, UIButtonEventArgs args) {
			UIAlertView parent_alert = (UIAlertView)sender;

			if (args.ButtonIndex == 0) {
				if (!tableSource.contains(parent_alert.GetTextField (0).Text)) {
					tableSource.addItem (parent_alert.GetTextField (0).Text, table);
					Communication.AddItem (tableSource.list.name, parent_alert.GetTextField (0).Text, tableSource.list.password, delegate(Communication.Response response) {
						if (response.status == Communication.Status.Ok){
							InvokeOnMainThread(() =>{
								tableSource.updateItem(parent_alert.GetTextField (0).Text,true,table);
								MasterViewController.SaveChanges ();
							});
						}
					});
				}
			}
		}
	}
}
