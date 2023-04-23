using Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Systems.KubernetesSystem
{
	public interface IKubernetesSystem
	{
		public Task StartStreamingResourceObjectEventsAsync(string group, string version, string namePlural, CancellationToken cancellationToken);
		public Task AddOrUpdateCustomResouceDefinitionsAsync(IEnumerable<CustomResourceDefinitionResourceObject> customResourceDefinitions, CancellationToken cancellationToken);
	}
}