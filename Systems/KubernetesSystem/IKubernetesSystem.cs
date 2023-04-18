using Common.Models;
using k8s.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Systems.KubernetesSystem
{
	public interface IKubernetesSystem
	{
		public Task StopStreamingResourceObjectEventsAsync(string group, string version, string namePlural, CancellationToken cancellationToken);
		public Task StartStreamingResourceObjectEventsAsync(string group, string version, string namePlural, CancellationToken cancellationToken);
		public Task AddOrUpdateCustomResouceDefinitionsAsync(List<V1CustomResourceDefinition> newAndUpdatedDefinitions, CancellationToken cancellationToken);
		public Task DeleteCustomResourceDefinitionsAsync(IEnumerable<V1CustomResourceDefinition> customResourceDefinitions, CancellationToken cancellationToken);
		public Task<IEnumerable<V1CustomResourceDefinition>> GetWebApiCustomResourceDefinitionsAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken);
		public Task<IEnumerable<V1CustomResourceDefinition>> GetKubernetesCustomResourceDefinitionsAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken);
	}
}