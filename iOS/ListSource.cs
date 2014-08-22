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
		public int index;
		public ToList List {
			get {
				if (index < Catalog.Instance.Lists.Count)
					return Catalog.Instance.Lists [index];
				else
					return null;
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
			
		private UIActivityIndicatorView getSpinner(UITableViewCell cell) {
			UIActivityIndicatorView spinner=null;
			foreach (UIView sv in cell.Subviews)
				if (sv is UIActivityIndicatorView)
					spinner = (UIActivityIndicatorView)sv;
					
			if (spinner == null) {
				spinner = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.Gray);
				spinner.HidesWhenStopped = true;
				spinner.Frame = new System.Drawing.RectangleF (cell.Frame.Width - 34, cell.Frame.Height / 2 - 12, 24, 24);
				cell.AddSubview (spinner);
			}

			return spinner;
		}


		private UIImage ticked = new UIImage ("checkbox-ticked@2x.png");
		private UIImage unticked = new UIImage ("checkbox@2x.png");

		public override UITableViewCell GetCell (UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
		{
			UITableViewCell cell = tableView.DequeueReusableCell (cellIdentifier);
			// if there are no cells to reuse, create a new one
			if (cell == null)
				cell = new UITableViewCell (UITableViewCellStyle.Default, cellIdentifier);

			cell.SelectionStyle = UITableViewCellSelectionStyle.None;
			cell.TextLabel.Text = Catalog.Instance.Lists[index][indexPath.Row].ToString();

			if (!Catalog.Instance.Lists [index] [indexPath.Row].Synchronized)
				getSpinner(cell).StartAnimating ();
			else
				getSpinner(cell).StopAnimating ();
				
			if (Catalog.Instance.Lists[index][indexPath.Row].Checked && cell.ImageView.Image != ticked)
				cell.ImageView.Image = ticked;

			if (!Catalog.Instance.Lists[index][indexPath.Row].Checked && cell.ImageView.Image != unticked)
				cell.ImageView.Image = unticked;

			return cell;
		}

		public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
		{
			Catalog.Instance.Lists[index][indexPath.Row].Checked = !Catalog.Instance.Lists[index][indexPath.Row].Checked;

			Communication.CheckItem (
				Catalog.Instance.Lists[index].Name, 
				Catalog.Instance.Lists[index][indexPath.Row].Name, 
				Catalog.Instance.Lists[index].Password, 
				(Communication.Response response) => {
					InvokeOnMainThread(() => {
						if (response.Status != HttpStatusCode.Created) {
							Catalog.Instance.Lists[index][indexPath.Row].Checked = !Catalog.Instance.Lists[index][indexPath.Row].Checked;
						}
						Catalog.Instance.Lists[index][indexPath.Row].Synchronized = true;
					});
				},
				Catalog.Instance.Lists[index][indexPath.Row].Checked
			);
		}
			
		public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, MonoTouch.Foundation.NSIndexPath indexPath)
		{
			switch (editingStyle) {
			case UITableViewCellEditingStyle.Delete:
				Communication.DeleteItem (Catalog.Instance.Lists [index].Name, Catalog.Instance.Lists [index] [indexPath.Row].Name, Catalog.Instance.Lists [index].Password, 
					(Communication.Response response)  => {}
				);
				Catalog.Instance.Lists [index].RemoveAt (indexPath.Row);
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

		public bool contains(string itemName){
			return Catalog.Instance.Lists[index][itemName] != null;
		}
	}
}

