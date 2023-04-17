using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Systems.BackgroundServiceSystem
{
	public interface IHostedServiceSystem
	{
		public bool Exists(string name);
		public IEnumerable<string> List();
		public Task RemoveAsync(string name);
		public Task AddAsync(string name, IHostedService hostedService, CancellationToken cancellationToken);
	}
}
