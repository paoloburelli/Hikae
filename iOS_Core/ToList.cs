using System;
using System.Collections.Generic;
using System.Linq;
using System.Json;
using System.IO;

namespace tofy
{
	public class ToList
	{
		public string name;
		public string password;
		public List<ToItem> items;


		public static List<ToList> Load(string filePath){
			List<ToList> rVal = new List<ToList>();
			try {
				using (TextReader reader = new StreamReader(filePath)) {
					string line = reader.ReadLine();
					while (line != null){
						rVal.Add(ToList.Parse(JsonValue.Parse(line)));
						line = reader.ReadLine();
					}
					reader.Close();
				}
			} catch (IOException) {
			}

			return rVal;
		}

		public static void Save(List<ToList> lists, string filePath){
			try {
				using (TextWriter writer = new StreamWriter(filePath)) {
					foreach (ToList l in lists)
						writer.WriteLine(l.ToJson().ToString());
					writer.Close();
				}
			} catch (IOException) {

			}
		}

		public static ToList Parse(
			JsonValue value) {
			JsonArray a = (JsonArray)value ["list_items"];


			string password=null;
			try {
				password = value ["list_password"];
			} catch (Exception){
			}

			List<ToItem> l = new List<ToItem> ();
			foreach (JsonValue v in a) {
				l.Insert (0, new ToItem (v["name"].ToString ().Trim ('"'),(bool)v["checked"],true));
			}

			return new ToList (value["list_name"].ToString().Trim('"'), l, password);
		}

		internal ToList (string name, List<ToItem> items, string password = null){
			this.password = password;
			this.name = name;
			this.items = items;
		}

		public override string ToString ()
		{
			return name;
		}

		public JsonObject ToJson ()
		{
			JsonObject jo = new JsonObject ();
			jo.Add ("list_name", new JsonPrimitive (name));
			jo.Add ("list_password", new JsonPrimitive (password));

			List<JsonObject> jitms = new List<JsonObject>();
			foreach(ToItem i in items)
				jitms.Insert(0,i.ToJson());

			jo.Add ("list_items", new JsonArray(jitms));
			return jo;
		}

		public bool synchronized{
			get {
				bool rVal = true;
				foreach (ToItem i in items)
					if (!i.synchronized) {
						rVal = false;
						break;
					}
				return rVal;
			}
		}
	}

	public class ToItem {
		public bool synchronized { 
			get; 
			set; 
		}
		public bool isChecked;
		public string name;

		public ToItem (string name, bool isChecked, bool synchronized){
			this.name = name;
			this.synchronized = synchronized;
			this.isChecked = isChecked;
		}

		public override string ToString ()
		{
			return name;
		}

		public JsonObject ToJson(){
			JsonObject j = new JsonObject ();
			j ["name"] = name;
			j ["checked"] = isChecked;
			return j;
		}
	}
}

