using System;
using MonoTouch.UIKit;

namespace tofy
{
	public class Device
	{
		public static string Token="";

		public static string Name {
			get {
				string n = UIDevice.CurrentDevice.Name;
				if (n.Contains ("'"))
					n = n.Split ('\'') [0];
				return n;
			}
		}
	}
}

