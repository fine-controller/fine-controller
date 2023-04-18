using Common.Interfaces;
using Common.Models;
using Common.Utils;
using k8s.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Systems.KubernetesSystem;

namespace Services.Impl
{
	internal class WebApiResourceObjectServiceImpl : IWebApiResourceObjectService
	{
		private static readonly Dictionary<string, WebApiResourceObject> WebApiResourceObjects = new();

		private readonly IKubernetesSystem _kubernetesSystem;
		private readonly ICustomResourceDefinitionComparer _customResourceDefinitionComparer;
		
		public WebApiResourceObjectServiceImpl
		(
			IKubernetesSystem kubernetesSystem,
			ICustomResourceDefinitionComparer customResourceDefinitionComparer
		)
		{
			_kubernetesSystem = kubernetesSystem ?? throw new ArgumentNullException(nameof(kubernetesSystem));
			_customResourceDefinitionComparer = customResourceDefinitionComparer ?? throw new ArgumentNullException(nameof(customResourceDefinitionComparer));
		}

		public async Task AddOrUpdateAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken)
		{
			if (webApiResourceObject is null)
			{
				throw new ArgumentNullException(nameof(webApiResourceObject));
			}

			if (!webApiResourceObject.FineControllerEnable)
			{
				throw new ArgumentException($"{Constants.FineControllerPort} is not true", nameof(webApiResourceObject));
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

			// find existing

			WebApiResourceObjects.TryGetValue(webApiResourceObject.LongName, out var existingWebApiResourceObject);

			// get definitions from web api and kubernetes

			var incomingDefinitions = await _kubernetesSystem.GetWebApiCustomResourceDefinitionsAsync(webApiResourceObject, cancellationToken);
			var existingDefinitions = await _kubernetesSystem.GetKubernetesCustomResourceDefinitionsAsync(webApiResourceObject, cancellationToken);
			
			// diff definitions with what's already in kubernetes

			var compareResult = _customResourceDefinitionComparer.Compare(existingDefinitions, incomingDefinitions);

			// apply changes

			var newAndUpdatedDefinitions = new List<V1CustomResourceDefinition>();
			newAndUpdatedDefinitions.AddRange(compareResult.New);
			newAndUpdatedDefinitions.AddRange(compareResult.Updated);

			// - update kubernetes

			await _kubernetesSystem.AddOrUpdateCustomResouceDefinitionsAsync(newAndUpdatedDefinitions, cancellationToken);
			await _kubernetesSystem.DeleteCustomResourceDefinitionsAsync(compareResult.Removed, cancellationToken);

			// - update resource object

			foreach (var @new in compareResult.New)
			{
				webApiResourceObject.CustomResourceDefinitions.Add(@new);
			}

			foreach (var updated in compareResult.Updated)
			{
				var existingUpdated = webApiResourceObject.CustomResourceDefinitions.SingleOrDefault(x => x.GetLongName().Equals(updated.GetLongName()));

				if (existingUpdated is not null)
				{
					webApiResourceObject.CustomResourceDefinitions.Remove(existingUpdated);
					webApiResourceObject.CustomResourceDefinitions.Add(updated);
				}
			}

			foreach (var removed in compareResult.Removed)
			{
				var existingRemoved = webApiResourceObject.CustomResourceDefinitions.SingleOrDefault(x => x.GetLongName().Equals(removed.GetLongName()));

				if (existingRemoved is not null)
				{
					webApiResourceObject.CustomResourceDefinitions.Remove(existingRemoved);
				}
			}

			// watching the definitions

			// TODO:~

			// add to storage

			WebApiResourceObjects.Add(webApiResourceObject.LongName, webApiResourceObject);
		}

		public async Task DeleteAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken)
		{
			if (webApiResourceObject is null)
			{
				throw new ArgumentNullException(nameof(webApiResourceObject));
			}

			if (!WebApiResourceObjects.TryGetValue(webApiResourceObject.LongName, out var existingWebApiResourceObject))
			{
				return; // it does not exist, ignore
			}

			// delete definitions
			
			await _kubernetesSystem.DeleteCustomResourceDefinitionsAsync(existingWebApiResourceObject.CustomResourceDefinitions, cancellationToken);

			// add from storage

			WebApiResourceObjects.Remove(existingWebApiResourceObject.LongName);
		}
	}
}
