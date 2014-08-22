using System;
using System.Collections.Generic;
using System.Linq;
using System.Json;
using System.IO;

namespace tofy
{
	public class ToItem {
		internal ToList Owner;

		private bool _synchronized;
		public bool Synchronized { 
			get {
				return _synchronized;
			}
			set {
				_synchronized = value;
				if (ToList.UpdateUI != null)
					ToList.UpdateUI (Owner,Owner.IndexOf(this));
				if (ToList.Save != null)
					ToList.Save();	
			}
		}
		private bool _checked;
		public bool Checked {
			get{
				return _checked;
			}
			set {
				_synchronized = false;
				_checked = value;
				if (ToList.UpdateUI != null)
					ToList.UpdateUI (Owner,Owner.IndexOf(this));	
			}
		}

		public string Name{
			get {
				return _name;
			}
			set {
				_synchronized = false;	
				_name = value;
				if (ToList.UpdateUI != null)
					ToList.UpdateUI (Owner,Owner.IndexOf(this));	
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
				if (ToList.UpdateUI != null)
					ToList.UpdateUI (Owner,Owner.IndexOf(this));	
			}
		}

		public void Synch (ToItem item)
		{
			this._name = item.Name;
			this._checked = item.Checked;
			this._lastAuthor = item.LastAuthor;
			this.Synchronized = true;
		}

		public ToItem (string name, bool isChecked=false, string author="Someone", bool synchronized=false){
			this._name = name;
			this._synchronized = synchronized;
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
