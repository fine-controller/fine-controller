using Common.Models;
using Common.Utils;
using Humanizer;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Systems.BackgroundServiceSystem;
using Systems.KubernetesSystem.HostedServices;
using Systems.KubernetesSystem.Models;

namespace Systems.KubernetesSystem.Impl
{
	internal class KubernetesSystemImpl : IKubernetesSystem
	{
		protected readonly ILogger _logger;
		protected readonly AppSettings _appSettings;
		protected readonly KubernetesClient _kubernetesClient;
		protected readonly IHostedServiceSystem _hostedServiceSystem;
		protected readonly AppCancellationToken _appCancellationToken;
		protected readonly IServiceProvider<ResourceObjectEventStreamer> _resourceObjectEventStreamerProvider;
		
		public KubernetesSystemImpl
		(
			AppSettings appSettings,
			KubernetesClient kubernetesClient,
			ILogger<KubernetesSystemImpl> logger,
			IHostedServiceSystem hostedServiceSystem,
			AppCancellationToken appCancellationToken,
			IServiceProvider<ResourceObjectEventStreamer> resourceObjectEventStreamerProvider
		)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			_kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));
			_hostedServiceSystem = hostedServiceSystem ?? throw new ArgumentNullException(nameof(hostedServiceSystem));
			_appCancellationToken = appCancellationToken ?? throw new ArgumentNullException(nameof(appCancellationToken));
			_resourceObjectEventStreamerProvider = resourceObjectEventStreamerProvider ?? throw new ArgumentNullException(nameof(resourceObjectEventStreamerProvider));

			// client

			if (_appSettings.IsProduction)
			{
				kubernetesClient.Client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
			}
		}

		private string GetResourceObjectEventStreamerName(string group, string version, string namePlural)
		{
			if (string.IsNullOrWhiteSpace(version))
			{
				throw new ArgumentNullException(nameof(version));
			}

			if (string.IsNullOrWhiteSpace(namePlural))
			{
				throw new ArgumentNullException(nameof(namePlural));
			}

			return $"{nameof(ResourceObjectEventStreamer)}:{NameUtil.GetKindLongName(group, version, namePlural)}";
		}

		public async Task StartStreamingResourceObjectEventsAsync(string group, string version, string namePlural, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(version))
			{
				throw new ArgumentNullException(nameof(version));
			}

			if (string.IsNullOrWhiteSpace(namePlural))
			{
				throw new ArgumentNullException(nameof(namePlural));
			}

			version = version.Trim();
			namePlural = namePlural.Trim();
			group = group?.Trim() ?? string.Empty;

			var resourceObjectEventStreamer = _resourceObjectEventStreamerProvider.GetRequiredService();
			var resourceObjectEventStreamerName = GetResourceObjectEventStreamerName(group, version, namePlural);
			
			resourceObjectEventStreamer.Group = group;
			resourceObjectEventStreamer.Version = version;
			resourceObjectEventStreamer.NamePlural = namePlural;

			await _hostedServiceSystem.AddAsync(resourceObjectEventStreamerName, resourceObjectEventStreamer, cancellationToken);
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

				if (current.FineControllerHash.Equals(customResourceDefinition.FineControllerHash, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				customResourceDefinition.Metadata.ResourceVersion = current.ResourceVersion();
				await _kubernetesClient.Client.ReplaceCustomResourceDefinitionAsync(customResourceDefinition, customResourceDefinition.Name(), cancellationToken: cancellationToken);
			}
		}

		public async Task<string> GetKnownKindForApiEndpointAsync(ApiEndpoint apiEndpoint, CancellationToken cancellationToken)
		{
			var group = apiEndpoint.GroupLowerCase?.Trim();

			if (string.IsNullOrWhiteSpace(group) || group == "-")
			{
				group = string.Empty;
			}

			// try origional

			try
			{
				var kindOrigional = apiEndpoint.KindLowerCase;
				await _kubernetesClient.Client.CustomObjects.ListClusterCustomObjectAsync(group, apiEndpoint.VersionLowerCase, kindOrigional, limit: 1, cancellationToken: cancellationToken);
				return kindOrigional;
			}
			catch (Exception exception)
			{
				if (!exception.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
				{
					throw;
				}
			}

			// try original singularized

			try
			{
				var kindSingularized = apiEndpoint.KindLowerCase.Singularize().ToLower();
				await _kubernetesClient.Client.CustomObjects.ListClusterCustomObjectAsync(group, apiEndpoint.VersionLowerCase, kindSingularized, limit: 1, cancellationToken: cancellationToken);
				return kindSingularized;
			}
			catch (Exception exception)
			{
				if (!exception.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
				{
					throw;
				}
			}

			// try original pluralized

			try
			{
				var kindPluralized = apiEndpoint.KindLowerCase.Pluralize().ToLower();
				await _kubernetesClient.Client.CustomObjects.ListClusterCustomObjectAsync(group, apiEndpoint.VersionLowerCase, kindPluralized, limit: 1, cancellationToken: cancellationToken);
				return kindPluralized;
			}
			catch (Exception exception)
			{
				if (!exception.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
				{
					throw;
				}
			}

			// try origional (without version)

			try
			{
				var kindOrigionalWithoutVersion = apiEndpoint.KindLowerCase[apiEndpoint.VersionLowerCase.Length..].ToLower();
				await _kubernetesClient.Client.CustomObjects.ListClusterCustomObjectAsync(group, apiEndpoint.VersionLowerCase, kindOrigionalWithoutVersion, limit: 1, cancellationToken: cancellationToken);
				return kindOrigionalWithoutVersion;
			}
			catch (Exception exception)
			{
				if (!exception.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
				{
					throw;
				}
			}

			// try original singularized (without version)

			try
			{
				var kindSingularizedWithoutVersion = apiEndpoint.KindLowerCase[apiEndpoint.VersionLowerCase.Length..].Singularize().ToLower();
				await _kubernetesClient.Client.CustomObjects.ListClusterCustomObjectAsync(group, apiEndpoint.VersionLowerCase, kindSingularizedWithoutVersion, limit: 1, cancellationToken: cancellationToken);
				return kindSingularizedWithoutVersion;
			}
			catch (Exception exception)
			{
				if (!exception.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
				{
					throw;
				}
			}

			// try original pluralized (without version)

			try
			{
				var kindPluralizedWithoutVersion = apiEndpoint.KindLowerCase[apiEndpoint.VersionLowerCase.Length..].Pluralize().ToLower();
				await _kubernetesClient.Client.CustomObjects.ListClusterCustomObjectAsync(group, apiEndpoint.VersionLowerCase, kindPluralizedWithoutVersion, limit: 1, cancellationToken: cancellationToken);
				return kindPluralizedWithoutVersion;
			}
			catch (Exception exception)
			{
				if (!exception.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
				{
					throw;
				}
			}

			// failed

			return default;
		}
	}
}
