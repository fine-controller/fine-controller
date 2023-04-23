using Common.Models;
using Common.Utils;
using k8s;
using Services;
using System;
using System.Text.Json;
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

		public async Task HandleAsync(ResourceObject resourceObject, CancellationToken cancellationToken)
		{
			// filter

			if (resourceObject is null)
			{
				throw new ArgumentNullException(nameof(resourceObject));
			}

			if (!resourceObject.ApiVersion.Equals(Constants.V1CamelCase, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			if (!resourceObject.Kind.Equals(Constants.ServicePascalCase, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			var webApiResourceObject = resourceObject.Data.Deserialize<WebApiResourceObject>();

			if (!webApiResourceObject.FineController)
			{
				return;
			}

			// delete

			if (resourceObject.EventType == WatchEventType.Deleted)
			{
				await _webApiResourceObjectService.DeleteAsync(webApiResourceObject, cancellationToken);
				return;
			}

			// add/update

			await _webApiResourceObjectService.AddOrUpdateAsync(webApiResourceObject, cancellationToken);
		}
	}
}
