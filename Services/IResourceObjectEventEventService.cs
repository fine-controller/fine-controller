using Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
	public interface IResourceObjectEventEventService
	{
		public Task AddOrUpdateAsync(ResourceObject resourceObjectEvent, CancellationToken cancellationToken);
	}
}
