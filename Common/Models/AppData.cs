using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Common.Models
{
	public class AppData
	{
		public IEnumerable<WebApiEndpoint> ApiPaths { get; set; } = Array.Empty<WebApiEndpoint>();
		public IDictionary<string, ResourceObject> WatchedResourceObjectsCurrentVersions { get; } = new ConcurrentDictionary<string, ResourceObject>();
		public IEnumerable<CustomResourceDefinitionResourceObject> CustomResourceDefinitions { get; set; } = Array.Empty<CustomResourceDefinitionResourceObject>();
	}
}
