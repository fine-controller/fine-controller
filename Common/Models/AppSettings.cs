using Common.Attributes;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Common.Models
{
	public class AppSettings
	{
		public string RootPath { get; set; }
		public bool IsProduction { get; set; }
		public string DataPath => Path.Combine(RootPath, "data");

		[Trim]
		[Required]
		[RegularExpression(@"^(?=.{1,253}$)(?:[a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])(?:\.(?:[a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9]))*$", ErrorMessage = "Invalid API host")]
		public string API_HOST { get; set; }

		[Required]
		[Range(1, 65535)]
		public int? API_PORT { get; set; }

		[Required]
		public bool? API_HTTPS { get; set; }

		[Trim]
		[Required]
		[RegularExpression(@"^(/?[^?#]*)(\?[^#]*)?(#.*)?$", ErrorMessage = "Invalid spec path")]
		public string API_SPEC_PATH { get; set; }
		
		[Required]
		public SpecFormat? API_SPEC_FORMAT { get; set; }

		[Trim]
		[Required]
		[RegularExpression(@"^(?=.{1,253}$)(?:[a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])(?:\.(?:[a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9]))*$", ErrorMessage = "Invalid API group name")]
		public string API_GROUP { get; set; }
	}
}
