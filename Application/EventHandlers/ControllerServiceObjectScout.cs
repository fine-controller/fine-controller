using Common.Interfaces;
using Common.Models;
using Common.Utils;
using k8s;
using k8s.Models;
using Services;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Application.EventHandlers
{
	internal class ControllerServiceObjectScout : IResourceObjectEventHandler
	{
		private readonly IControllerResourceObjectService _controllerResourceObjectService;

		public ControllerServiceObjectScout
		(
			IControllerResourceObjectService controllerResourceObjectService
		)
		{
			_controllerResourceObjectService = controllerResourceObjectService ?? throw new ArgumentNullException(nameof(controllerResourceObjectService));
		}

		public async Task HandleAsync(JsonObject resourceObjectEvent, CancellationToken cancellationToken)
		{
			// filter

			if (resourceObjectEvent is null)
			{
				throw new ArgumentNullException(nameof(resourceObjectEvent));
			}

			if (!resourceObjectEvent.GetResourceObjectApiVersion().Equals(Constants.V1CamelCase, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			if (!resourceObjectEvent.GetResourceObjectKind().Equals(Constants.ServicePascalCase, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			var controllerResourceObject = resourceObjectEvent.Deserialize<ControllerResourceObject>();

			if (controllerResourceObject.GetLabel(Constants.FineControllerDashCase) != "true")
			{
				return;
			}

			// process

			switch (resourceObjectEvent.GetResourceObjectSpecificEvent())
			{
				case WatchEventType.Deleted:
					await _controllerResourceObjectService.DeleteAsync(controllerResourceObject, cancellationToken);
					break;

				default:
					await _controllerResourceObjectService.AddOrUpdateAsync(controllerResourceObject, cancellationToken);
					break;
			}
			

			await Task.CompletedTask;
		}
	}
}
