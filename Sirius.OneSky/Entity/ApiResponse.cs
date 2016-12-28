using System;

using Newtonsoft.Json.Linq;

namespace OneSky.Entity {
	/// <summary>
	/// Generic class for JSON API responses; the Data is untyped to account for different layout depending on success/failure status
	/// https://github.com/onesky/api-documentation-platform#response
	/// </summary>
	public class ApiResponse {
		/// <summary>
		/// The <c>meta</c> data from the API
		/// </summary>
		public ApiResponseMetadata Meta {
			get;
			set;
		}

		/// <summary>
		/// The <c>data</c> payload from the API
		/// </summary>
		public JToken Data {
			get;
			set;
		}
	}
}
