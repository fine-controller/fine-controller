using Common.Models;
using Common.Utils;
using k8s;
using k8s.Models;
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
		private const string KIND_FOLDER = "./KubernetesSystem/Assets/Kind";
		private const string EXAMPLE_YAML_FILE = $"{KIND_FOLDER}/example.yaml";
		private const string KUBE_CTL_FOLDER = "./KubernetesSystem/Assets/KubeCtl";
		private const string INGRESS_NGINX_YAML_FILE = $"{KIND_FOLDER}/ingress-nginx.yaml";
		private const string CLUSTER_CONFIGURATION_YAML_FILE = $"{KIND_FOLDER}/cluster-configuration.yaml";
		
		private readonly string _kindExecutableFile;
		private readonly string _kubeCtlExecutableFile;

		public KindKubernetesSystemImpl
		(
			AppSettings appSettings,
			KubernetesClient kubernetesClient,
			ILogger<KindKubernetesSystemImpl> logger,
			IHostedServiceSystem hostedServiceSystem,
			AppCancellationToken appCancellationToken,
			IServiceProvider<ResourceObjectEventStreamer> resourceObjectEventStreamerProvider
		)
		: base
		(
			appSettings,
			kubernetesClient,
			logger,
			hostedServiceSystem,
			appCancellationToken,
			resourceObjectEventStreamerProvider
		)
		{
			_ = logger ?? throw new ArgumentNullException(nameof(logger));
			_ = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			_ = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));
			_ = hostedServiceSystem ?? throw new ArgumentNullException(nameof(hostedServiceSystem));
			_ = appCancellationToken ?? throw new ArgumentNullException(nameof(appCancellationToken));
			_ = resourceObjectEventStreamerProvider ?? throw new ArgumentNullException(nameof(resourceObjectEventStreamerProvider));

			// Determine which executable to use
			
			_logger.LogInformation("Operating System : Windows {OSArchitecture}", RuntimeInformation.OSArchitecture);

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						_kindExecutableFile = $"{KIND_FOLDER}/windows-amd64";
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
						_kindExecutableFile = $"{KIND_FOLDER}/darwin-amd64";
						_kubeCtlExecutableFile = $"{KUBE_CTL_FOLDER}/darwin-amd64";
						break;

					case Architecture.Arm64:
						_kindExecutableFile = $"{KIND_FOLDER}/darwin-arm64";
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
						_kindExecutableFile = $"{KIND_FOLDER}/linux-amd64";
						_kubeCtlExecutableFile = $"{KUBE_CTL_FOLDER}/linux-amd64";
						break;

					case Architecture.Arm64:
						_kindExecutableFile = $"{KIND_FOLDER}/linux-arm64";
						_kubeCtlExecutableFile = $"{KUBE_CTL_FOLDER}/linux-arm64";
						break;

					default:
						throw new ApplicationException("Not supported");
				}
			}

			// Chmod the executable for non-windows OS

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				ProcessUtil.ExecuteAsync("chmod", new[] { "+x", _kindExecutableFile }, _appCancellationToken.Token).GetAwaiter().GetResult();
				ProcessUtil.ExecuteAsync("chmod", new[] { "+x", _kubeCtlExecutableFile }, _appCancellationToken.Token).GetAwaiter().GetResult();
			}

			// Create Kind cluster (if it does not already exist)

			if (IsClusterRunningAsync().GetAwaiter().GetResult())
			{
				WaitForClusterAsync().GetAwaiter().GetResult();
			}
			else
			{
				_logger.LogInformation("Creating cluster {Name}", Constants.FineKubeOperator);

				CreateClusterAsync().GetAwaiter().GetResult();
				WaitForClusterAsync().GetAwaiter().GetResult();
			}

			// Create kubernetes client

			var kubeConfigYaml = GetClusterKubeConfigAsync().GetAwaiter().GetResult();
			using var kubeConfigYamlStream = StreamUtils.FromString(kubeConfigYaml);
			var kubeConfig = KubernetesClientConfiguration.LoadKubeConfig(kubeConfigYamlStream);
			var kubeClientConfig = KubernetesClientConfiguration.BuildConfigFromConfigObject(kubeConfig);

			kubernetesClient.Client?.Dispose();
			kubernetesClient.Client = new Kubernetes(kubeClientConfig);

			// Install ingress-nginx (if it's not already installed)

			if (IsIngressNginxInstalledAsync().GetAwaiter().GetResult())
			{
				WaitForIngressNginxAsync().GetAwaiter().GetResult();
			}
			else
			{
				_logger.LogInformation("Installing Ingress-Nginx");

				InstallIngressNginxAsync().GetAwaiter().GetResult();
				WaitForIngressNginxAsync().GetAwaiter().GetResult();
			}

			// Install example (if it's not already installed)

			if (IsExampleInstalledAsync().GetAwaiter().GetResult())
			{
				WaitForExampleAsync().GetAwaiter().GetResult();
			}
			else
			{
				_logger.LogInformation("Installing Example");

				InstallExampleAsync().GetAwaiter().GetResult();
				WaitForExampleAsync().GetAwaiter().GetResult();
			}

			// Print KubeConfig

			_logger.LogInformation("Cluster KubeConfig : \n\n {KubeConfigYaml}", kubeConfigYaml);
		}

		private async Task<bool> IsClusterRunningAsync()
		{
			var result = await ProcessUtil.ExecuteAsync(_kindExecutableFile, new[] { "get", "clusters" }, _appCancellationToken.Token);
			return result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Any(x => x.Equals(Constants.FineKubeOperator));
		}

		private async Task<bool> IsIngressNginxInstalledAsync()
		{
			var namespaces = (await _kubernetesClient.Client.ListNamespaceAsync(cancellationToken: _appCancellationToken.Token)).Items.Select(x => x.Name());
			if (!namespaces.Any(@namespace => @namespace == "ingress-nginx")) return false;

			var result = await ProcessUtil.ExecuteAsync(_kubeCtlExecutableFile, new[] { "get", "--namespace", "ingress-nginx", "--no-headers=true", "deployment/ingress-nginx-controller" }, _appCancellationToken.Token);
			return result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Any(x => x.StartsWith("ingress-nginx-controller"));
		}

		private async Task<bool> IsIngressNginxRunningAsync()
		{
			var result = await ProcessUtil.ExecuteAsync(_kubeCtlExecutableFile, new[] { "get", "--namespace", "ingress-nginx", "--no-headers=true", "deployment/ingress-nginx-controller" }, _appCancellationToken.Token);
			return result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Any(x => x.Contains("1/1"));
		}

		private async Task<bool> IsExampleInstalledAsync()
		{
			var namespaces = (await _kubernetesClient.Client.ListNamespaceAsync(cancellationToken: _appCancellationToken.Token)).Items.Select(x => x.Name());
			if (!namespaces.Any(@namespace => @namespace == "example")) return false;

			var result = await ProcessUtil.ExecuteAsync(_kubeCtlExecutableFile, new[] { "get", "--namespace", "example", "--no-headers=true", "deployment/example" }, _appCancellationToken.Token);
			return result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Any(x => x.StartsWith("example"));
		}

		private async Task<bool> IsExampleRunningAsync()
		{
			var result = await ProcessUtil.ExecuteAsync(_kubeCtlExecutableFile, new[] { "get", "--namespace", "example", "--no-headers=true", "deployment/example" }, _appCancellationToken.Token);
			return result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Any(x => x.Contains("1/1"));
		}

		private async Task CreateClusterAsync()
		{
			await ProcessUtil.ExecuteAsync(_kindExecutableFile, new[] { "create", "cluster", "--name", Constants.FineKubeOperator, "--config", CLUSTER_CONFIGURATION_YAML_FILE }, _appCancellationToken.Token);
		}

		private async Task InstallIngressNginxAsync()
		{
			await ProcessUtil.ExecuteAsync(_kubeCtlExecutableFile, new[] { "apply", "-n", "ingress-nginx", "-f", INGRESS_NGINX_YAML_FILE }, _appCancellationToken.Token);
		}

		private async Task InstallExampleAsync()
		{
			await ProcessUtil.ExecuteAsync(_kubeCtlExecutableFile, new[] { "apply", "-f", EXAMPLE_YAML_FILE }, _appCancellationToken.Token);
		}

		private async Task WaitForClusterAsync()
		{
			_logger.LogInformation("Waiting for Cluster");

			while (!await IsClusterRunningAsync())
			{
				await Task.Delay(2000);
			}
		}

		private async Task WaitForIngressNginxAsync()
		{
			_logger.LogInformation("Waiting for Ingress-Nginx");

			while (!await IsIngressNginxRunningAsync())
			{
				await Task.Delay(2000);
			}
		}

		private async Task WaitForExampleAsync()
		{
			_logger.LogInformation("Waiting for Example");

			while (!await IsExampleRunningAsync())
			{
				await Task.Delay(2000);
			}
		}

		private async Task<string> GetClusterKubeConfigAsync()
		{
			return await ProcessUtil.ExecuteAsync(_kindExecutableFile, new[] { "get", "kubeconfig", "--name", Constants.FineKubeOperator }, _appCancellationToken.Token);
		}
	}
}