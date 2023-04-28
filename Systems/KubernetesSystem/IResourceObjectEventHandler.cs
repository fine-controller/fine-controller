using Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Systems.KubernetesSystem
{
	public interface IResourceObjectEventHandler
    {
        public Task HandleAsync(ResourceObject resourceObject, CancellationToken cancellationToken);
    }
}
