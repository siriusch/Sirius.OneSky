OneSky API wrapper for .NET
===========================

Wrapper for simple access to all API functions of OneSky ("new API"), which takes care of correctly authenticateing and marshalling data between .NET POCO (with PascalCase or camelCase member naming convention) and the API calls.

See https://github.com/onesky/api-documentation-platform for available API.

Usage
-----

The wrapper is generic, so that any existing (and future) method of can be invoked. Therefore, the following list is not comprehensive as it represents just a few examples.

For all operations, an instance of `ApiClient` needs to be used; we assume an instance named `client` in the following examples. The instance holds the API URI and keys. SInce the instance wraps a `HttpClient`, the same re-use and thread-safety guidelines apply. In summary, use a single instance for the lifetime of your application, you may use it from multiple threads, and it should be disposed at the end.

### List Project Groups

```C#
var response = client.GetAll<ApiIdName>("project-groups").Result;
foreach (var pair in response) {
	Console.WriteLine($"{pair.Id}: {pair.Name}");
}
```

### List Projects in a Project Group

```C#
var response = client.GetAll<ApiIdName>($"project-groups/{groupId}/projects").Result;
foreach (var pair in response) {
	Console.WriteLine($"{pair.Id}: {pair.Name}");
}
```

### List uploaded Files in a Project

```C#
var response = client.GetAll<ApiFileInfo>($"projects/{projectId}/files").Result;
foreach (var pair in response) {
	Console.WriteLine($"{pair.FileName}: {pair.StringCount}");
}
```

### Get Translations

```C#
using (MemoryStream data = new MemoryStream()) {
	var response = client.GetFile($"projects/{projectId}/translations", new {
		SourceFileName = fileName,
		Locale = "de",
		FileFormat = "HIERARCHICAL_JSON"
	}, data).Result;
	data.Seek(0, SeekOrigin.Begin);
	using (TextReader textReader = new StreamReader(data, Encoding.GetEncoding(response.Headers.ContentType?.CharSet ?? "UTF-8"))) {
		Console.WriteLine(textReader.ReadToEnd());
	}
}
```

### Upload a File

```C#
var jsonContent = new StringContent(@"{ ""key"": ""Some text"" }");
jsonContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
	FileName = lf.RemoteFileName,
};
jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
var response = client.Post<ApiFileUploadInfo>(projectFilesUri, new {
	File = jsonContent,
	FileFormat = "HIERARCHICAL_JSON"
})).Result;
```

### Delete a File

```C#
var response = client.Delete<ApiName>(projectFilesUri, new {
	rf.FileName
}).Result;
```

License
-------
(C) 2016 Sirius Technologies AG

MIT license, see LICENSE.txt