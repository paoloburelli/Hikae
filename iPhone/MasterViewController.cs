using System;
using System.Drawing;
using System.Collections.Generic;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Core;

namespace iPhone
{
	public partial class MasterViewController : UITableViewController
	{
		DataSource dataSource;
		LoadingOverlay loadingOverlay;

		public MasterViewController (IntPtr handle) : base (handle)
		{
			Title = NSBundle.MainBundle.LocalizedString ("Hikae", "Hikae");

			// Custom initialization
		}

		void AddNewItem (object sender, EventArgs args)
		{
			UIAlertView alert = new UIAlertView ();
			alert.AddButton ("Open/Create List");
			alert.AddButton ("Cancel");
			alert.Message = "Please enter the name and the password for the list you want to create or open.";
			alert.AlertViewStyle = UIAlertViewStyle.LoginAndPasswordInput;
			alert.Clicked += AlertButtonClicked; 
			alert.Show ();
		}

		void AlertButtonClicked(object sender, UIButtonEventArgs args) {
			UIAlertView parent_alert = (UIAlertView)sender;

			if (args.ButtonIndex == 0) {
				loadingOverlay = new LoadingOverlay (UIScreen.MainScreen.Bounds);
				View.Add (loadingOverlay);

				ToFy.GetList (parent_alert.GetTextField (0).Text, parent_alert.GetTextField (1).Text, delegate(ToFy.Response response) {
					if (response.status == ToFy.Status.NotFound) {
						InvokeOnMainThread (() => {
							ToFy.AddList (parent_alert.GetTextField (0).Text, parent_alert.GetTextField (1).Text, delegate(ToFy.Response resp) {
								InvokeOnMainThread (() => {
									handleResponse (resp);
								});
							});
						});
					} else {
						InvokeOnMainThread (() => {
							handleResponse (response);
						});
					}
				});
			} 
		}

		private void handleResponse(ToFy.Response response){
			switch (response.status) {
			case ToFy.Status.Ok:
				gotObject(response.list);
				break;
			case ToFy.Status.Unauthorized:
				wrongPassword ();
				break;
			default:
				generalError(response.status);
				break;
			}
		}

		private void gotObject(ToFy.List list) {
			dataSource.Objects.Insert (0, list);

			using (var indexPath = NSIndexPath.FromRowSection (0, 0)) {
				TableView.InsertRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
				//TableView.SelectRow (indexPath, true, UITableViewScrollPosition.Top);
			}

			loadingOverlay.Hide ();
		}

		private void wrongPassword() {
			UIAlertView alert = new UIAlertView ();

			alert.AddButton ("Ok");
			alert.Message = "Wrong password!";
			alert.AlertViewStyle = UIAlertViewStyle.Default;
			alert.Clicked += (object s, UIButtonEventArgs e) => {
				loadingOverlay.Hide ();
			}; 
			alert.Show ();
		}

		private void generalError(ToFy.Status status) {
			UIAlertView alert = new UIAlertView ();

			alert.AddButton ("Ok");
			alert.Message = "Error: " + status.ToString ();
			alert.AlertViewStyle = UIAlertViewStyle.Default;
			alert.Clicked += (object s, UIButtonEventArgs e) => {
				loadingOverlay.Hide ();
			}; 
			alert.Show ();
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
			
			// Perform any additional setup after loading the view, typically from a nib.
			//NavigationItem.LeftBarButtonItem = EditButtonItem;

			var addButton = new UIBarButtonItem (UIBarButtonSystemItem.Add, AddNewItem);
			NavigationItem.RightBarButtonItem = addButton;

			TableView.Source = dataSource = new DataSource (this);
		}

		class DataSource : UITableViewSource
		{
			static readonly NSString CellIdentifier = new NSString ("Cell");
			readonly List<ToFy.List> objects = new List<ToFy.List> ();
			readonly MasterViewController controller;

			public DataSource (MasterViewController controller)
			{
				this.controller = controller;
			}

			public IList<ToFy.List> Objects {
				get { return objects; }
			}

			// Customize the number of sections in the table view.
			public override int NumberOfSections (UITableView tableView)
			{
				return 1;
			}

			public override int RowsInSection (UITableView tableview, int section)
			{
				return objects.Count;
			}

			// Customize the appearance of table view cells.
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				var cell = (UITableViewCell)tableView.DequeueReusableCell (CellIdentifier, indexPath);

				cell.TextLabel.Text = objects [indexPath.Row].ToString ();

				return cell;
			}

			public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
			{
				// Return false if you do not want the specified item to be editable.
				return true;
			}

			public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
			{
				if (editingStyle == UITableViewCellEditingStyle.Delete) {
					// Delete the row from the data source.
					objects.RemoveAt (indexPath.Row);
					controller.TableView.DeleteRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Fade);
				} else if (editingStyle == UITableViewCellEditingStyle.Insert) {
					// Create a new instance of the appropriate class, insert it into the array, and add a new row to the table view.
				}
			}

			/*
			// Override to support rearranging the table view.
			public override void MoveRow (UITableView tableView, NSIndexPath sourceIndexPath, NSIndexPath destinationIndexPath)
			{
			}
			*/

			/*
			// Override to support conditional rearranging of the table view.
			public override bool CanMoveRow (UITableView tableView, NSIndexPath indexPath)
			{
				// Return false if you do not want the item to be re-orderable.
				return true;
			}
			*/
		}

		public override void PrepareForSegue (UIStoryboardSegue segue, NSObject sender)
		{
			if (segue.Identifier == "showDetail") {
				var indexPath = TableView.IndexPathForSelectedRow;
				var item = dataSource.Objects [indexPath.Row];

				((DetailViewController)segue.DestinationViewController).SetDetailItem (item);
			}
		}
	}
}

