using Common.Interfaces;
using Common.Models;
using Common.Utils;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Systems.BackgroundServiceSystem;
using Systems.KubernetesSystem.HostedServices;
using Systems.KubernetesSystem.Models;
using Constants = Common.Models.Constants;
using JsonObject = System.Text.Json.Nodes.JsonObject;

namespace Systems.KubernetesSystem.Impl
{
	internal class KubernetesSystemImpl : IKubernetesSystem
	{
		protected const string HELM_FOLDER = "./KubernetesSystem/Assets/Helm";
		protected const string KUBE_CTL_FOLDER = "./KubernetesSystem/Assets/KubeCtl";

		protected readonly ILogger _logger;
		protected readonly string _helmExecutableFile;
		protected readonly string _kubeCtlExecutableFile;
		protected readonly KubernetesClient _kubernetesClient;
		protected readonly IHostedServiceSystem _hostedServiceSystem;
		protected readonly AppCancellationToken _appCancellationToken;
		protected readonly IServiceProvider<ResourceObjectEventStreamer> _resourceObjectEventStreamerProvider;
		
		public KubernetesSystemImpl
		(
			KubernetesClient kubernetesClient,
			ILogger<KubernetesSystemImpl> logger,
			IHostedServiceSystem hostedServiceSystem,
			AppCancellationToken appCancellationToken,
			IServiceProvider<ResourceObjectEventStreamer> resourceObjectEventStreamerProvider
		)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));
			_hostedServiceSystem = hostedServiceSystem ?? throw new ArgumentNullException(nameof(hostedServiceSystem));
			_appCancellationToken = appCancellationToken ?? throw new ArgumentNullException(nameof(appCancellationToken));
			_resourceObjectEventStreamerProvider = resourceObjectEventStreamerProvider ?? throw new ArgumentNullException(nameof(resourceObjectEventStreamerProvider));

			// client

			//kubernetesClient.Client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());

			// Determine which executables to use
			
			_logger.LogInformation("Operating System : Windows {OSArchitecture}", RuntimeInformation.OSArchitecture);

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						_helmExecutableFile = $"{HELM_FOLDER}/windows-amd64";
						_kubeCtlExecutableFile = $"{KUBE_CTL_FOLDER}/windows-amd64";
						break;

					default:
						throw new ApplicationException("Not supported");
				}
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						_helmExecutableFile = $"{HELM_FOLDER}/darwin-amd64";
						_kubeCtlExecutableFile = $"{KUBE_CTL_FOLDER}/darwin-amd64";
						break;

					case Architecture.Arm64:
						_helmExecutableFile = $"{HELM_FOLDER}/darwin-arm64";
						_kubeCtlExecutableFile = $"{KUBE_CTL_FOLDER}/darwin-arm64";
						break;

					default:
						throw new ApplicationException("Not supported");
				}
			}
			else
			{
				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						_helmExecutableFile = $"{HELM_FOLDER}/linux-amd64";
						_kubeCtlExecutableFile = $"{KUBE_CTL_FOLDER}/linux-amd64";
						break;

					case Architecture.Arm64:
						_helmExecutableFile = $"{HELM_FOLDER}/linux-arm64";
						_kubeCtlExecutableFile = $"{KUBE_CTL_FOLDER}/linux-arm64";
						break;

					default:
						throw new ApplicationException("Not supported");
				}
			}

			// Chmod the executable for non-windows OS

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				ProcessUtil.ExecuteAsync("chmod", new[] { "+x", _helmExecutableFile }, _appCancellationToken.Token).GetAwaiter().GetResult();
				ProcessUtil.ExecuteAsync("chmod", new[] { "+x", _kubeCtlExecutableFile }, _appCancellationToken.Token).GetAwaiter().GetResult();
			}
		}

		private string GetResourceObjectEventStreamerName(string group, string version, string namePlural)
		{
			return $"{nameof(ResourceObjectEventStreamer)}:{NameUtil.GetResourceObjectKindLongName(group, version, namePlural)}";
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

		public async Task StopStreamingResourceObjectEventsAsync(string group, string version, string namePlural, CancellationToken cancellationToken)
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

			var streamName = GetResourceObjectEventStreamerName(group, version, namePlural);

			await _hostedServiceSystem.RemoveAsync(streamName);
		}

		public async Task<IEnumerable<V1CustomResourceDefinition>> GetKubernetesCustomResourceDefinitionsAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken)
		{
			var labelSelector = $"{Constants.FineControllerApiGroup}={webApiResourceObject.ApiGroup()}";
			var result = await _kubernetesClient.Client.ListCustomResourceDefinitionAsync(labelSelector: labelSelector, cancellationToken: cancellationToken);
			return result.Items.ToArray();
		}

		internal virtual async Task<string> GetWebApiUrlAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken)
		{
			// while the implementation is boring as had to justify needing it's own method,
			// the Kind one is interesting and sole motivation for this design choice

			var scheme = webApiResourceObject.FineControllerHttps ? "https" : "http";
			var url = $"{scheme}://{webApiResourceObject.Name()}:{webApiResourceObject.FineControllerPort}";

			// return

			return await Task.FromResult(url);
		}

		public async Task<IEnumerable<V1CustomResourceDefinition>> GetWebApiCustomResourceDefinitionsAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken)
		{
			if (webApiResourceObject is null)
			{
				throw new ArgumentNullException(nameof(webApiResourceObject));
			}

			// client

			webApiResourceObject.FineControllerWebApiUrl = await GetWebApiUrlAsync(webApiResourceObject, cancellationToken);
			var restClient = new RestClient(webApiResourceObject.FineControllerWebApiUrl);

			// metadata

			try
			{
				var metadataRequest = new RestRequest("metadata", Method.GET);
				var metadataResponse = restClient.Execute(metadataRequest);

				if (!metadataResponse.IsSuccessful)
				{
					throw new ApplicationException(metadataResponse.Content);
				}

				var metadata = metadataResponse.Deserialize<WebApiMetaData>();
				webApiResourceObject.FineControllerApiGroup = metadata.Group;
			}
			catch (Exception exception)
			{
				throw new ApplicationException("Failed to get metadata from web api", exception);
			}

			// spec

			var specJsonObject = default(JsonObject);

			try
			{
				var specRequest = new RestRequest(webApiResourceObject.FineControllerSpecPath, Method.GET);
				var specResponse = restClient.Execute(specRequest);

				if (!specResponse.IsSuccessful)
				{
					throw new ApplicationException(specResponse.Content);
				}

				switch (webApiResourceObject.FineControllerSpecFormat)
				{
					case SpecFormat.Json:
						specJsonObject = (JsonObject)JsonNode.Parse(specResponse.Content);
						break;

					case SpecFormat.Yaml:
						specJsonObject = (JsonObject)KubernetesYaml.LoadAllFromString($"[{specResponse.Content}]").Single();
						break;
				}
			}
			catch (Exception exception)
			{
				throw new ApplicationException("Failed to get spec from web api", exception);
			}

			// parsing definitions

			var definitions = new List<V1CustomResourceDefinition>();
			var schemasJsonObject = (JsonObject)specJsonObject?["components"]?["schemas"];

			foreach (var definitionJsonObject in schemasJsonObject.AsEnumerable().Select(x => x.Value.AsObject()))
			{
				var definition = new V1CustomResourceDefinition();
			}

			// return

			return definitions;
		}

		public async Task DeleteCustomResourceDefinitionsAsync(IEnumerable<V1CustomResourceDefinition> customResourceDefinitions, CancellationToken cancellationToken)
		{
			// TODO:maybe we don't want CRD deletions or make it opt-in

			var deleteOptions = customResourceDefinitions
				.Select(x => new
				{
					Kind = x.Kind,
					ApiVersion = x.ApiVersion,
					ApiVersionAndKind = $"{x.ApiVersion}|{x.Kind}"
				})
				.DistinctBy(x => x.ApiVersionAndKind)
				.Select(x => new V1DeleteOptions
				{
					Kind = x.Kind,
					OrphanDependents = false,
					ApiVersion = x.ApiVersion,
					PropagationPolicy = "Foreground"
				});

			foreach (var deleteOption in deleteOptions)
			{
				await _kubernetesClient.Client.DeleteCollectionCustomResourceDefinitionAsync(deleteOption, cancellationToken: cancellationToken);
			}
		}

		public Task AddOrUpdateCustomResouceDefinitionsAsync(List<V1CustomResourceDefinition> newAndUpdatedDefinitions, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
