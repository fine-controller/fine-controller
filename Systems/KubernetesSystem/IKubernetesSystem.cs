using Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Systems.KubernetesSystem
{
	public interface IKubernetesSystem
	{
		public Task StopStreamingResourceObjectEventsAsync(string group, string version, string namePlural, CancellationToken cancellationToken);
		public Task StartStreamingResourceObjectEventsAsync(string group, string version, string namePlural, CancellationToken cancellationToken);
		public Task AddOrUpdateCustomResouceDefinitionsAsync(IEnumerable<CustomResourceDefinitionResourceObject> customResourceDefinitions, CancellationToken cancellationToken);
		public Task SetWebApiCustomResourceObjectDataAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken);
		public Task<IEnumerable<CustomResourceDefinitionResourceObject>> GetKubernetesCustomResourceDefinitionsAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken);
	}
}