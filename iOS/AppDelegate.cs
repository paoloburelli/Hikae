using System;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using tofy;
using MonoTouch.AddressBook;

namespace Hikae
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to
	// application events from iOS.
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		// class-level declarations

		public override UIWindow Window {
			get;
			set;
		}

		//
		// This method is invoked when the application is about to move from active to inactive state.
		//
		// OpenGL applications should use this method to pause.
		//
		public override void OnResignActivation (UIApplication application)
		{
		}

		// This method should be used to release shared resources and it should store the application state.
		// If your application supports background exection this method is called instead of WillTerminate
		// when the user quits.
		public override void DidEnterBackground (UIApplication application)
		{
		}

		// This method is called as part of the transiton from background to active state.
		public override void WillEnterForeground (UIApplication application)
		{
			Catalog.Instance.Refresh ();
			application.ApplicationIconBadgeNumber = 0;
		}

		// This method is called when the application is about to terminate. Save data, if needed.
		public override void WillTerminate (UIApplication application)
		{
		}

		public override void FinishedLaunching (UIApplication application)
		{
			UIRemoteNotificationType notificationTypes = UIRemoteNotificationType.Alert | UIRemoteNotificationType.Badge | UIRemoteNotificationType.Sound;
			UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(notificationTypes);
			Catalog.Instance.Refresh ();
			application.ApplicationIconBadgeNumber = 0;
		}

		public override void RegisteredForRemoteNotifications (
			UIApplication application, NSData deviceToken)
		{
			Device.Token = deviceToken.ToString();
		}

		public override void DidReceiveRemoteNotification (UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
		{
			if (application.ApplicationState == UIApplicationState.Active) {

				string eventType = userInfo ["type"].ToString ();
				string listName = userInfo ["list_name"].ToString ();
				string itemName = "";
				if (userInfo.ContainsKey (new NSString ("item_name")))
					itemName = userInfo ["item_name"].ToString ();
				string author = "";
				if (userInfo.ContainsKey (new NSString ("author")))
					author = userInfo ["author"].ToString ();

				Catalog.Instance.HandleNotification (eventType, listName, itemName, author);
			}
		}
	}
}

