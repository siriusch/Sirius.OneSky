using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using OneSky.Entity;

namespace OneSky {
	/// <summary>
	/// OneSky API extension methods.
	/// </summary>
	public static class ApiExtensions {
		/// <summary>
		/// Check the response for success, and retrieve the result as the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the result</typeparam>
		/// <param name="response">The SPI response object</param>
		/// <returns>An instance of T, if the response was indicating a success.</returns>
		public static T GetData<T>(this ApiResponse response) {
			if (response.Meta.Status < 200 || response.Meta.Status > 299) {
				throw new InvalidOperationException(response.Meta.Message ?? $"Generic API error {response.Meta.Status}");
			}
			using (JsonReader reader = new JTokenReader(response.Data)) {
				return ApiClient.Serializer.Deserialize<T>(reader);
			}
		}

		/// <summary>
		/// Request a file from the API, storing the file contents in the given stream.
		/// </summary>
		/// <param name="client">The <see cref="ApiClient"/> instance.</param>
		/// <param name="uri">The relative URI after the API base uri without slash</param>
		/// <param name="args">The arguments POCO, may be null.</param>
		/// <param name="target">The target stream which shall receive the file data</param>
		/// <param name="checkStatusCode">Whether the HTTP result code shall be checked for success. Defaults to <c>true</c>.</param>
		/// <param name="token">The cancellation token, may be <see cref="CancellationToken.None"/></param>
		/// <returns>A <see cref="HttpResponseInfo"/> with the metadata of the request (e.g. HTTP status code and headers).</returns>
		public static Task<HttpResponseInfo> GetFile(this ApiClient client, string uri, object args, Stream target, bool checkStatusCode = true, CancellationToken token = default(CancellationToken)) {
			return client.SendRequest(HttpMethod.Get, uri, ApiClient.ArgsToDictionary(args), message => CopyResponseStream(message, target, checkStatusCode), token);
		}

		private static async Task<HttpResponseInfo> CopyResponseStream(HttpResponseMessage message, Stream target, bool checkStatusCode) {
			if (checkStatusCode && message.StatusCode != HttpStatusCode.OK) {
				throw new InvalidOperationException($"File could not be loaded, server returned {message.StatusCode} {message.ReasonPhrase}");
			}
			using (HttpContent content = message.Content) {
				using (Stream source = await content.ReadAsStreamAsync().ConfigureAwait(false)) {
					await source.CopyToAsync(target);
					return new HttpResponseInfo(message.StatusCode, message.ReasonPhrase, message.Content.Headers);
				}
			}
		}

		/// <summary>
		/// Execute a GET API request to retrieve information as JSON. For getting files, use the <see cref="GetFile"/> instead.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="client">The <see cref="ApiClient"/> instance.</param>
		/// <param name="uri">The relative URI after the API base uri without slash</param>
		/// <param name="args">The arguments POCO, may be null.</param>
		/// <returns>If the request was successful, an instance of <c>T</c></returns>
		public static async Task<T> Get<T>(this ApiClient client, string uri, object args = null) {
			return (await client.SendRequest(HttpMethod.Get, uri, args).ConfigureAwait(false)).GetData<T>();
		}

		/// <summary>
		/// Execute GET API request(s) to retrieve a list of items, automatically paging as needed.
		/// </summary>
		/// <typeparam name="T">The type of the list element entity to retrieve.</typeparam>
		/// <param name="client">The <see cref="ApiClient"/> instance.</param>
		/// <param name="uri">The relative URI after the API base uri without slash</param>
		/// <param name="args">The arguments POCO, may be null.</param>
		/// <returns>If the request was successful, a <see cref="IReadOnlyList{T}"/> holding all the entities</returns>
		public static async Task<IReadOnlyList<T>> GetAll<T>(this ApiClient client, string uri, object args = null) {
			List<T> results = new List<T>();
			var argsDict = ApiClient.ArgsToDictionary(args);
			int perPage = 100;
			if (argsDict.ContainsKey("per_page")) {
				perPage = Convert.ToInt32(argsDict["per_page"], CultureInfo.InvariantCulture);
			} else {
				argsDict["per_page"] = perPage;
			}
			for (int page = 1;; page++) {
				argsDict["page"] = page;
				var data = (await client.SendRequest(HttpMethod.Get, uri, args).ConfigureAwait(false)).GetData<T[]>();
				results.AddRange(data);
				if (data.Length < perPage) {
					break;
				}
			}
			return results;
		}

		/// <summary>
		/// Execute a POST API request.
		/// </summary>
		/// <typeparam name="T">The type of the entity returned.</typeparam>
		/// <param name="client">The <see cref="ApiClient"/> instance.</param>
		/// <param name="uri">The relative URI after the API base uri without slash</param>
		/// <param name="args">The arguments POCO, may be null.</param>
		/// <returns>If the request was successful, an instance of <c>T</c></returns>
		public static async Task<T> Post<T>(this ApiClient client, string uri, object args = null) {
			return (await client.SendRequest(HttpMethod.Post, uri, args).ConfigureAwait(false)).GetData<T>();
		}

		/// <summary>
		/// Execute a PUT API request.
		/// </summary>
		/// <typeparam name="T">The type of the entity returned.</typeparam>
		/// <param name="client">The <see cref="ApiClient"/> instance.</param>
		/// <param name="uri">The relative URI after the API base uri without slash</param>
		/// <param name="args">The arguments POCO, may be null.</param>
		/// <returns>If the request was successful, an instance of <c>T</c></returns>
		public static async Task<T> Put<T>(this ApiClient client, string uri, object args = null) {
			return (await client.SendRequest(HttpMethod.Put, uri, args).ConfigureAwait(false)).GetData<T>();
		}

		/// <summary>
		/// Execute a DELETE API request.
		/// </summary>
		/// <typeparam name="T">The type of the entity returned.</typeparam>
		/// <param name="client">The <see cref="ApiClient"/> instance.</param>
		/// <param name="uri">The relative URI after the API base uri without slash</param>
		/// <param name="args">The arguments POCO, may be null.</param>
		/// <returns>If the request was successful, an instance of <c>T</c></returns>
		public static async Task<T> Delete<T>(this ApiClient client, string uri, object args = null) {
			return (await client.SendRequest(HttpMethod.Delete, uri, args).ConfigureAwait(false)).GetData<T>();
		}
	}
}
