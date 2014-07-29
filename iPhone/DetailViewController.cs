using System;
using System.Drawing;
using System.Collections.Generic;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Core;
using System.Threading;
using System.Linq;

namespace iPhone
{
	public partial class DetailViewController : UIViewController
	{
		TableSource tableSource;
		UIAlertView alert;
		LoadingOverlay loadingOverlay;

		public DetailViewController (IntPtr handle) : base (handle)
		{
		}

		public void SetDetailItem (ToFy.List newDetailItem)
		{
			if (tableSource == null){
				if (newDetailItem != null)
					tableSource = new TableSource (newDetailItem);
			} else if(tableSource.list != newDetailItem) {
				tableSource = new TableSource(newDetailItem);
			}

			ConfigureView ();
		}

		void ConfigureView ()
		{
			// Update the user interface for the detail item
			if (IsViewLoaded && tableSource != null && tableSource.list != null) {
				Title = tableSource.list.ToString ();
				loadingOverlay = new LoadingOverlay (UIScreen.MainScreen.Bounds);
				View.Add (loadingOverlay);

				ToFy.GetList (tableSource.list.name,tableSource.list.password,delegate(ToFy.Response response){
					InvokeOnMainThread ( () => {
						switch(response.status) {
						case ToFy.Status.Ok:
							tableSource = new TableSource (response.list);
							table.Source = tableSource;
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
			base.DidReceiveMemoryWarning ();
		}

		void DeleteList (object sender, EventArgs args)
		{
			UIAlertView alert = new UIAlertView ();
			alert.AddButton ("Ok");
			alert.AddButton ("Cancel");
			alert.Message = "Do you want to delete "+tableSource.list.name+"?";
			alert.Clicked += (object parent_sender, UIButtonEventArgs e) => {
				if (e.ButtonIndex == 0) {
					NavigationController.PopViewControllerAnimated(true);
					/**
					 * TODO: Finish deletion of the list
					 * 
					 **/
					//((MasterViewController)NavigationController.ViewControllers[0]).dataSource
				}
			}; 
			alert.Show ();
		}



		void ShareList (object sender, EventArgs args)
		{

		}

		void refresh (UIRefreshControl refreshControl=null){
			ToFy.GetList(tableSource.list.name,tableSource.list.password,delegate(ToFy.Response response) {
				InvokeOnMainThread(() => {
					if (response.status == ToFy.Status.Ok){
						tableSource = new TableSource (response.list);
						table.Source = tableSource;
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
//			UIBarButtonItem refreshButton = new UIBarButtonItem (UIBarButtonSystemItem.Refresh, delegate(object sender, EventArgs e) {
//				refresh();
//			});
			UIBarButtonItem changePassword = new UIBarButtonItem (UIBarButtonSystemItem.Bookmarks, delegate(object sender, EventArgs e) {

			});
		
//			refreshButton.TintColor = UIColor.White;
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

					ToFy.AddItem (tableSource.list.name, parent_alert.GetTextField (0).Text, tableSource.list.password, delegate(ToFy.Response response) {
						if (response.status == ToFy.Status.ConnectionFailed){
							Thread.Sleep(1000);
							AlertButtonClicked(parent_alert,args);
						} else if (response.status == ToFy.Status.Ok){
							InvokeOnMainThread(() =>{
								tableSource.updateItem(parent_alert.GetTextField (0).Text,true,table);
							});
						}
					});
				}
			}
		}
	}
}

