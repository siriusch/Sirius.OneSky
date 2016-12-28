using System.Text.RegularExpressions;

using Newtonsoft.Json.Serialization;

namespace OneSky {
	/// <summary>
	/// JSON.NET <see cref="NamingStrategy"/> which converts <c>PascalCase</c> to <c>underscore_case</c>
	/// </summary>
	public class UnderscoreNamingStrategy: NamingStrategy {
		private static readonly Regex rxUpper = new Regex(@"\p{Lu}+", RegexOptions.Compiled);

		/// <summary>
		/// Method to convert a <c>PascalCase</c> name to <c>underscore_case</c> API key
		/// </summary>
		/// <param name="name">The property name</param>
		/// <returns>The API key</returns>
		public static string ConvertCamelCaseToUnderscore(string name) {
			return rxUpper.Replace(name, (match) => match.Index == 0 ? match.Value.ToLowerInvariant() : $"_{match.Value.ToLowerInvariant()}");
		}

		/// <inheritdoc />
		protected override string ResolvePropertyName(string name) {
			return ConvertCamelCaseToUnderscore(name);
		}
	}
}
