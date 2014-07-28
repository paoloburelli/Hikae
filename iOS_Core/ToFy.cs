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

		private static string CLIENT_URL = "http://tofy.herokuapp.com/api/v1/";

		private static void makeRequest(string resource, string password, Method method, Action<Response> callback){
			HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(CLIENT_URL+resource);

			if (password != null && password != "")
				myReq.Headers ["password"] = Utils.Base64Encode(password);

			myReq.Method = method.ToString();

			Thread t = new Thread (() => receive(myReq,password,callback));
			t.Start ();
		}

		private static void receive(HttpWebRequest request, string password, Action<Response> callback){
			Response r;
			try {
				HttpWebResponse response = (HttpWebResponse)request.GetResponse ();
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
			public string name { get; set; }
			public string password { get; set; }
			public List<string> items { get; set; }

			public static List Parse(JsonValue value) {

				JsonArray a = (JsonArray)value ["list_items"];
				List<string> l = new List<string> ();
				foreach (JsonValue v in a)
					l.Add (v.ToString().Trim('"'));

				return new List (value["list_name"].ToString().Trim('"'), l);
			}

			internal List (string name, List<string> items, string passwrod = null){
				this.password = password;
				this.name = name;
				this.items = items;
			}

			public override string ToString ()
			{
				return name;
			}
		}
	
	}
}

