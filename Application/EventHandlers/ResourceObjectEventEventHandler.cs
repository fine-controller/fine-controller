using Common.Models;
using Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Systems.KubernetesSystem;

namespace Application.EventHandlers
{
	internal class ResourceObjectEventEventHandler : IResourceObjectEventHandler
	{
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

			// add

			await _resourceObjectEventEventService.AddOrUpdateAsync(resourceObject, cancellationToken);
		}
	}
}
