using Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Systems.ApiSystem
{
	public interface IApiSystem
	{
		public string GetBaseUrl();
		public Task LoadSpecAsync(CancellationToken cancellationToken);
		public Task<bool> IsRunningAsync(CancellationToken cancellationToken);
		public Task DeleteAsync(ResourceObject resourceObject, CancellationToken cancellationToken);
		public Task AddOrUpdateAsync(ResourceObject resourceObject, CancellationToken cancellationToken);
	}
}
