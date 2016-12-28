namespace OneSky.Entity {
	/// <summary>
	/// https://github.com/onesky/api-documentation-platform/blob/master/resources/file.md#upload---upload-a-file
	/// </summary>
	public class ApiFileUploadInfo {
		public string Name {
			get;
			set;
		}

		public string Format {
			get;
			set;
		}

		public ApiLanguage Language {
			get;
			set;
		}

		public ApiImport Import {
			get;
			set;
		}
	}
}