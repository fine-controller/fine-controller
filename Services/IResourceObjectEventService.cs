using Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
	public interface IResourceObjectEventService
	{
		public Task AddOrUpdateAsync(ResourceObject resourceObjectEvent, CancellationToken cancellationToken);
		public Task DeleteAsync(ResourceObject resourceObject, CancellationToken cancellationToken);
	}
}
