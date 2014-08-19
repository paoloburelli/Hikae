using System;
using System.Collections.Generic;
using System.Linq;
using System.Json;
using System.IO;

namespace tofy
{
	public class ToList
	{
		public string Name;
		public string Password;
		public List<ToItem> Items {
			get { return _items; 
			}
			set {
				Changes = Math.Abs (_items.Count - value.Count);
				_items = value;
			}
		}

		private List<ToItem> _items = new List<ToItem>();
		public int Changes=0;




		public static ToList Parse(
			JsonValue value) {
			JsonArray a = (JsonArray)value ["items"];


			string password=null;
			try {
				password = value ["password"];
			} catch (Exception){
			}

			List<ToItem> l = new List<ToItem> ();
			foreach (JsonValue v in a) {
				l.Insert (0, ToItem.Parse(v));
			}

			return new ToList (value["name"].ToString().Trim('"'), l, password);
		}

		public ToList (string name, List<ToItem> items, string password = null){
			this.Password = password;
			this.Name = name;
			this.Items = items;
		}

		public override string ToString ()
		{
			return Name;
		}

		public JsonObject ToJson ()
		{
			JsonObject jo = new JsonObject ();
			jo.Add ("name", new JsonPrimitive (Name));
			jo.Add ("password", new JsonPrimitive (Password));

			List<JsonValue> jitms = new List<JsonValue>();
			foreach(ToItem i in Items)
				jitms.Insert(0,i.ToJson());

			jo.Add ("items", new JsonArray(jitms));
			return jo;
		}

		public bool synchronized{
			get {
				bool rVal = true;
				foreach (ToItem i in Items)
					if (!i.Synchronized) {
						rVal = false;
						break;
					}
				return rVal;
			}
		}
	}

	public class ToItem {
		public bool Synchronized { 
			get; 
			set; 
		}
		public bool Checked;
		public string Name;
		public string LastAuthor;

		public ToItem (string name, bool isChecked=false, string author="Someone", bool synchronized=false){
			this.Name = name;
			this.Synchronized = synchronized;
			this.Checked = isChecked;
			this.LastAuthor = author;
		}

		public override string ToString ()
		{
			return Name;
		}

		public JsonValue ToJson(){
			JsonObject j = new JsonObject ();
			j ["name"] = Name;
			j ["checked"] = Checked;
			return j;
		}

		public static ToItem Parse(JsonValue jo) {
			string name = "";
			if (jo.ContainsKey ("name"))
				name = jo ["name"].ToString ().Trim ('"');

			bool isChecked = false;
			if (jo.ContainsKey("checked"))
				isChecked = (bool)jo ["checked"];

			string lastAuthor = "Someone";
			if (jo.ContainsKey("last_author"))
				lastAuthor = jo ["last_author"].ToString ().Trim ('"');


			return new ToItem (name, isChecked, lastAuthor, true);
		}
	}
}

