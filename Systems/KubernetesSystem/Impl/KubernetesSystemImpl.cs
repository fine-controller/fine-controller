using Common.Interfaces;
using Common.Models;
using Common.Utils;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Systems.BackgroundServiceSystem;
using Systems.KubernetesSystem.HostedServices;
using Systems.KubernetesSystem.Models;
using Constants = Common.Models.Constants;

namespace Systems.KubernetesSystem.Impl
{
	internal class KubernetesSystemImpl : IKubernetesSystem
	{
		private const string KUBECTL_EXECUTABLE_FOLDER = "./KubernetesSystem/KubeCtl";

		private readonly ILogger _logger;
		private readonly string _kubeCtlExecutableFile;
		private readonly KubernetesClient _kubernetesClient;
		private readonly IHostedServiceSystem _hostedServiceSystem;
		private readonly IServiceProvider<ServicePortForwarder> _servicePortForwarderProvider;
		private readonly IServiceProvider<ResourceObjectEventStreamer> _resourceObjectEventStreamerProvider;
		
		public KubernetesSystemImpl
		(
			KubernetesClient kubernetesClient,
			ILogger<KubernetesSystemImpl> logger,
			IHostedServiceSystem hostedServiceSystem,
			AppCancellationToken appCancellationToken,
			IServiceProvider<ServicePortForwarder> servicePortForwarderProvider,
			IServiceProvider<ResourceObjectEventStreamer> resourceObjectEventStreamerProvider
		)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_ = appCancellationToken ?? throw new ArgumentNullException(nameof(appCancellationToken));
			_kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));
			_hostedServiceSystem = hostedServiceSystem ?? throw new ArgumentNullException(nameof(hostedServiceSystem));
			_servicePortForwarderProvider = servicePortForwarderProvider ?? throw new ArgumentNullException(nameof(servicePortForwarderProvider));
			_resourceObjectEventStreamerProvider = resourceObjectEventStreamerProvider ?? throw new ArgumentNullException(nameof(resourceObjectEventStreamerProvider));

			// Determine which kubectl executable to use

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				_logger.LogInformation("Operating System : Windows {OSArchitecture}", RuntimeInformation.OSArchitecture);

				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						_kubeCtlExecutableFile = $"{KUBECTL_EXECUTABLE_FOLDER}/windows-amd64";
						break;

					default:
						throw new ApplicationException("Not supported");
				}
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				_logger.LogInformation("Operating System : OSX {OSArchitecture}", RuntimeInformation.OSArchitecture);

				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						_kubeCtlExecutableFile = $"{KUBECTL_EXECUTABLE_FOLDER}/darwin-amd64";
						break;

					case Architecture.Arm64:
						_kubeCtlExecutableFile = $"{KUBECTL_EXECUTABLE_FOLDER}/darwin-arm64";
						break;

					default:
						throw new ApplicationException("Not supported");
				}
			}
			else
			{
				_logger.LogInformation("Operating System : Linux {OSArchitecture}", RuntimeInformation.OSArchitecture);

				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						_kubeCtlExecutableFile = $"{KUBECTL_EXECUTABLE_FOLDER}/linux-amd64";
						break;

					case Architecture.Arm64:
						_kubeCtlExecutableFile = $"{KUBECTL_EXECUTABLE_FOLDER}/linux-arm64";
						break;

					default:
						throw new ApplicationException("Not supported");
				}
			}

			// Chmod the executable for non-windows OS

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				_logger.LogInformation("Chmoding {Executable}", _kubeCtlExecutableFile);
				ProcessUtil.ExecuteAsync("chmod", $"+x {_kubeCtlExecutableFile}", appCancellationToken.Token).GetAwaiter().GetResult();
			}

			kubernetesClient.Client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
		}

		public async Task<IEnumerable<V1CustomResourceDefinition>> GetKubernetesCustomResourceDefinitionsAsync(ControllerResourceObject controllerResourceObject, CancellationToken cancellationToken)
		{
			var labelSelector = $"{Constants.FineControllerDashCase}={controllerResourceObject.ApiGroup()}";
			var result = await _kubernetesClient.Client.ListCustomResourceDefinitionAsync(labelSelector: labelSelector, cancellationToken: cancellationToken);
			return result.Items.ToArray();
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

		public string GetServicePortForwarderName(ControllerResourceObject controllerResourceObject)
		{
			return $"{nameof(ServicePortForwarder)}:{controllerResourceObject.LongName}";
		}

		public async Task StartWebApiPortForwardAsync(ControllerResourceObject controllerResourceObject, CancellationToken cancellationToken)
		{
			if (controllerResourceObject is null)
			{
				throw new ArgumentNullException(nameof(controllerResourceObject));
			}

			var servicePortForwarderName = GetServicePortForwarderName(controllerResourceObject);
			var ports = controllerResourceObject.Spec?.Ports?.Where(x => x.Protocol?.Equals("TCP", StringComparison.OrdinalIgnoreCase) == true)?.ToList() ?? new();

			foreach (var port in ports)
			{
				var servicePortForwarder = _servicePortForwarderProvider.GetRequiredService();

				servicePortForwarder.LocalPort = 8888;//TODO
				servicePortForwarder.ServicePort = port.Port;
				servicePortForwarder.ServiceName = controllerResourceObject.Name();
				servicePortForwarder.KubeCtlExecutableFile = _kubeCtlExecutableFile;
				servicePortForwarder.Namespace = controllerResourceObject.Namespace();
				
				await _hostedServiceSystem.AddAsync(servicePortForwarderName, servicePortForwarder, cancellationToken);

				await Task.Delay(999999999);
			}
		}

		public async Task StopWebApiPortForwardAsync(ControllerResourceObject controllerResourceObject, CancellationToken cancellationToken)
		{
			if (controllerResourceObject is null)
			{
				throw new ArgumentNullException(nameof(controllerResourceObject));
			}

			var portForwardName = GetServicePortForwarderName(controllerResourceObject);

			await _hostedServiceSystem.RemoveAsync(portForwardName);
		}

		public async Task<IEnumerable<V1CustomResourceDefinition>> GetWebApiCustomResourceDefinitionsAsync(ControllerResourceObject controllerResourceObject, CancellationToken cancellationToken)
		{
			return await Task.FromResult(new List<V1CustomResourceDefinition>());
		}

		public async Task DeleteCustomResourceDefinitionsAsync(IEnumerable<V1CustomResourceDefinition> customResourceDefinitions, CancellationToken cancellationToken)
		{
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
	}
}
