using Common.Models;
using Common.Utils;
using k8s;
using Services;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Systems.KubernetesSystem;

namespace Application.EventHandlers
{
	internal class WebApiResourceObjectEventHandler : IResourceObjectEventHandler
	{
		private readonly IWebApiResourceObjectService _webApiResourceObjectService;

		public WebApiResourceObjectEventHandler
		(
			IWebApiResourceObjectService webApiResourceObjectService
		)
		{
			_webApiResourceObjectService = webApiResourceObjectService ?? throw new ArgumentNullException(nameof(webApiResourceObjectService));
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

			var webApiResourceObject = resourceObjectEvent.Deserialize<WebApiResourceObject>();

			if (!webApiResourceObject.FineControllerEnable)
			{
				return;
			}

			// delete

			if (resourceObjectEvent.GetResourceObjectSpecificEvent() == WatchEventType.Deleted)
			{
				await _webApiResourceObjectService.DeleteAsync(webApiResourceObject, cancellationToken);
				return;
			}

			// add/update

			await _webApiResourceObjectService.AddOrUpdateAsync(webApiResourceObject, cancellationToken);
		}
	}
}
