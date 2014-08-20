using System;
using System.Collections.Generic;
using System.Linq;
using System.Json;
using System.IO;

namespace tofy
{
	public class ToList
	{
		public static Action OnChange;
		public string Name;
		public string Password;
		public ToItem this[int index] {
			get { 
				return Items[index]; 
			}
		}

		public ToItem this[string name] {
			get { 
				try{
					return Items.First (p => p.Name == name);
				}catch (InvalidOperationException){
					return null;
				}
			}
		}

		public void AddItem(ToItem i) {
			Items.Add (i);
			Changes++;
			if (OnChange != null)
				OnChange ();
		}

		public int Count {
			get {
				return Items.Count;
			}
		}

		public void RemoveAt(int index) {
			Items.RemoveAt (index);
			Changes++;
			if (OnChange != null)
				OnChange ();
		}

		public void Insert(int index,ToItem i) {
			Items.Insert (index,i);
			Changes++;
			if (OnChange != null)
				OnChange ();
		}

		public int IndexOf(ToItem it) {
			return Items.IndexOf (it);
		}

		private List<ToItem> Items = new List<ToItem>();
		public int Changes=0;

		public void Synch(ToList source) {
			Changes = Math.Abs (source.Count - this.Count);
			Items = source.Items;
			Password = source.Password;
			if (OnChange != null)
				OnChange ();
		}

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
		private bool _checked;
		public bool Checked {
			get{
				return _checked;
			}
			set {
				Synchronized = false;
				_checked = value;
				if (ToList.OnChange != null)
					ToList.OnChange ();
			}
		}

		public string Name{
			get {
				return _name;
			}
			set {
				Synchronized = false;
				_name = value;
				if (ToList.OnChange != null)
					ToList.OnChange ();			
			}
		}
		private string _name;

		private string _lastAuthor;
		public string LastAuthor{
			get {
				return _lastAuthor;
			}
			set {
				_lastAuthor = value;
				if (ToList.OnChange != null)
					ToList.OnChange ();			
			}
		}

		public ToItem (string name, bool isChecked=false, string author="Someone", bool synchronized=false){
			this._name = name;
			this.Synchronized = synchronized;
			this._checked = isChecked;
			this._lastAuthor = author;
		}

		public override string ToString ()
		{
			return Name;
		}

		public JsonValue ToJson(){
			JsonObject j = new JsonObject ();
			j ["name"] = Name;
			j ["checked"] = _checked;
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

