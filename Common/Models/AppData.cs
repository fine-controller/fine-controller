using System.Collections.Concurrent;

namespace Common.Models
{
	public class AppData
	{
		public ConcurrentDictionary<string, WebApiResourceObject> WebApiResourceObjects { get; } = new();
		public ConcurrentDictionary<string, ResourceObject> WatchedResourceObjectsCurrentVersions { get; } = new();
	}
}
