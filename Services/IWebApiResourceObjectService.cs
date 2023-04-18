using Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
	public interface IWebApiResourceObjectService
	{
		public Task AddOrUpdateAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken);
		public Task DeleteAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken);
	}
}
