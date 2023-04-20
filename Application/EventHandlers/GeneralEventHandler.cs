using Services;
using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Systems.KubernetesSystem;

namespace Application.EventHandlers
{
	internal class GeneralEventHandler : IResourceObjectEventHandler
	{
		private readonly IWebApiResourceObjectService _webApiResourceObjectService;

		public GeneralEventHandler
		(
			IWebApiResourceObjectService webApiResourceObjectService
		)
		{
			_webApiResourceObjectService = webApiResourceObjectService ?? throw new ArgumentNullException(nameof(webApiResourceObjectService));
		}

		public async Task HandleAsync(JsonObject resourceObjectEvent, CancellationToken cancellationToken)
		{
			await Task.CompletedTask;
		}
	}
}
