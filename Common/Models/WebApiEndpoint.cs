using Microsoft.OpenApi.Models;

namespace Common.Models
{
	public class WebApiEndpoint
	{
		public string Path { get; set; }
		public string Kind { get; set; }
		public string Name { get; set; }
		public string Namespace { get; set; }
		public string[] PathArray { get; set; }
		public OperationType Method { get; set; }
		public OpenApiOperation Operation { get; set; }
	}
}
