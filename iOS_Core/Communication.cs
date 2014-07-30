using System;
using System.Net;
using System.Threading;
using System.IO;
using System.Json;
using System.Collections.Generic;

namespace tofy
{
	public static class Communication
	{
		public enum Status {BadSyntax=400,Unauthorized=401,NotFound=404,Conflict=409,PreconditionFailed=412,Locket=423,Ok=200,ConnectionFailed=666};
		public enum Method {GET,PUT,DELETE};
		public const string CLIENT_URL = "http://tofy-test.herokuapp.com/api/v1/";
		public const float WAIT_TIME = 10;

		public class Response {
			public Status status { get; set; }
			public ToList list { get; set; }

			public static Response Parse(string jsonString) {
				JsonValue v = JsonObject.Parse (jsonString);

				try {
					return new Response (((int)v["status"]),ToList.Parse(v["data"]));
				} catch (KeyNotFoundException){
					return new Response (((int)v["status"]),null);
				}

			}

			internal Response (int status, ToList data){
				this.status = (Status)status;
				this.list = data;
			}

			internal Response (Status status, ToList data){
				this.status = status;
				this.list = data;
			}
		}

		private static void makeRequest(string resource, string password, Method method, bool repeat, Action<Response> callback, string extraHeaderKey=null, string extraHeaderValue=null){
			ThreadPool.QueueUserWorkItem (a => receive (resource, password, method, repeat, callback, extraHeaderKey,extraHeaderValue));
		}

		private static void receive(string resource, string password, Method method, bool repeat, Action<Response> callback,string extraHeaderKey=null, string extraHeaderValue=null){
			Response r;
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(CLIENT_URL+resource);
			if (password != null && password != "")
				request.Headers ["password"] = Utils.Base64Encode(password);

			if (extraHeaderKey != null && extraHeaderValue != null)
				request.Headers [extraHeaderKey] = Utils.Base64Encode(extraHeaderValue);
				
			request.Method = method.ToString();

			try {
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				TextReader tr = new StreamReader (response.GetResponseStream ());
				r = Response.Parse (tr.ReadToEnd ());
				if (r.list != null && password != null)
					r.list.password = password;

				response.Close();
			} catch (WebException) {
				r = new Response(Status.ConnectionFailed,null);	
				request.Abort ();
			}

			if (r.status == Status.ConnectionFailed && repeat) {
				Thread.Sleep ((int)(WAIT_TIME * 1000));
				receive (resource, password, method, repeat, callback);
			} else
				callback (r);
		}

		public static void AddList(string listName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName, password, Method.PUT, false, callback);
		}

		public static void GetList(string listName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName, password, Method.GET, false, callback);
		}
			
		public static void DeleteList(string listName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName, password, Method.DELETE, false, callback);
		}

		public static void ChangePassword(string listName, string password, string newPassword, Action<Response> callback) {
			makeRequest ("list/" + listName + "/password", password, Method.PUT, true, callback, "newpassword", newPassword);
		}

		public static void AddItem(string listName,string itemName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName+"/item/"+itemName, password, Method.PUT, true, callback);
		}

		public static void DeleteItem(string listName,string itemName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName+"/item/"+itemName, password, Method.DELETE, true, callback);
		}

		public static void DeleteAllItems(string listName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName+"/allitems", password, Method.DELETE, false, callback);
		}

		public static void CheckItem(string listName,string itemName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName+"/item/"+itemName+"/checkmark", password, Method.PUT , true, callback);
		}

		public static void UncheckItem(string listName,string itemName, string password, Action<Response> callback) {
			makeRequest ("list/" + listName+"/item/"+itemName+"/checkmark", password, Method.DELETE , true, callback);
		}
	}
}

