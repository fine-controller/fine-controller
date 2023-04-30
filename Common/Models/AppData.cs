using Common.Utils;
using System;
using System.Collections.Generic;

namespace Common.Models
{
	public class AppData
	{
		public ResourceObjectQueue ResourceObjects { get; } = new();
		public IEnumerable<ApiEndpoint> KnownKindApiEndpoints { get; set; } = Array.Empty<ApiEndpoint>();
		public IEnumerable<CustomResourceDefinitionResourceObject> CustomResourceDefinitions { get; set; } = Array.Empty<CustomResourceDefinitionResourceObject>();
	}
}
