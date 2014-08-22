using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using tofy;
using System.Net;
using System.Linq;

namespace Hikae
{
	partial class Catalog 
	{
		public static readonly string FilePath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "data.json").ToString ();

		public void Load(){
			Lists.Clear ();
			try {
				string content = File.ReadAllText(FilePath);
				JsonArray ja = ((JsonArray)JsonObject.Parse(content)["data"]);

				foreach (JsonValue jv in ja)
					Lists.Add(ToList.Parse(jv));

			} catch (IOException) {
			}
		
			//Lists.Add(new ToList("cestil",new List<ToItem>()));
		}

		public void Save(){
			JsonObject jo = new JsonObject ();
			JsonArray data = new JsonArray ();
			foreach (ToList l in Lists)
				data.Add (l.ToJson ());
			jo ["data"] = data;

			try {
				using (TextWriter writer = new StreamWriter(FilePath)) {
					writer.WriteLine(jo.ToString());
					writer.Close();
				}
			} catch (IOException) {

			}
		}

		private static Catalog _instance; 
		public static Catalog Instance {
			get {
				if (_instance == null)
					_instance = new Catalog ();
				return _instance;
			}
		}
			
		public Catalog ()
		{
			Load ();
			ToList.Save += () => {
				Catalog.Instance.Save ();
			};
		}

		public readonly List<ToList> Lists = new List<ToList>();

		public void HandleNotification(string eventName, string listName, string itemName, string author) {
			switch (eventName) {
			case "ITEM_ADD":
				foreach (ToList l in Lists)
					if (l.Name == listName) {
						l.Insert (0, new ToItem (itemName,false,author,true));
						break;
					}
				break;
			case "ITEM_DELETE":
				foreach (ToList l in Lists)
					if (l.Name == listName) {
						l.RemoveAt (l.IndexOf (l [itemName]));
						break;
					}
				break;
			case "ITEM_CHANGE":
				foreach (ToList l in Lists)
					if (l.Name == listName) {
						Communication.GetItem(listName,itemName,l.Password,(Communication.Response response) => {
							if (response.Status == System.Net.HttpStatusCode.OK) {
								l[itemName].Synch(response.Item);
							}
						});
						 
						break;
					}
				break;
			case "REGISTER":
				Lists.Find(l => l.Name == listName).Observers.Add(author);
				break;
			case "UNREGISTER":
				Lists.Find(l => l.Name == listName).Observers.Remove(author);
				break;
			case "LIST_DELETE":
				ToList list = Lists.Find(l => l.Name == listName);

				Lists.Remove(list);
				if (ToList.UpdateUI != null)
					ToList.UpdateUI(list,-1);
				if (ToList.Save != null)
					ToList.Save();

				break;

			case "PW_CHANGE":
				ToList lst = Lists.Find (l => l.Name == listName);

				lst.Password = ToList.PASSWORD_CHANGED_FLAG;
				if (ToList.UpdateUI != null)
					ToList.UpdateUI(lst,-1);
				if (ToList.Save != null)
					ToList.Save();

				break;
		}
		}

		public void Refresh ()
		{
			foreach (ToList tl in Lists) {
				Communication.GetList(tl.Name,tl.Password,(Communication.Response response) => {
					InvokeOnMainThread ( () => {
						switch(response.Status) {
						case HttpStatusCode.OK:
							tl.Synch(response.List);
							break;
						case HttpStatusCode.NotFound: 
							break;
						case HttpStatusCode.Unauthorized:
							break;
						default: 
							break;
						}
					});
				});
			}
		}
	}
}

