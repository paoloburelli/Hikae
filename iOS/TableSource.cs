using System;
using MonoTouch.UIKit;
using System.Collections.Generic;
using MonoTouch.Foundation;
using tofy;
using System.Linq;
using System.Threading;

namespace Hikae
{
	public class TableSource : UITableViewSource {
		public readonly ToList list;
		string cellIdentifier = "Cell";

		public TableSource (ref ToList list)
		{
			this.list = list;
		}
		public override int RowsInSection (UITableView tableview, int section)
		{
			return list.items.Count;
		}
		public override UITableViewCell GetCell (UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
		{
			UITableViewCell cell = tableView.DequeueReusableCell (cellIdentifier);
			// if there are no cells to reuse, create a new one
			if (cell == null)
				cell = new UITableViewCell (UITableViewCellStyle.Default, cellIdentifier);

			cell.SelectionStyle = UITableViewCellSelectionStyle.None;

			cell.TextLabel.Text = list.items[indexPath.Row].ToString();

			if (!list.items [indexPath.Row].synchronized) {
				UIActivityIndicatorView spinner = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.Gray);
				spinner.Frame = new System.Drawing.RectangleF (cell.Frame.Width-34, cell.Frame.Height/2-12, 24, 24);
				cell.AddSubview(spinner);
				cell.TextLabel.Text = cell.TextLabel.Text;
				spinner.StartAnimating ();
			}


			if (list.items [indexPath.Row].isChecked)
				cell.ImageView.Image = new UIImage ("checkbox-ticked.png");

			return cell;
		}

		public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
		{
			list.items [indexPath.Row].isChecked = !list.items [indexPath.Row].isChecked;
			tableView.ReloadRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);

			Action<Communication.Response> callback = delegate (Communication.Response response) {
				if (response.status != Communication.Status.Ok) {
					InvokeOnMainThread(() => {
						list.items [indexPath.Row].isChecked = !list.items [indexPath.Row].isChecked;
						tableView.ReloadRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
						MasterViewController.SaveChanges ();
					});
				}
			};

			if (list.items [indexPath.Row].isChecked)
				Communication.CheckItem (list.name, list.items [indexPath.Row].name, list.password, callback);
			else
				Communication.UncheckItem (list.name, list.items [indexPath.Row].name, list.password, callback);

		}
			
		public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, MonoTouch.Foundation.NSIndexPath indexPath)
		{
			switch (editingStyle) {
			case UITableViewCellEditingStyle.Delete:

				Communication.DeleteItem (list.name, list.items [indexPath.Row].name, list.password, delegate(Communication.Response response) {
					if (response.status == Communication.Status.Ok)
						MasterViewController.SaveChanges ();
				}) ;

				// remove the item from the underlying data source
				list.items.RemoveAt (indexPath.Row);
				// delete the row from the table
				tableView.DeleteRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Fade);

				break;
			case UITableViewCellEditingStyle.None:
				Console.WriteLine ("CommitEditingStyle:None called");
				break;
			}
		}
		public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
		{
			return true;
		}

		public void addItem(string itemName, UITableView table=null) {
			if (table != null)
				table.BeginUpdates ();

			list.items.Insert (0,new ToItem(itemName,false,false));

			if (table != null) {
				using (var indexPath = NSIndexPath.FromRowSection (0, 0))
					table.InsertRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
				table.EndUpdates ();
			}
		}

		public void updateItem(string itemName, bool synchronized, UITableView table = null) {
			if (table != null)
				table.BeginUpdates ();

			ToItem it = list.items.First(p => p.name == itemName);
			it.synchronized = true;

			if (table != null) {
				table.EndUpdates ();
				table.ReloadRows (new NSIndexPath[]{ NSIndexPath.FromRowSection (list.items.IndexOf (it), 0) }, UITableViewRowAnimation.Fade);
			}
		}

		public bool contains(string itemName){
			return list.items.Count (p => p.ToString () == itemName) != 0;
		}
	}
}

