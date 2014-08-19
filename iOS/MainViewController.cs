using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;

namespace Hikae
{
	partial class MainViewController : UIViewController
	{
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
		}

		void CreateList (object sender, EventArgs args)
		{

		}

		void OpenList (object sender, EventArgs args)
		{

		}

		public override void PrepareForSegue (UIStoryboardSegue segue, NSObject sender)
		{
			if (segue.Identifier == "showList") {
				NSIndexPath indexPath =  table.IndexPathForSelectedRow;
				((ListViewController)segue.DestinationViewController).SetSource (Catalog.Instance[indexPath]);
			}
		}
	}
}
