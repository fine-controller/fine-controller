using k8s;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Systems.HelmSystem
{
	public interface IHelmSystem
	{
		public Task ApplyAsync<T>(IEnumerable<T> customResourceDefinitions, CancellationToken cancellationToken) where T : IKubernetesObject;
	}
}
