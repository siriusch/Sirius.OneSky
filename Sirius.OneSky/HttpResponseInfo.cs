using System;
using System.Net;
using System.Net.Http.Headers;

namespace OneSky {
	/// <summary>
	/// HTTP Response Metadata object, which does not hold any disposable references
	/// </summary>
	public class HttpResponseInfo {
		/// <summary>
		/// Initialize the HTTP response information ojbect
		/// </summary>
		/// <param name="statusCode">The HTTP status</param>
		/// <param name="reasonPhrase">The HTTP status reason</param>
		/// <param name="headers">The HTTP response headers</param>
		public HttpResponseInfo(HttpStatusCode statusCode, string reasonPhrase, HttpContentHeaders headers) {
			Headers = headers;
			ReasonPhrase = reasonPhrase;
			StatusCode = statusCode;
		}

		/// <summary>
		/// The HTTP status
		/// </summary>
		public HttpStatusCode StatusCode {
			get;
		}

		/// <summary>
		/// The HTTP status reason
		/// </summary>
		public string ReasonPhrase {
			get;
		}

		/// <summary>
		/// The HTTP response headers
		/// </summary>
		public HttpContentHeaders Headers {
			get;
		}
	}
}
