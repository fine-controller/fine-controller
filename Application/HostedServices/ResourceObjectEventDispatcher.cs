using Common.Models;
using k8s;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Systems.ApiSystem;

namespace Application.HostedServices
{
	internal class ResourceObjectEventDispatcher : BackgroundService
	{
		private readonly AppData _appData;
		private readonly IApiSystem _apiSystem;

		public ResourceObjectEventDispatcher
		(
			AppData appData,
			IApiSystem apiSystem
		)
		{
			_appData = appData ?? throw new ArgumentNullException(nameof(appData));
			_apiSystem = apiSystem ?? throw new ArgumentNullException(nameof(apiSystem));
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				var resourceObject = await _appData.ResourceObjects.GetNextAsync(stoppingToken);

				if (resourceObject.EventType == WatchEventType.Deleted)
				{
					await _apiSystem.DeleteAsync(resourceObject, stoppingToken);
				}
				else
				{
					await _apiSystem.AddOrUpdateAsync(resourceObject, stoppingToken);
				}
			}
		}
	}
}
