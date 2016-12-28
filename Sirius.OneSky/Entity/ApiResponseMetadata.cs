using System;

namespace OneSky.Entity {
	public class ApiResponseMetadata {
		public int Status {
			get;
			set;
		}

		public string Message {
			get;
			set;
		}

		public int? RecordCount {
			get;
			set;
		}

		public int? PageCount {
			get;
			set;
		}

		public Uri NextPage {
			get;
			set;
		}

		public Uri PrevPage {
			get;
			set;
		}

		public Uri FirstPage {
			get;
			set;
		}

		public Uri LastPage {
			get;
			set;
		}
	}
}
