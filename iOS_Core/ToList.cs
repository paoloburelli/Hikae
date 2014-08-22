using System;
using System.Collections.Generic;
using System.Linq;
using System.Json;
using System.IO;

namespace tofy
{
	public class ToList
	{
		public static string PASSWORD_CHANGED_FLAG="---<not valid anymore>---";
		public static Action<ToList,int> UpdateUI;
		public static Action Save;
		public string Name;
		public string Password{
			get {
				return _password;
			}
			set {
				_password = value;
				if (ToList.Save != null)
					ToList.Save();	
				if (UpdateUI != null)
					UpdateUI (this,-1);
			}
		}
		private string _password;

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
			i.Owner = this;
			Items.Add (i);
			_changes++;
			if (UpdateUI != null)
				UpdateUI (this,this.Count-1);
			if (Save != null)
				Save ();
		}

		public int Count {
			get {
				return Items.Count;
			}
		}

		public void RemoveAt(int index) {
			Items.RemoveAt (index);
			_changes++;
			if (UpdateUI != null)
				UpdateUI (this,index);
			if (Save != null)
				Save ();
		}

		public void Insert(int index,ToItem i) {
			i.Owner = this;
			Items.Insert (index,i);
			_changes++;
			if (UpdateUI != null)
				UpdateUI (this,index);
			if (Save != null)
				Save ();
		}

		public int IndexOf(ToItem it) {
			return Items.IndexOf (it);
		}

		private List<ToItem> Items;
		public List<string> Observers;

		private int _changes=0;
		public int Changes{
			get { 
				return _changes;
			}
		}
		public void ResetChanges() {
			_changes = 0;
			if (UpdateUI != null)
				UpdateUI (this,-1);
			if (Save != null)
				Save ();
		}

		public void Synch(ToList source) {
			_changes += Math.Abs (source.Count - this.Count);
			Items = source.Items;
			Observers = source.Observers;
			if (source.Password != null)
				_password = source.Password;
			if (UpdateUI != null)
				UpdateUI (this,-1);
			if (Save != null)
				Save ();
		}

		public static ToList Parse(
			JsonValue value) {

			string password=null;
			if (value.ContainsKey("password"))
				password = value ["password"];

			List<ToItem> tl = null;
			if (value.ContainsKey ("items")) {
				JsonArray a = (JsonArray)value ["items"];
				tl = new List<ToItem> ();
				foreach (JsonValue v in a) {
					tl.Insert (0, ToItem.Parse (v));
				}
			}

			List<string> obs = null;
			if (value.ContainsKey("observers")) {
				JsonArray a = (JsonArray)value ["observers"];
				obs = new List<string> ();
				foreach (JsonValue v in a) {
					obs.Insert (0, v.ToString());
				}
			}

			ToList rValue = new ToList (value ["name"].ToString ().Trim ('"'), password, tl, obs);

			if (value.ContainsKey("changes"))
				rValue._changes = value ["changes"];

			return rValue;
		}

		public ToList (string name, string password = null, List<ToItem> items=null, List<string> observers=null){
			this._password = password;
			this.Name = name;

			if (items == null)
				this.Items = new List<ToItem> ();
			else
				this.Items = items;
			if (observers == null)
				this.Observers = new List<string> ();
			else
				this.Observers = observers;

			foreach (ToItem it in Items)
				it.Owner = this;
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
			jo.Add ("changes", new JsonPrimitive (Changes));

			List<JsonValue> jitms = new List<JsonValue>();
			foreach(ToItem i in Items)
				jitms.Insert(0,i.ToJson());

			jo.Add ("items", new JsonArray(jitms));

			List<JsonValue> jobs = new List<JsonValue>();
			foreach(string o in Observers)
				jobs.Insert(0,new JsonPrimitive(o));

			jo.Add ("observers", new JsonArray(jobs));
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

}

