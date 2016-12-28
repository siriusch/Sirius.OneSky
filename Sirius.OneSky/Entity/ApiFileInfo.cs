using System;

namespace OneSky.Entity {
	/// <summary>
	/// https://github.com/onesky/api-documentation-platform/blob/master/resources/file.md#list---list-uploaded-files
	/// </summary>
	public class ApiFileInfo {
		public string FileName {
			get;
			set;
		}

		public int StringCount {
			get;
			set;
		}

		public ApiLastImport LastImport {
			get;
			set;
		}

		public DateTime? UploadedAt {
			get;
			set;
		}

		public long UploadedAtTimestamp {
			get;
			set;
		}
	}
}
