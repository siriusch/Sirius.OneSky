using System;

namespace OneSky.Entity {
	/// <summary>
	/// https://github.com/onesky/api-documentation-platform/blob/master/resources/project_group.md#list---retrieve-all-project-groups
	/// </summary>
	public class ApiIdName: ApiName {
		public int Id {
			get;
			set;
		}
	}
}
