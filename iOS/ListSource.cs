using System;
using MonoTouch.UIKit;
using System.Collections.Generic;
using MonoTouch.Foundation;
using tofy;
using System.Linq;
using System.Threading;
using System.Net;

namespace Hikae
{
	public class ListSource : UITableViewSource {
		private int index;
		public ToList List {
			get {
				return Catalog.Instance.Lists [index];
			}
		}
		string cellIdentifier = "Cell";



		public ListSource (int index)
		{
			this.index = index;
		}
		public override int RowsInSection (UITableView tableview, int section)
		{
			return Catalog.Instance.Lists[index].Count;
		}
		public override UITableViewCell GetCell (UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
		{
			UITableViewCell cell = tableView.DequeueReusableCell (cellIdentifier);
			// if there are no cells to reuse, create a new one
			if (cell == null)
				cell = new UITableViewCell (UITableViewCellStyle.Default, cellIdentifier);

			cell.SelectionStyle = UITableViewCellSelectionStyle.None;

			cell.TextLabel.Text = Catalog.Instance.Lists[index][indexPath.Row].ToString();

			if (!Catalog.Instance.Lists[index][indexPath.Row].Synchronized) {
				UIActivityIndicatorView spinner = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.Gray);
				spinner.Frame = new System.Drawing.RectangleF (cell.Frame.Width-34, cell.Frame.Height/2-12, 24, 24);
				cell.AddSubview(spinner);
				cell.TextLabel.Text = cell.TextLabel.Text;
				spinner.StartAnimating ();
			}


			if (Catalog.Instance.Lists[index][indexPath.Row].Checked)
				cell.ImageView.Image = new UIImage ("checkbox-ticked.png");

			return cell;
		}

		public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
		{
			Catalog.Instance.Lists[index][indexPath.Row].Checked = !Catalog.Instance.Lists[index][indexPath.Row].Checked;
			tableView.ReloadRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);

			Action<Communication.Response> callback = delegate (Communication.Response response) {
				InvokeOnMainThread(() => {
					if (response.Status != HttpStatusCode.Created) {
							Catalog.Instance.Lists[index][indexPath.Row].Checked = !Catalog.Instance.Lists[index][indexPath.Row].Checked;
					}
					Catalog.Instance.Lists[index][indexPath.Row].Synchronized = true;
					tableView.ReloadRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
				});
			};

			Communication.CheckItem (Catalog.Instance.Lists[index].Name, Catalog.Instance.Lists[index][indexPath.Row].Name, Catalog.Instance.Lists[index].Password, 
				callback,
				Catalog.Instance.Lists[index][indexPath.Row].Checked
			);
		}
			
		public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, MonoTouch.Foundation.NSIndexPath indexPath)
		{
			switch (editingStyle) {
			case UITableViewCellEditingStyle.Delete:

				Communication.DeleteItem (Catalog.Instance.Lists[index].Name, Catalog.Instance.Lists[index][indexPath.Row].Name, Catalog.Instance.Lists[index].Password, delegate(Communication.Response response) {
					//if (response.Status == HttpStatusCode.OK)
						//MasterViewController.SaveChanges ();
				}) ;

				// remove the item from the underlying data source
				Catalog.Instance.Lists[index].RemoveAt (indexPath.Row);
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

			Catalog.Instance.Lists[index].Insert (0,new ToItem(itemName));

			if (table != null) {
				using (var indexPath = NSIndexPath.FromRowSection (0, 0))
					table.InsertRows (new NSIndexPath[] { indexPath }, UITableViewRowAnimation.Automatic);
				table.EndUpdates ();
			}
		}

		public void updateItem(string itemName, bool synchronized, UITableView table = null) {
			if (table != null)
				table.BeginUpdates ();

			ToItem it = Catalog.Instance.Lists[index][itemName];
			it.Synchronized = true;

			if (table != null) {
				table.EndUpdates ();
				table.ReloadRows (new NSIndexPath[]{ NSIndexPath.FromRowSection (Catalog.Instance.Lists[index].IndexOf (it), 0) }, UITableViewRowAnimation.Fade);
			}
		}

		public bool contains(string itemName){
			return Catalog.Instance.Lists[index][itemName] != null;
		}
	}
}

