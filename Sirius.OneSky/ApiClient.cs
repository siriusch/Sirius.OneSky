using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using OneSky.Entity;

namespace OneSky {
	/// <summary>
	/// Provides a base class for sending API requests to OneSky. 
	/// </summary>
	/// <threadsafety static="true" instance="true"/>
	public class ApiClient: IDisposable {
		public static readonly JsonSerializer Serializer = new JsonSerializer() {
			ContractResolver = new DefaultContractResolver() {
				NamingStrategy = new UnderscoreNamingStrategy() {
					ProcessDictionaryKeys = true,
					OverrideSpecifiedNames = true
				}
			},
			DateFormatHandling = DateFormatHandling.IsoDateFormat
		};

		/// <summary>
		/// Convert a POCO to an argument dictionary, typically used with anonymous objects. Property names are converted to underscore notation, e.g. <c>MyProperty</c> is converted to the key <c>my_property</c>.
		/// </summary>
		/// <param name="args">The object to convert to a dictionary. May be null.</param>
		/// <returns>A dictionary of key-object pairs.</returns>
		public static IDictionary<string, object> ArgsToDictionary(object args) {
			return (args as IDictionary<string, object>) ?? args?.GetType().GetProperties().ToDictionary(p => UnderscoreNamingStrategy.ConvertCamelCaseToUnderscore(p.Name), p => p.GetValue(args, null)) ?? new Dictionary<string, object>(0);
		}

		private readonly string apiPublic;
		private readonly string apiSecret;
		private readonly HttpClient httpClient;
		private readonly MD5 md5;
		private readonly Uri oneSkyApiUri;

		/// <summary>
		/// Initializes the <c>ApiClient</c> class for accessing the default API endpoint (<c>https://platform.api.onesky.io/1/</c>).
		/// </summary>
		/// <param name="apiPublic">The OneSky Public Key (Site Settings -> API Keys)</param>
		/// <param name="apiSecret">The OneSky Secret Key (Site Settings -> API Keys)</param>
		public ApiClient(string apiPublic, string apiSecret): this(new Uri("https://platform.api.onesky.io/1/"), apiPublic, apiSecret) {}

		/// <summary>
		/// Initializes the <c>ApiClient</c> class.
		/// </summary>
		/// <param name="oneSkyApiUri">The OneSky API endpoint.</param>
		/// <param name="apiPublic">The OneSky Public Key (Site Settings -> API Keys)</param>
		/// <param name="apiSecret">The OneSky Secret Key (Site Settings -> API Keys)</param>
		public ApiClient(Uri oneSkyApiUri, string apiPublic, string apiSecret) {
			if (oneSkyApiUri == null) {
				throw new ArgumentNullException(nameof(oneSkyApiUri));
			}
			if (string.IsNullOrEmpty(apiPublic)) {
				throw new ArgumentNullException(nameof(apiPublic));
			}
			if (string.IsNullOrEmpty(apiSecret)) {
				throw new ArgumentNullException(nameof(apiSecret));
			}
			this.oneSkyApiUri = oneSkyApiUri;
			this.apiPublic = apiPublic;
			this.apiSecret = apiSecret;
			httpClient = new HttpClient();
			httpClient.Timeout = new TimeSpan(0, 15, 0);
			md5 = new MD5CryptoServiceProvider();
		}

		public void Dispose() {
			httpClient.Dispose();
			md5.Dispose();
		}

		private async Task<ApiResponse> DeserializeToApiResponse(HttpResponseMessage response) {
			using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false)) {
				using (TextReader textReader = new StreamReader(stream, Encoding.UTF8)) {
					TextReader debugReader = null;
					if (Debugger.IsAttached) {
						string data = textReader.ReadToEnd();
						debugReader = new StringReader(data);
						Debug.WriteLine(data);
					}
					using (JsonReader jsonReader = new JsonTextReader(debugReader ?? textReader)) {
						return Serializer.Deserialize<ApiResponse>(jsonReader);
					}
				}
			}
		}

		/// <summary>
		/// Initiate an asynchronous API request via HTTP, with optional arguments and cancellation token.
		/// </summary>
		/// <param name="method">The HTTP method to use, as documented by OneSky API docs.</param>
		/// <param name="uri">The relative URI after the API base uri without slash</param>
		/// <param name="args">The arguments POCO, may be null.</param>
		/// <param name="token">The cancellation token, may be <see cref="CancellationToken.None"/></param>
		/// <returns>A <see cref="Task"/> for retrieving the <see cref="ApiResponse"/>.</returns>
		public Task<ApiResponse> SendRequest(HttpMethod method, string uri, object args = null, CancellationToken token = default(CancellationToken)) {
			return SendRequest(method, uri, ArgsToDictionary(args), DeserializeToApiResponse, token);
		}

		/// <summary>
		/// Initiate an asynchronous API request via HTTP, with arguments, a result message processor and an optional cancellation token.
		/// </summary>
		/// <typeparam name="T">The type returned by the result message processor <cref>processResult</cref></typeparam>
		/// <param name="method">The HTTP method to use, as documented by OneSky API docs.</param>
		/// <param name="uri">The relative URI after the API base uri without slash</param>
		/// <param name="argDict">The arguments dictionary, may not be null.</param>
		/// <param name="processResult">A callback for processing the <see cref="HttpResponseMessage"/> into the result of type <c>T</c>.</param>
		/// <param name="token">The cancellation token, may be <see cref="CancellationToken.None"/></param>
		/// <returns>A <see cref="Task"/> for retrieving the result.</returns>
		public async Task<T> SendRequest<T>(HttpMethod method, string uri, IDictionary<string, object> argDict, Func<HttpResponseMessage, Task<T>> processResult, CancellationToken token = default(CancellationToken)) {
			if (argDict == null) {
				throw new ArgumentNullException(nameof(argDict));
			}
			string timestamp = ((long)(DateTime.UtcNow-new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString(CultureInfo.InvariantCulture);
			StringBuilder query = new StringBuilder(uri);
			query.Append(uri.IndexOf('?') >= 0 ? '&' : '?');
			query.Append("api_key=");
			query.Append(Uri.EscapeDataString(apiPublic));
			query.Append("&timestamp=");
			query.Append(timestamp);
			query.Append("&dev_hash=");
			foreach (byte b in md5.ComputeHash(Encoding.ASCII.GetBytes(timestamp+apiSecret))) {
				query.Append(b.ToString("x2", CultureInfo.InvariantCulture));
			}
			var simpleRequest = (method == HttpMethod.Delete) || (method == HttpMethod.Get);
			if (simpleRequest) {
				foreach (var arg in argDict) {
					if (arg.Value is HttpContent) {
						throw new ArgumentException("Simple requests cannot have multipart content");
					}
					query.Append('&');
					query.Append(Uri.EscapeDataString(arg.Key));
					query.Append('=');
					query.Append(Uri.EscapeDataString(Convert.ToString(arg.Value, CultureInfo.InvariantCulture)));
				}
				argDict.Clear();
			}
			var requestUri = new Uri(oneSkyApiUri, query.ToString());
			if (Debugger.IsAttached) {
				Debug.WriteLine(requestUri);
			}
			HttpRequestMessage message = new HttpRequestMessage(method, requestUri);
			if (!simpleRequest) {
				if (argDict.Values.OfType<HttpContent>().Any()) {
					// multipart-formdata needed
					MultipartFormDataContent content = new MultipartFormDataContent();
					foreach (var arg in argDict) {
						HttpContent argContent = arg.Value as HttpContent;
						if (argContent != null) {
							if (argContent.Headers.ContentDisposition != null) {
								argContent.Headers.ContentDisposition.Name = arg.Key;
							} else {
								argContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
									Name = arg.Key
								};
							}
							content.Add(argContent);
						} else {
							content.Add(new StringContent(Convert.ToString(arg.Value, CultureInfo.InvariantCulture)), arg.Key);
						}
					}
					message.Content = content;
				} else {
					// send as JSON key-value dictionary
					MemoryStream stream = new MemoryStream();
					using (TextWriter writer = new StreamWriter(stream, Encoding.UTF8, 1024, true)) {
						Serializer.Serialize(writer, argDict);
					}
					stream.Seek(0, SeekOrigin.Begin);
					var content = new StreamContent(stream);
					content.Headers.ContentLength = stream.Length;
					content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
					message.Content = content;
				}
			}
			using (HttpResponseMessage response = await httpClient.SendAsync(message, token).ConfigureAwait(false)) {
				return await processResult(response).ConfigureAwait(false);
			}
		}
	}
}
