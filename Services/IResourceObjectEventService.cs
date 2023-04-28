using Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
	public interface IResourceObjectEventService
	{
		public Task DeleteAsync(ResourceObject resourceObject, CancellationToken cancellationToken);
		public Task AddOrUpdateAsync(ResourceObject resourceObject, CancellationToken cancellationToken);
	}
}
