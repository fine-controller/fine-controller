using Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
	public interface IControllerResourceObjectService
	{
		public Task AddOrUpdateAsync(ControllerResourceObject controllerResourceObject, CancellationToken cancellationToken);
		public Task DeleteAsync(ControllerResourceObject controllerResourceObject, CancellationToken cancellationToken);
	}
}
