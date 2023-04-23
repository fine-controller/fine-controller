using Common.Models;
using Common.Utils;
using k8s;
using System;
using System.Threading;
using System.Threading.Tasks;
using Systems.KubernetesSystem;

namespace Services.Impl
{
	internal class WebApiResourceObjectServiceImpl : IWebApiResourceObjectService
    {
		private readonly AppData _appData;
		private readonly IKubernetesSystem _kubernetesSystem;
		
		public WebApiResourceObjectServiceImpl
        (
			AppData appData,
			IKubernetesSystem kubernetesSystem
		)
        {
			_appData = appData ?? throw new ArgumentNullException(nameof(appData));
			_kubernetesSystem = kubernetesSystem ?? throw new ArgumentNullException(nameof(kubernetesSystem));
		}

        public async Task AddOrUpdateAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken)
        {
            if (webApiResourceObject is null)
            {
                throw new ArgumentNullException(nameof(webApiResourceObject));
            }

            if (!webApiResourceObject.FineController)
            {
                throw new ArgumentException($"{Constants.FineController} is not true", nameof(webApiResourceObject));
            }

            if (!webApiResourceObject.FineControllerPort.HasValue)
            {
                throw new ArgumentException($"{Constants.FineControllerPort} is required", nameof(webApiResourceObject));
            }

            if (string.IsNullOrWhiteSpace(webApiResourceObject.FineControllerSpecPath))
            {
                throw new ArgumentException($"{Constants.FineControllerSpecFormat} is required", nameof(webApiResourceObject));
            }

            if (!webApiResourceObject.FineControllerSpecFormat.HasValue)
            {
                throw new ArgumentException($"{Constants.FineControllerSpecFormat} is required", nameof(webApiResourceObject));
            }

            _ = webApiResourceObject.FineControllerHttps; // just to ensure validation

            // data

            await _kubernetesSystem.SetWebApiCustomResourceObjectDataAsync(webApiResourceObject, cancellationToken);
            var existingDefinitions = await _kubernetesSystem.GetKubernetesCustomResourceDefinitionsAsync(webApiResourceObject, cancellationToken);

            // diff definitions with what's already in kubernetes

            var compareResult = CustomResourceDefinitionResourceObjectDiffUtil.GetDiff(existingDefinitions, webApiResourceObject.CustomResourceDefinitions);
			
			// update kubernetes

			await _kubernetesSystem.AddOrUpdateCustomResouceDefinitionsAsync(webApiResourceObject.CustomResourceDefinitions, cancellationToken);

            // update streams

			foreach (var definition in webApiResourceObject.CustomResourceDefinitions)
			{
				await _kubernetesSystem.StartStreamingResourceObjectEventsAsync(definition.Spec.Group, definition.Spec.Versions[0].Name, definition.Spec.Names.Plural, cancellationToken);
			}

			foreach (var definition in compareResult.Removed)
			{
				await _kubernetesSystem.StopStreamingResourceObjectEventsAsync(definition.Spec.Group, definition.Spec.Versions[0].Name, definition.Spec.Names.Plural, cancellationToken);
			}

            // add/update

			_appData.WebApiResourceObjects[webApiResourceObject.LongName] = webApiResourceObject;

            if (webApiResourceObject.EventType == WatchEventType.Added)
            {

            }
		}

        public async Task DeleteAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken)
        {
            if (webApiResourceObject is null)
            {
                throw new ArgumentNullException(nameof(webApiResourceObject));
            }

			var existingDefinitions = await _kubernetesSystem.GetKubernetesCustomResourceDefinitionsAsync(webApiResourceObject, cancellationToken);

            // stop streams

			foreach (var definition in existingDefinitions)
			{
				await _kubernetesSystem.StopStreamingResourceObjectEventsAsync(definition.Spec.Group, definition.Spec.Versions[0].Name, definition.Spec.Names.Plural, cancellationToken);
			}

            // remove

            _appData.WebApiResourceObjects.TryRemove(webApiResourceObject.LongName, out var _);
		}
    }
}
