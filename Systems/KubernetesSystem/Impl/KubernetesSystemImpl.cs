using Common.Models;
using Common.Utils;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Systems.BackgroundServiceSystem;
using Systems.KubernetesSystem.HostedServices;
using Systems.KubernetesSystem.Models;

namespace Systems.KubernetesSystem.Impl
{
	internal class KubernetesSystemImpl : IKubernetesSystem
	{
		protected const string HELM_FOLDER = "./KubernetesSystem/Assets/Helm";
		protected const string KUBE_CTL_FOLDER = "./KubernetesSystem/Assets/KubeCtl";
		protected static readonly string[] K8S_MODEL_NAMES = typeof(V1ClusterRole).Assembly.GetTypes().Select(x => x.Name).ToArray();
		protected static readonly string[] OPEN_API_MODEL_NAMES = typeof(OpenApiSchema).Assembly.GetTypes().Select(x => x.Name).ToArray();
		protected static readonly JsonSerializerSettings JSON_SERIALIZER_SETTINGS = new() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None };

		protected readonly ILogger _logger;
		protected readonly AppSettings _appSettings;
		protected readonly string _helmExecutableFile;
		protected readonly string _kubeCtlExecutableFile;
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
			if (string.IsNullOrWhiteSpace(version))
			{
				throw new ArgumentNullException(nameof(version));
			}

			if (string.IsNullOrWhiteSpace(namePlural))
			{
				throw new ArgumentNullException(nameof(namePlural));
			}

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

		public async Task<IEnumerable<CustomResourceDefinitionResourceObject>> GetKubernetesCustomResourceDefinitionsAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken)
		{
			if (webApiResourceObject is null)
			{
				throw new ArgumentNullException(nameof(webApiResourceObject));
			}

			if (string.IsNullOrWhiteSpace(webApiResourceObject.FineControllerGroup))
			{
				throw new ArgumentException("FineControllerGroup is required", nameof(webApiResourceObject));
			}

			var labelSelector = $"{Constants.FineControllerGroup}={webApiResourceObject.FineControllerGroup}";
			var result = await _kubernetesClient.Client.ListCustomResourceDefinitionAsync(labelSelector: labelSelector, cancellationToken: cancellationToken);
			var kind = result.Kind[0 .. result.Kind.LastIndexOf("List")];
			return result.Items.Select(CustomResourceDefinitionResourceObject.Convert).ToArray();
		}

		internal virtual async Task<string> GetWebApiUrlAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken)
		{
			if (webApiResourceObject is null)
			{
				throw new ArgumentNullException(nameof(webApiResourceObject));
			}

			// while the implementation is boring as had to justify needing it's own method,
			// the Kind one is interesting and sole motivation for this design choice

			var scheme = webApiResourceObject.FineControllerHttps ? "https" : "http";
			var url = $"{scheme}://{webApiResourceObject.Name()}:{webApiResourceObject.FineControllerPort}";

			// return

			return await Task.FromResult(url);
		}

		public async Task SetWebApiCustomResourceObjectDataAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken)
		{
			if (webApiResourceObject is null)
			{
				throw new ArgumentNullException(nameof(webApiResourceObject));
			}

			// client

			webApiResourceObject.FineControllerWebApiUrl = await GetWebApiUrlAsync(webApiResourceObject, cancellationToken);

			var restClient = new RestClient(webApiResourceObject.FineControllerWebApiUrl);

			// group

			try
			{
				var metadataRequest = new RestRequest("info/group", Method.GET);
				var metadataResponse = restClient.Execute(metadataRequest);

				if (!metadataResponse.IsSuccessful)
				{
					throw new ApplicationException(metadataResponse.Content);
				}

				webApiResourceObject.FineControllerGroup = metadataResponse.Content.Trim('\\', '"');
			}
			catch (Exception exception)
			{
				throw new ApplicationException("Failed to get metadata from web api", exception);
			}

			// spec

			var openApiDocument = default(OpenApiDocument);

			try
			{
				var specRequest = new RestRequest(webApiResourceObject.FineControllerSpecPath, Method.GET);
				var specResponse = restClient.Execute(specRequest);

				if (!specResponse.IsSuccessful)
				{
					throw new ApplicationException(specResponse.Content);
				}

				var openApiDiagnostic = default(OpenApiDiagnostic);
				var openApiStringReader = new OpenApiStringReader();

				switch (webApiResourceObject.FineControllerSpecFormat)
				{
					case SpecFormat.Json:
						openApiDocument = openApiStringReader.Read(specResponse.Content, out openApiDiagnostic);
						break;

					case SpecFormat.Yaml:
						openApiDocument = openApiStringReader.Read(specResponse.Content, out openApiDiagnostic);
						break;
				}

				if (openApiDiagnostic.SpecificationVersion != Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0)
				{
					throw new ApplicationException("Specification is not version 3");
				}

				foreach (var openApiWarning in openApiDiagnostic.Warnings)
				{
					_logger.LogWarning("{Message} : {Pointer}", openApiWarning.Message, openApiWarning.Pointer);
				}

				foreach (var openApiError in openApiDiagnostic.Errors)
				{
					_logger.LogError("{Message} : {Pointer}", openApiError.Message, openApiError.Pointer);
				}

				if (openApiDiagnostic.Errors.Any())
				{
					throw new ApplicationException("Specification has errors");
				}
			}
			catch (Exception exception)
			{
				throw new ApplicationException("Failed to get spec from web api", exception);
			}

			// definitions

			var definitions = new List<CustomResourceDefinitionResourceObject>();

			var apiSchemas = openApiDocument.Components.Schemas
				.Where(x => !K8S_MODEL_NAMES.Contains(x.Key, StringComparer.OrdinalIgnoreCase))
				.Where(x => !OPEN_API_MODEL_NAMES.Contains(x.Key, StringComparer.OrdinalIgnoreCase))
				.ToList();

			webApiResourceObject.ApiPaths = openApiDocument.Paths
				.SelectMany(x => x.Value.Operations.Select(o =>
				{
					try
					{
						return new WebApiEndpoint(x.Key, x.Value, o.Key, o.Value, webApiResourceObject.FineControllerGroup);
					}
					catch (Exception exception)
					{
						_logger.LogInformation("Path '{Path}' skipped because '{Message}'", x.Key, exception.Message);
						return null;
					}
				}))
				.Where(x => x is not null)
				.Where(x => x.OperationType == OperationType.Put || x.OperationType == OperationType.Delete)
				.ToList();

			foreach (var apiSchemaKeyAndValue in apiSchemas)
			{
				var putEndpoint = webApiResourceObject.ApiPaths.SingleOrDefault(x => x.OperationType == OperationType.Put && x.KindLowerCase.Equals(apiSchemaKeyAndValue.Key, StringComparison.OrdinalIgnoreCase));
				var deleteEndpoint = webApiResourceObject.ApiPaths.SingleOrDefault(x => x.OperationType == OperationType.Delete && x.KindLowerCase.Equals(apiSchemaKeyAndValue.Key, StringComparison.OrdinalIgnoreCase));

				if (putEndpoint is null)
				{
					continue;
				}

				if (deleteEndpoint is null)
				{
					continue;
				}

				if (!string.IsNullOrWhiteSpace(putEndpoint.NamespaceLowerCase) && !putEndpoint.NamespaceLowerCase.Equals(deleteEndpoint.NamespaceLowerCase, StringComparison.OrdinalIgnoreCase))
				{
					throw new ApplicationException($"PUT and DELETE endpoints for schema '{apiSchemaKeyAndValue.Key}' must have the same value for the namespace segment index 3");
				}

				var kind = apiSchemaKeyAndValue.Key;
				var definitionVersion = putEndpoint.VersionLowerCase;
				var definition = new CustomResourceDefinitionResourceObject();
				var apiSchemaJson = JsonConvert.SerializeObject(apiSchemaKeyAndValue.Value, JSON_SERIALIZER_SETTINGS);

				definition.Kind = Constants.CustomResourceDefinitionPascalCase;
				definition.ApiVersion = Constants.ApiExtensionsK8sIoV1LowerCase;

				definition.EnsureMetadata();
				definition.Metadata.EnsureLabels();
				definition.Metadata.EnsureAnnotations();
				definition.Metadata.Labels[Constants.FineController] = true.ToString().ToLower();
				definition.Metadata.Name = $"{kind.ToLower()}.{webApiResourceObject.FineControllerGroup}";
				definition.Metadata.Annotations[Constants.FineControllerHash] = HashUtil.Hash(apiSchemaJson);
				definition.Metadata.Labels[Constants.FineControllerGroup] = webApiResourceObject.FineControllerGroup;

				definition.Spec ??= new();

				definition.Spec.Group = webApiResourceObject.FineControllerGroup;
				definition.Spec.Scope = string.IsNullOrWhiteSpace(putEndpoint.NamespaceLowerCase) ? Constants.ClusterPascalCase : Constants.NamespacedPascalCase;
				
				definition.Spec.Names ??= new();
				definition.Spec.Names.Kind = kind;
				definition.Spec.Names.Plural = kind.ToLower();
				definition.Spec.Names.Singular = kind.ToLower();
				
				definition.Spec.Versions ??= new List<V1CustomResourceDefinitionVersion>();
				definition.Spec.Versions.Add(new() { Name = definitionVersion, Schema = new() { OpenAPIV3Schema = new() }, Served = true, Storage = true, });
				definition.Spec.Versions[0].Schema.OpenAPIV3Schema = OpenApiSchemaUtil.Convert(apiSchemaKeyAndValue.Value);

				definition.Validate();
				definitions.Add(definition);
			}

			webApiResourceObject.CustomResourceDefinitions = definitions;
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
	}
}
