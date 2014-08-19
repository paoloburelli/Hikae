using System;
using System.Net;
using System.Threading;
using System.IO;
using System.Json;
using System.Collections.Generic;
using System.Text;

namespace tofy
{
	public static class Communication
	{
		public enum Method {GET,PUT,DELETE,POST,PATCH};
		public const string CLIENT_URL = "https://tofy-test.herokuapp.com/api/v1/";
		//public const string CLIENT_URL = "http://localhost:3000/api/v1/";
		public const float WAIT_TIME = 10;

		public class Response {
			public readonly HttpStatusCode Status;
			public readonly ToList List;
			public readonly ToItem Item;

			public Response (HttpStatusCode status, string content) {
				this.Status = status;
				if (status == HttpStatusCode.OK) {
					content = content.Trim('\\');
					JsonValue jv = JsonObject.Parse(content);
					if (jv.ContainsKey("checked"))
						Item = ToItem.Parse(jv);
					else 
						List = ToList.Parse(jv);
				}
			}

			public Response (HttpStatusCode status, ToList data){
				this.Status = status;
				this.List = data;
			}

			public Response (HttpStatusCode status, ToItem data){
				this.Status = status;
				this.Item = data;
			}

			public Response(HttpStatusCode status){
				this.Status = status;
			}

			private static Response _ServiceUnavailable = new Response(HttpStatusCode.ServiceUnavailable);
			public static Response ServiceUnavailable{
				get {
					return _ServiceUnavailable;
				}
			}
		}

		private static void makeRequest(string resource, string password, Method method, bool repeat, Action<Response> callback, string body="{}"){
			ThreadPool.QueueUserWorkItem (a => receive (resource, password, method, repeat, callback,body));
		}

		private static void receive(string resource, string password, Method method, bool repeat, Action<Response> callback, string body="{}"){
			Response response;
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(CLIENT_URL+resource);
			if (password != null && password != "")
				request.Headers ["Authorization"] = Utils.Base64Encode(":"+password);

			request.Headers ["Device-Id"] = Device.Token;

			request.Headers ["Author"] = Device.Name;
	
			request.Method = method.ToString();

			if (method == Method.POST || method == Method.PATCH || method == Method.PUT) {
				byte[] requestBytes = new ASCIIEncoding ().GetBytes (body);
				request.ContentType = "application/json";
				request.ContentLength = requestBytes.Length;
				Stream requestStream = request.GetRequestStream ();
				requestStream.Write (requestBytes, 0, requestBytes.Length);
			}

			try {
				HttpWebResponse r = (HttpWebResponse)request.GetResponse();
				TextReader tr = new StreamReader (r.GetResponseStream ());
				string content = tr.ReadToEnd ();

				response = new Response(r.StatusCode,content);
		
				r.Close();
			} catch (WebException e) {
				if (e.Response == null)
					response = new Response (HttpStatusCode.ServiceUnavailable);
				else
					response = new Response(((HttpWebResponse)e.Response).StatusCode);	
				request.Abort ();
			}

			if (response.Status == HttpStatusCode.ServiceUnavailable && repeat) {
				Thread.Sleep ((int)(WAIT_TIME * 1000));
				receive (resource, password, method, repeat, callback,body);
			} else
				callback (response);
		}

		public static void AddList(string listName, string password, Action<Response> callback) {
			JsonObject jo = new JsonObject ();
			jo ["name"] = listName;
			makeRequest ("lists/", password, Method.POST, false, callback,jo.ToString());
		}

		public static void GetList(string listName, string password, Action<Response> callback) {
			makeRequest ("lists/" + listName, password, Method.GET, false, callback);
		}
			
		public static void DeleteList(string listName, string password, Action<Response> callback) {
			makeRequest ("lists/" + listName, password, Method.DELETE, false, callback);
		}

		public static void ChangePassword(string listName, string password, string newPassword, Action<Response> callback) {
			JsonObject jo = new JsonObject ();
			jo ["password"] = newPassword;
			makeRequest ("lists/" + listName, password, Method.PATCH, true, callback,jo.ToString());
		}

		public static void AddItem(string listName,string itemName, string password, Action<Response> callback) {
			JsonObject jo = new JsonObject ();
			jo ["name"] = itemName;
			makeRequest ("lists/" + listName+"/items/", password, Method.PUT, true, callback,jo.ToString());
		}

		public static void DeleteItem(string listName,string itemName, string password, Action<Response> callback) {
			makeRequest ("lists/" + listName+"/items/"+itemName, password, Method.DELETE, true, callback);
		}

		public static void DeleteAllItems(string listName, string password, Action<Response> callback) {
			makeRequest ("lists/" + listName+"/allitems", password, Method.DELETE, false, callback);
		}

		public static void CheckItem(string listName,string itemName, string password, Action<Response> callback, bool check=true) {
			JsonObject jo = new JsonObject ();
			jo ["checked"] = check;
			makeRequest ("lists/" + listName+"/items/"+itemName, password, Method.PATCH , true, callback,jo.ToString());
		}
	}
}

