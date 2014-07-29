using System;
using MonoTouch.UIKit;
using System.Collections.Generic;
using MonoTouch.Foundation;
using Core;
using System.Linq;
using System.Threading;

namespace iPhone
{
	public class TableSource : UITableViewSource {
		public readonly ToFy.List list;
		string cellIdentifier = "TableCell";

		public TableSource (ToFy.List list)
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
				cell.BackgroundColor = new UIColor (0.9f, 0.9f, 0.9f, 1.0f);
				cell.TextLabel.TextColor = UIColor.LightGray;
			}

			if (list.items[indexPath.Row].isChecked)
				cell.Accessory = UITableViewCellAccessory.Checkmark;


			return cell;
		}

		public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
		{
			list.items [indexPath.Row].isChecked = !list.items [indexPath.Row].isChecked;
			tableView.ReloadRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);

			setCheckMarkItem (list.name, list.items [indexPath.Row].name, list.password, list.items [indexPath.Row].isChecked);
		}

		private void setCheckMarkItem(string name,string itemName, string password, bool checkMark) {
			Action<ToFy.Response> callback = delegate (ToFy.Response response) {
				if (response.status == ToFy.Status.ConnectionFailed) {
					Thread.Sleep (1000);
					setCheckMarkItem(list.name, itemName, list.password,checkMark);
				}
			};

			if (checkMark)
				ToFy.CheckItem (list.name, itemName, list.password, callback);
			else
				ToFy.UncheckItem (list.name, itemName, list.password, callback);
		}

		private void deleteItem(string name,string itemName, string password) {
			Action<ToFy.Response> callback = delegate (ToFy.Response response) {
				if (response.status == ToFy.Status.ConnectionFailed) {
					Thread.Sleep (1000);
					deleteItem(list.name, itemName, list.password);
				}
			};

			ToFy.DeleteItem (list.name, itemName, list.password, callback);
		}

		public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, MonoTouch.Foundation.NSIndexPath indexPath)
		{
			switch (editingStyle) {
			case UITableViewCellEditingStyle.Delete:

				deleteItem (list.name, list.items [indexPath.Row].name, list.password);

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

			list.items.Insert (0,new ToFy.Item(itemName,false));

			if (table != null) {
				using (var indexPath = NSIndexPath.FromRowSection (0, 0))
					table.InsertRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
				table.EndUpdates ();
			}
		}

		public void updateItem(string itemName, bool synchronized, UITableView table = null) {
			if (table != null)
				table.BeginUpdates ();

			ToFy.Item it = list.items.First(p => p.name == itemName);
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

