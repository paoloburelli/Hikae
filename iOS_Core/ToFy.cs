using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Json;
using System.Threading;

namespace Core
{
	public static class ToFy
	{
		public enum Status {BadSyntax=400,Unauthorized=401,NotFound=404,Conflict=409,PreconditionFailed=412,Locket=423,Ok=200,ConnectionFailed=666};
		private enum Method {GET,PUT,DELETE};

		private static string CLIENT_URL = "http://localhost:3000/api/v1/";//"http://tofy.herokuapp.com/api/v1/";

		private static void makeRequest(string resource, string password, Method method, Action<Response> callback){
			HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(CLIENT_URL+resource);

			if (password != null && password != "")
				myReq.Headers ["password"] = Utils.Base64Encode(password);

			myReq.Method = method.ToString();

			ThreadPool.QueueUserWorkItem (a => receive (myReq, password, callback));
		}

		private static void receive(HttpWebRequest request, string password, Action<Response> callback){
			Response r;
			try {
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				TextReader tr = new StreamReader (response.GetResponseStream ());
				r = Response.Parse (tr.ReadToEnd ());
				if (r.list != null && password != null)
					r.list.password = password;

				response.Close();
			} catch (WebException) {
				r = new Response(Status.ConnectionFailed,null);	
			}
				
			callback (r);

		}

		public static void AddList(string listName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName, password, Method.PUT,callback);
		}

		public static void GetList(string listName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName, password, Method.GET,callback);
		}

		public static void AddItem(string listName,string itemName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName+"/item/"+itemName, password, Method.PUT,callback);
		}

		public static void DeleteItem(string listName,string itemName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName+"/item/"+itemName, password, Method.DELETE,callback);
		}

		public static void CheckItem(string listName,string itemName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName+"/item/"+itemName+"/checkmark", password, Method.PUT,callback);
		}

		public static void UncheckItem(string listName,string itemName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName+"/item/"+itemName+"/checkmark", password, Method.DELETE,callback);
		}
	
		public class Response {
			public Status status { get; set; }
			public List list { get; set; }

			public static Response Parse(string jsonString) {
				JsonValue v = JsonObject.Parse (jsonString);

				try {
					return new Response (((int)v["status"]),List.Parse(v["data"]));
				} catch (KeyNotFoundException){
					return new Response (((int)v["status"]),null);
				}

			}

			internal Response (int status, List data){
				this.status = (Status)status;
				this.list = data;
			}

			internal Response (Status status, List data){
				this.status = status;
				this.list = data;
			}
		}

		public class List
		{
			public string name;
			public string password;
			public List<Item> items;

			public static List Parse(JsonValue value) {
				JsonArray a = (JsonArray)value ["list_items"];


				string password=null;
				try {
					password = value ["list_password"];
				} catch (Exception){
				}

				List<Item> l = new List<Item> ();
				foreach (JsonValue v in a) {
					l.Insert (0, new Item (v["name"].ToString ().Trim ('"'),(bool)v["checked"]));
				}

				return new List (value["list_name"].ToString().Trim('"'), l, password);
			}

			internal List (string name, List<Item> items, string password = null){
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

				List<JsonPrimitive> jitms = new List<JsonPrimitive>();
				foreach(Item i in items)
					jitms.Add(new JsonPrimitive(i.ToString()));

				jo.Add ("list_items", new JsonArray(jitms));
				return jo;
			}

			public bool synchronized{
				get {
					bool rVal = true;
					foreach (Item i in items)
						if (!i.synchronized) {
							rVal = false;
							break;
						}
					return rVal;
				}
			}
		}

		public class Item {
			public bool synchronized = true;
			public bool isChecked = false;
			public string name;
			public Item (string name, bool isChecked){
				this.name = name;
				this.isChecked = isChecked;
			}

			public Item (string name, bool isChecked, bool synchronized){
				this.name = name;
				this.synchronized = synchronized;
				this.isChecked = isChecked;

			}

			public override string ToString ()
			{
				return name;
			}
		}
	
	}
}

