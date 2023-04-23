using Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Impl
{
	internal class ResourceObjectEventEventServiceImpl : IResourceObjectEventEventService
	{
		private readonly AppData _appData;
		
		public ResourceObjectEventEventServiceImpl
		(
			AppData appData
		)
		{
			_appData = appData ?? throw new ArgumentNullException(nameof(appData));
		}

		public async Task AddOrUpdateAsync(ResourceObject resourceObject, CancellationToken cancellationToken)
		{
			if (resourceObject is null)
			{
				throw new ArgumentNullException(nameof(resourceObject));
			}

			_appData.WatchedResourceObjectsCurrentVersions.TryGetValue(resourceObject.LongName, out var currentResourceObject);

			if (currentResourceObject is null || resourceObject.IsNewerThan(currentResourceObject))
			{
				_appData.WatchedResourceObjectsCurrentVersions[resourceObject.LongName] = resourceObject;
			}

			await Task.CompletedTask;
		}
	}
}
