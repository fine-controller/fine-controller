using Common.Models;
using Common.Utils;
using k8s;
using k8s.Autorest;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
						throw new ApplicationException("Not Supported");
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
						throw new ApplicationException("Not Supported");
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
						throw new ApplicationException("Not Supported");
				}
			}

			// Chmod the executable for non-windows OS

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				ProcessUtil.ExecuteAsync("chmod", new[] { "+x", _kindExecutableFile }, _appCancellationToken.Token).GetAwaiter().GetResult();
				ProcessUtil.ExecuteAsync("chmod", new[] { "+x", _kubeCtlExecutableFile }, _appCancellationToken.Token).GetAwaiter().GetResult();
			}

			// Create Kind cluster (if it does not already exist)

			if (IsClusterReadyAsync().GetAwaiter().GetResult())
			{
				WaitForClusterAsync().GetAwaiter().GetResult();
			}
			else
			{
				_logger.LogInformation("Creating Cluster {Name}", Constants.FineKubeOperator);

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

		private async Task<bool> IsDeploymentInstalledAsync(string name, string @namespace)
		{
			try
			{
				var deployment = await _kubernetesClient.Client.AppsV1.ReadNamespacedDeploymentAsync(name, @namespace, cancellationToken: _appCancellationToken.Token);
				return deployment is not null;
			}
			catch (HttpOperationException httpOperationException) when (httpOperationException.Response.StatusCode == HttpStatusCode.NotFound)
			{
				return false;
			}
		}

		private async Task<bool> IsDeploymentReadyAsync(string name, string @namespace)
		{
			try
			{
				var deployment = await _kubernetesClient.Client.AppsV1.ReadNamespacedDeploymentAsync(name, @namespace, cancellationToken: _appCancellationToken.Token);
				return deployment.Status.ReadyReplicas == deployment.Status.Replicas;
			}
			catch (HttpOperationException httpOperationException) when (httpOperationException.Response.StatusCode == HttpStatusCode.NotFound)
			{
				return false;
			}
		}

		private async Task ApplyYamlAsync(string yamlFilePath, string @namespace = null)
		{
			var args = new List<string> { "apply", "-f", yamlFilePath };
			
			if (!string.IsNullOrWhiteSpace(@namespace))
			{
				args.AddRange(new[] { "-n", @namespace });
			}

			await ProcessUtil.ExecuteAsync(_kubeCtlExecutableFile, args, _appCancellationToken.Token);
		}

		private async Task<bool> IsClusterReadyAsync()
		{
			var result = await ProcessUtil.ExecuteAsync(_kindExecutableFile, new[] { "get", "clusters" }, _appCancellationToken.Token);
			return result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Any(x => x.Equals(Constants.FineKubeOperator));
		}

		private async Task<bool> IsIngressNginxInstalledAsync()
		{
			return await IsDeploymentInstalledAsync("ingress-nginx-controller", "ingress-nginx");
		}

		private async Task<bool> IsIngressNginxReadyAsync()
		{
			return await IsDeploymentReadyAsync("ingress-nginx-controller", "ingress-nginx");
		}

		private async Task<bool> IsExampleInstalledAsync()
		{
			return await IsDeploymentInstalledAsync("example", "example");
		}

		private async Task<bool> IsExampleReadyAsync()
		{
			return await IsDeploymentReadyAsync("example", "example");
		}

		private async Task CreateClusterAsync()
		{
			await ProcessUtil.ExecuteAsync(_kindExecutableFile, new[] { "create", "cluster", "--name", Constants.FineKubeOperator, "--config", CLUSTER_CONFIGURATION_YAML_FILE }, _appCancellationToken.Token);
		}

		private async Task InstallIngressNginxAsync()
		{
			await ApplyYamlAsync(INGRESS_NGINX_YAML_FILE, "ingress-nginx");
		}

		private async Task InstallExampleAsync()
		{
			await ApplyYamlAsync(EXAMPLE_YAML_FILE, "example");
		}

		private async Task WaitForClusterAsync()
		{
			_logger.LogInformation("Waiting For Cluster");

			while (!await IsClusterReadyAsync())
			{
				await Task.Delay(2000);
			}
		}

		private async Task WaitForIngressNginxAsync()
		{
			_logger.LogInformation("Waiting For Ingress-Nginx");

			while (!await IsIngressNginxReadyAsync())
			{
				await Task.Delay(2000);
			}
		}

		private async Task WaitForExampleAsync()
		{
			_logger.LogInformation("Waiting For Example");

			while (!await IsExampleReadyAsync())
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