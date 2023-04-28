using Common.Models;
using k8s.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Systems.ApiSystem;

namespace Services.Impl
{
	internal class ResourceObjectEventServiceImpl : IResourceObjectEventService
	{
		private readonly AppData _appData;
		private readonly IApiSystem _apiSystem;
		
		public ResourceObjectEventServiceImpl
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

			// check if resourceObject 'resourceVersion' is newer than the last

			if (_appData.WatchedResourceObjectsCurrentResourceVersions.TryGetValue(resourceObject.LongName, out var currentResourceVersion))
			{
				return;
			}

			if (!string.IsNullOrWhiteSpace(currentResourceVersion) && !resourceObject.IsNewerThan(currentResourceVersion))
			{
				return;
			}

			// dispatch update

			await _apiSystem.AddOrUpdateAsync(resourceObject, cancellationToken);

			// incoming is now the latest

			_appData.WatchedResourceObjectsCurrentResourceVersions[resourceObject.LongName] = resourceObject.ResourceVersion();
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

			_appData.WatchedResourceObjectsCurrentResourceVersions.Remove(resourceObject.LongName);
		}
	}
}
