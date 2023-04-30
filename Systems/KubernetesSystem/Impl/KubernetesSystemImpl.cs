using Common.Models;
using Humanizer;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Systems.BackgroundServiceSystem;
using Systems.KubernetesSystem.Models;

namespace Systems.KubernetesSystem.Impl
{
	internal class KubernetesSystemImpl : IKubernetesSystem
	{
		protected readonly ILogger _logger;
		protected readonly AppSettings _appSettings;
		protected readonly KubernetesClient _kubernetesClient;
		protected readonly IHostedServiceSystem _hostedServiceSystem;
		
		public KubernetesSystemImpl
		(
			AppSettings appSettings,
			KubernetesClient kubernetesClient,
			ILogger<KubernetesSystemImpl> logger,
			IHostedServiceSystem hostedServiceSystem
		)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			_kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));
			_hostedServiceSystem = hostedServiceSystem ?? throw new ArgumentNullException(nameof(hostedServiceSystem));

			// client

			if (_appSettings.IsProduction)
			{
				kubernetesClient.Client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
			}
		}

		public async Task AddOrUpdateCustomResouceDefinitionsAsync(IEnumerable<CustomResourceDefinitionResourceObject> customResourceDefinitions, CancellationToken cancellationToken)
		{
			if (customResourceDefinitions is null)
			{
				throw new ArgumentNullException(nameof(customResourceDefinitions));
			}

            foreach (var customResourceDefinition in customResourceDefinitions)
            {
				var current = (await _kubernetesClient.Client.ListCustomResourceDefinitionAsync(fieldSelector: $"metadata.name={customResourceDefinition.Name()}", cancellationToken: cancellationToken)).Items.Select(CustomResourceDefinitionResourceObject.Convert).SingleOrDefault();

				if (current is null)
				{
					await _kubernetesClient.Client.CreateCustomResourceDefinitionAsync(customResourceDefinition, cancellationToken: cancellationToken);
					continue;
				}

				customResourceDefinition.Metadata ??= new();
				customResourceDefinition.Metadata.ResourceVersion = current.ResourceVersion();
				
				await _kubernetesClient.Client.ReplaceCustomResourceDefinitionAsync(customResourceDefinition, customResourceDefinition.Name(), cancellationToken: cancellationToken);
			}
		}

		public async Task<string> GetKnownKindForApiEndpointAsync(ApiEndpoint apiEndpoint, CancellationToken cancellationToken)
		{
			if (apiEndpoint is null)
			{
				throw new ArgumentNullException(nameof(apiEndpoint));
			}

			var group = apiEndpoint.GroupLowerCase?.Trim();

			if (string.IsNullOrWhiteSpace(group) || group == "-")
			{
				group = string.Empty;
			}

			var kindWithoutVersion = apiEndpoint.KindLowerCase[apiEndpoint.VersionLowerCase.Length..];

			foreach (var kindVariation in new[]
			{
				apiEndpoint.KindLowerCase,
				kindWithoutVersion.Pluralize(),
				kindWithoutVersion.Singularize(),
				apiEndpoint.KindLowerCase.Pluralize(),
				apiEndpoint.KindLowerCase.Singularize(),
			})
			{
				try
				{
					await _kubernetesClient.Client.CustomObjects.ListClusterCustomObjectAsync(group, apiEndpoint.VersionLowerCase, kindVariation, limit: 1, cancellationToken: cancellationToken);
					return kindVariation;
				}
				catch (HttpOperationException exception)
				{
					if (exception.Response.StatusCode != HttpStatusCode.NotFound)
					{
						throw;
					}
				}
			}

			return default;
		}
	}
}
