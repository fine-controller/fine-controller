using Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Systems.ApiSystem;

namespace Services.Impl
{
	internal class ResourceObjectEventEventServiceImpl : IResourceObjectEventEventService
	{
		private readonly AppData _appData;
		private readonly IApiSystem _apiSystem;
		
		public ResourceObjectEventEventServiceImpl
		(
			AppData appData,
			IApiSystem apiSystem
		)
		{
			_appData = appData ?? throw new ArgumentNullException(nameof(appData));
			_apiSystem = apiSystem ?? throw new ArgumentNullException(nameof(apiSystem));
		}

		public async Task AddOrUpdateAsync(ResourceObject resourceObject, CancellationToken cancellationToken)
		{
			if (resourceObject is null)
			{
				throw new ArgumentNullException(nameof(resourceObject));
			}

			_appData.WatchedResourceObjectsCurrentVersions.TryGetValue(resourceObject.LongName, out var currentResourceObject);

			if (currentResourceObject is not null && !resourceObject.IsNewerThan(currentResourceObject))
			{
				return; // old news
			}

			// dispatch update

			await _apiSystem.AddOrUpdateAsync(resourceObject, cancellationToken);

			// incoming is now the latest

			_appData.WatchedResourceObjectsCurrentVersions[resourceObject.LongName] = resourceObject;
		}

		public async Task DeleteAsync(ResourceObject resourceObject, CancellationToken cancellationToken)
		{
			if (resourceObject is null)
			{
				throw new ArgumentNullException(nameof(resourceObject));
			}

			// dispatch delete

			await _apiSystem.DeleteAsync(resourceObject, cancellationToken);

			// clean up

			_appData.WatchedResourceObjectsCurrentVersions.Remove(resourceObject.LongName);
		}
	}
}
