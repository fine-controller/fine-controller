using Common.Interfaces;
using Common.Models;
using Common.Utils;
using k8s;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Systems.BackgroundServiceSystem;
using Systems.KubernetesSystem.HostedServices;
using Systems.KubernetesSystem.Models;

namespace Systems.KubernetesSystem.Impl
{
	internal class KindKubernetesSystemImpl : KubernetesSystemImpl
	{
		private const string CLUSTER_NAME = "fine-controller";
		private const string KIND_EXECUTABLE_FOLDER = "./KubernetesSystem/Kind";

		private readonly ILogger _logger;
		private readonly string _kindExecutableFile;
		private readonly AppCancellationToken _appCancellationToken;

		public KindKubernetesSystemImpl
		(
			KubernetesClient kubernetesClient,
			ILogger<KindKubernetesSystemImpl> logger,
			IHostedServiceSystem hostedServiceSystem,
			AppCancellationToken appCancellationToken,
			IServiceProvider<ServicePortForwarder> servicePortForwarderProvider,
			IServiceProvider<ResourceObjectEventStreamer> resourceObjectEventStreamerProvider
		)
		: base
		(
			kubernetesClient,
			logger,
			hostedServiceSystem,
			appCancellationToken,
			servicePortForwarderProvider,
			resourceObjectEventStreamerProvider
		)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_ = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));
			_ = hostedServiceSystem ?? throw new ArgumentNullException(nameof(hostedServiceSystem));
			_ = servicePortForwarderProvider ?? throw new ArgumentNullException(nameof(servicePortForwarderProvider));
			_appCancellationToken = appCancellationToken ?? throw new ArgumentNullException(nameof(appCancellationToken));
			_ = resourceObjectEventStreamerProvider ?? throw new ArgumentNullException(nameof(resourceObjectEventStreamerProvider));

			// Starting

			_logger.LogInformation("Starting Kind cluster : {Name}", CLUSTER_NAME);

			// Determine which executable to use

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				_logger.LogInformation("Operating System : Windows {OSArchitecture}", RuntimeInformation.OSArchitecture);

				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						_kindExecutableFile = $"{KIND_EXECUTABLE_FOLDER}/windows-amd64";
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
						_kindExecutableFile = $"{KIND_EXECUTABLE_FOLDER}/darwin-amd64";
						break;

					case Architecture.Arm64:
						_kindExecutableFile = $"{KIND_EXECUTABLE_FOLDER}/darwin-arm64";
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
						_kindExecutableFile = $"{KIND_EXECUTABLE_FOLDER}/linux-amd64";
						break;

					case Architecture.Arm64:
						_kindExecutableFile = $"{KIND_EXECUTABLE_FOLDER}/linux-arm64";
						break;

					default:
						throw new ApplicationException("Not supported");
				}
			}

			// Chmod the executable for non-windows OS

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				_logger.LogInformation("Chmoding {Executable}", _kindExecutableFile);
				ProcessUtil.ExecuteAsync("chmod", $"+x {_kindExecutableFile}", _appCancellationToken.Token).GetAwaiter().GetResult();
			}

			// Create the Kind cluster (if it does not already exist)

			if (!IsClusterRunningAsync().GetAwaiter().GetResult())
			{
				_logger.LogInformation("Creating cluster {Name}", CLUSTER_NAME);
				CreateClusterAsync().GetAwaiter().GetResult();
				_logger.LogInformation("Cluster created");
			}

			// Create kubernetes client

			var configYaml = GetClusterConfigAsync().GetAwaiter().GetResult();
			using var configYamlStream = StreamUtils.FromString(configYaml);
			var kubeConfig = KubernetesClientConfiguration.LoadKubeConfig(configYamlStream);
			var kubernetesClientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigObject(kubeConfig);

			kubernetesClient.Client?.Dispose();
			kubernetesClient.Client = new Kubernetes(kubernetesClientConfiguration);

			_logger.LogInformation($"Cluster KubeConfig:\n\n{{ConfigYaml}}", configYaml);
		}

		private async Task<bool> IsClusterRunningAsync()
		{
			var result = await ProcessUtil.ExecuteAsync(_kindExecutableFile, "get clusters", _appCancellationToken.Token);
			return result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Any(x => x.Equals(CLUSTER_NAME));
		}

		private async Task CreateClusterAsync()
		{
			await ProcessUtil.ExecuteAsync(_kindExecutableFile, $"create cluster --name {CLUSTER_NAME}", _appCancellationToken.Token);

			while (!await IsClusterRunningAsync())
			{
				await Task.Delay(1000);
			}
		}

		private async Task<string> GetClusterConfigAsync()
		{
			return await ProcessUtil.ExecuteAsync(_kindExecutableFile, $"get kubeconfig --name {CLUSTER_NAME}", _appCancellationToken.Token);
		}
	}
}