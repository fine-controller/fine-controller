using Common.Models;
using k8s;
using Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Systems.KubernetesSystem;

namespace Application.EventHandlers
{
	internal class ResourceObjectEventEventHandler : IResourceObjectEventHandler
	{
		private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
		private readonly IResourceObjectEventEventService _resourceObjectEventEventService;

		public ResourceObjectEventEventHandler
		(
			IResourceObjectEventEventService resourceObjectEventEventService
		)
		{
			_resourceObjectEventEventService = resourceObjectEventEventService ?? throw new ArgumentNullException(nameof(resourceObjectEventEventService));
		}

		public async Task HandleAsync(ResourceObject resourceObject, CancellationToken cancellationToken)
		{
			// filter

			if (resourceObject is null)
			{
				throw new ArgumentNullException(nameof(resourceObject));
			}

			// semaphore to limit concurrency so versions are not messed up
			// named for each specific resource object so that concurrency is allowed for different resource objects

			var namedSemaphore = _semaphores.GetOrAdd(resourceObject.LongName, x => new(1));

			// process

			try
			{
				if (resourceObject.EventType == WatchEventType.Deleted)
				{
					await _resourceObjectEventEventService.DeleteAsync(resourceObject, cancellationToken);
				}
				else
				{
					await _resourceObjectEventEventService.AddOrUpdateAsync(resourceObject, cancellationToken);
				}
			}
			finally
			{
				// release semaphore

				namedSemaphore.Release();

				// clean up

				if (resourceObject.EventType == WatchEventType.Deleted)
				{
					_semaphores.Remove(resourceObject.LongName, out var _);
				}
			}
		}
	}
}
