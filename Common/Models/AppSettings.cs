using Common.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Common.Models
{
	public class AppSettings
	{
		public string RootPath { get; set; }
		public bool IsProduction { get; set; }

		[Trim]
		[Required]
		[RegularExpression(@"^(?=.{1,253}$)(?:[a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])(?:\.(?:[a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9]))*$")]
		public string API_HOST { get; set; }

		[Required]
		[Range(1, 65535)]
		public int? API_PORT { get; set; }

		[Required]
		public bool? API_HTTPS { get; set; }

		[Trim]
		[Required]
		[RegularExpression(@"^(/?[^?#]*)(\?[^#]*)?(#.*)?$")]
		public string API_SPEC_PATH { get; set; }
		
		[Required]
		public SpecFormat? API_SPEC_FORMAT { get; set; }

		[Trim]
		[Required]
		[RegularExpression(@"^(?=.{1,253}$)(?:[a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])(?:\.(?:[a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9]))*$")]
		public string API_GROUP { get; set; }
	}
}
