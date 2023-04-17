using Common.Interfaces;
using Common.Models;
using k8s.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Systems.HelmSystem;
using Systems.KubernetesSystem;

namespace Services.Impl
{
	internal class ControllerResourceObjectServiceImpl : IControllerResourceObjectService
	{
		private static readonly Dictionary<string, ControllerResourceObject> controllerResourceObjects = new();

		private readonly IHelmSystem _helmSystem;
		private readonly IKubernetesSystem _kubernetesSystem;
		private readonly ICustomResourceDefinitionComparer _customResourceDefinitionComparer;
		
		public ControllerResourceObjectServiceImpl
		(
			IHelmSystem helmSystem,
			IKubernetesSystem kubernetesSystem,
			ICustomResourceDefinitionComparer customResourceDefinitionComparer
		)
		{
			_helmSystem = helmSystem ?? throw new ArgumentNullException(nameof(helmSystem));
			_kubernetesSystem = kubernetesSystem ?? throw new ArgumentNullException(nameof(kubernetesSystem));
			_customResourceDefinitionComparer = customResourceDefinitionComparer ?? throw new ArgumentNullException(nameof(customResourceDefinitionComparer));
		}

		public async Task AddOrUpdateAsync(ControllerResourceObject controllerResourceObject, CancellationToken cancellationToken)
		{
			if (controllerResourceObject is null)
			{
				throw new ArgumentNullException(nameof(controllerResourceObject));
			}

			// start web api port forward

			await _kubernetesSystem.StartWebApiPortForwardAsync(controllerResourceObject, cancellationToken);

			// get definitions from web api and kubernetes

			var definitionsFromKubernetes = await _kubernetesSystem.GetKubernetesCustomResourceDefinitionsAsync(controllerResourceObject, cancellationToken);
			var definitionsFromControllerWebApi = await _kubernetesSystem.GetWebApiCustomResourceDefinitionsAsync(controllerResourceObject, cancellationToken);
			
			// diff definitions with what's already in kubernetes

			var compareResult = _customResourceDefinitionComparer.Compare(definitionsFromKubernetes, definitionsFromControllerWebApi);

			// apply changes to kubernetes

			var newAndUpdatedDefinitions = new List<V1CustomResourceDefinition>();
			newAndUpdatedDefinitions.AddRange(compareResult.New);
			newAndUpdatedDefinitions.AddRange(compareResult.Updated);
			await _helmSystem.ApplyAsync(newAndUpdatedDefinitions, cancellationToken);

			// delete definition and definition instances that don't exist anymore
			// TODO:maybe we don't want CRD deletions or make it opt-in

			await _kubernetesSystem.DeleteCustomResourceDefinitionsAsync(compareResult.Removed, cancellationToken);

			// watching the definitions

			// add to storage

			controllerResourceObjects[controllerResourceObject.LongName] = controllerResourceObject;
		}

		public async Task DeleteAsync(ControllerResourceObject controllerResourceObject, CancellationToken cancellationToken)
		{
			if (controllerResourceObject is null)
			{
				throw new ArgumentNullException(nameof(controllerResourceObject));
			}

			// stop web api port forward

			await _kubernetesSystem.StopWebApiPortForwardAsync(controllerResourceObject, cancellationToken);

			// delete definitions
			// TODO:maybe we don't CRD deletions or make it opt-in

			//await _kubernetesSystem.DeleteCustomResourceDefinitionsAsync(controllerResourceObject.Removed, cancellationToken);

			// remove to storage

			controllerResourceObjects.Remove(controllerResourceObject.LongName);

			// force async

			await Task.CompletedTask;
		}
	}
}
