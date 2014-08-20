using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using tofy;

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
			ToList.OnChange = delegate() {
				Catalog.Instance.Save ();
			};
		}

		public readonly List<ToList> Lists = new List<ToList>();
	}
}

