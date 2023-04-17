using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Systems.BackgroundServiceSystem.Impl
{
	internal class HostedServiceSystemImpl : IHostedServiceSystem
	{
		private readonly Dictionary<string, (IHostedService HostedService, CancellationTokenSource CancellationTokenSource, CancellationTokenSource CompositeCancellationTokenSource)> _items = new();

		public IEnumerable<string> List()
		{
			return _items.Keys;
		}

		public bool Exists(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentNullException(nameof(name));
			}

			return _items.ContainsKey(name);
		}

		public async Task AddAsync(string name, IHostedService hostedService, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentNullException(nameof(name));
			}

			if (hostedService is null)
			{
				throw new ArgumentNullException(nameof(hostedService));
			}

			if (Exists(name))
			{
				throw new ApplicationException($"'{name}' already exists");
			}

			var cancellationTokenSource = new CancellationTokenSource();
			var compositeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, cancellationToken);
			
			_items.Add(name, (hostedService, cancellationTokenSource, compositeCancellationTokenSource));

			await hostedService.StartAsync(compositeCancellationTokenSource.Token);
		}

		public async Task RemoveAsync(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentNullException(nameof(name));
			}

			if (!_items.TryGetValue(name, out var item))
			{
				throw new ApplicationException($"'{name}' does not exist");
			}

			item.CancellationTokenSource.Cancel();

			await item.HostedService.StopAsync(item.CompositeCancellationTokenSource.Token);

			_items.Remove(name);
		}
	}
}
