using Common.Models;
using Common.Utils;
using Humanizer;
using k8s;
using k8s.Autorest;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Systems.BackgroundServiceSystem;
using Systems.KubernetesSystem.Models;

namespace Systems.KubernetesSystem.Impl
{
	internal class KindKubernetesSystemImpl : KubernetesSystemImpl
	{
		private readonly string _exampleYamlFile;
		private readonly string _kindExecutableFile;
		private readonly string _ingressNginxYamlFile;
		private readonly string _kubeCtlExecutableFile;
		private readonly string _clusterConfigurationYamlFile;

		public KindKubernetesSystemImpl
		(
			AppSettings appSettings,
			KubernetesClient kubernetesClient,
			ILogger<KindKubernetesSystemImpl> logger,
			IHostedServiceSystem hostedServiceSystem,
			AppCancellationToken appCancellationToken
		)
		: base
		(
			appSettings,
			kubernetesClient,
			logger,
			hostedServiceSystem
		)
		{
			_ = logger ?? throw new ArgumentNullException(nameof(logger));
			_ = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			_ = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));
			_ = hostedServiceSystem ?? throw new ArgumentNullException(nameof(hostedServiceSystem));
			_ = appCancellationToken ?? throw new ArgumentNullException(nameof(appCancellationToken));

			// vars

			var osPlatform = typeof(OSPlatform)
				.GetProperties(BindingFlags.Public | BindingFlags.Static)
				.Where(x => x.PropertyType == typeof(OSPlatform))
				.Select(x => (OSPlatform)x.GetValue(null))
				.Single(RuntimeInformation.IsOSPlatform);

			var executableFileName = $"{osPlatform}-{RuntimeInformation.OSArchitecture}".ToLower();
			var assetsFolder = Path.Combine(_appSettings.RootPath, "KubernetesSystem/Assets");
			var kubeCtlFolder = Path.Combine(assetsFolder, "KubeCtl");
			var kindFolder = Path.Combine(assetsFolder, "Kind");

			_exampleYamlFile = Path.Combine(kindFolder, "example.yaml");
			_kindExecutableFile = Path.Combine(kindFolder, executableFileName);
			_ingressNginxYamlFile = Path.Combine(kindFolder, "ingress-nginx.yaml");
			_kubeCtlExecutableFile = Path.Combine(kubeCtlFolder, executableFileName);
			_clusterConfigurationYamlFile = Path.Combine(kindFolder, "cluster-configuration.yaml");
			
			if (!File.Exists(_kindExecutableFile))
			{
				throw new FileNotFoundException($"{Path.GetFileName(_kindExecutableFile)} not found for your system, please add it to continue", _kindExecutableFile);
			}

			if (!File.Exists(_kubeCtlExecutableFile))
			{
				throw new FileNotFoundException($"{Path.GetFileName(_kubeCtlExecutableFile)} not found for your system, please add it continue", _kubeCtlExecutableFile);
			}

			if (osPlatform != OSPlatform.Windows)
			{
				ProcessUtil.ExecuteAsync("chmod", new[] { "+x", _kindExecutableFile }, appCancellationToken.Token).GetAwaiter().GetResult();
				ProcessUtil.ExecuteAsync("chmod", new[] { "+x", _kubeCtlExecutableFile }, appCancellationToken.Token).GetAwaiter().GetResult();
			}

			// Ensure Kind cluster is running

			EnsureClusterIsRunningAsync(appCancellationToken.Token).GetAwaiter().GetResult();

			// Create kubernetes client

			var kubeConfigYaml = GetClusterKubeConfigAsync(appCancellationToken.Token).GetAwaiter().GetResult();
			using var kubeConfigYamlStream = StreamUtils.FromString(kubeConfigYaml);
			var kubeConfig = KubernetesClientConfiguration.LoadKubeConfig(kubeConfigYamlStream);
			var kubeClientConfig = KubernetesClientConfiguration.BuildConfigFromConfigObject(kubeConfig);

			kubernetesClient.Client?.Dispose();
			kubernetesClient.Client = new Kubernetes(kubeClientConfig);

			// Ensure ingress-nginx is running

			EnsureIngressNginxIsRunningAsync(appCancellationToken.Token).GetAwaiter().GetResult();

			// Ensure example is running

			EnsureExampleIsRunningAsync(appCancellationToken.Token).GetAwaiter().GetResult();

			// Print KubeConfig

			_logger.LogInformation("Cluster KubeConfig : \n\n {KubeConfigYaml}", kubeConfigYaml);
		}

		private async Task<bool> DeploymentExistsAsync(string name, string @namespace, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentNullException(nameof(name));
			}

			if (string.IsNullOrWhiteSpace(@namespace))
			{
				throw new ArgumentNullException(nameof(@namespace));
			}

			try
			{
				var deployment = await _kubernetesClient.Client.AppsV1.ReadNamespacedDeploymentAsync(name, @namespace, cancellationToken: cancellationToken);
				return deployment is not null;
			}
			catch (HttpOperationException httpOperationException) when (httpOperationException.Response.StatusCode == HttpStatusCode.NotFound)
			{
				return false;
			}
		}

		private async Task<bool> DeploymentIsReadyAsync(string name, string @namespace, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentNullException(nameof(name));
			}

			if (string.IsNullOrWhiteSpace(@namespace))
			{
				throw new ArgumentNullException(nameof(@namespace));
			}

			try
			{
				var deployment = await _kubernetesClient.Client.AppsV1.ReadNamespacedDeploymentAsync(name, @namespace, cancellationToken: cancellationToken);
				return deployment.Status.ReadyReplicas == deployment.Status.Replicas;
			}
			catch (HttpOperationException httpOperationException) when (httpOperationException.Response.StatusCode == HttpStatusCode.NotFound)
			{
				return false;
			}
		}

		private async Task WaitForDeploymentAsync(string name, string @namespace, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentNullException(nameof(name));
			}

			if (string.IsNullOrWhiteSpace(@namespace))
			{
				throw new ArgumentNullException(nameof(@namespace));
			}

			_logger.LogInformation("Waiting For {Name}", name.Titleize());

			while (!await DeploymentIsReadyAsync(name, @namespace, cancellationToken))
			{
				await Task.Delay(2000, cancellationToken);
			}
		}

		private async Task ApplyYamlAsync(string yamlFilePath, string @namespace, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(yamlFilePath))
			{
				throw new ArgumentNullException(nameof(yamlFilePath));
			}

			var args = new List<string> { "apply", "-f", yamlFilePath };

			if (!string.IsNullOrWhiteSpace(@namespace))
			{
				args.AddRange(new[] { "-n", @namespace });
			}

			await ProcessUtil.ExecuteAsync(_kubeCtlExecutableFile, args, cancellationToken);
		}

		private async Task EnsureClusterIsRunningAsync(CancellationToken cancellationToken)
		{
			if (await ClusterExistsAsync(cancellationToken))
			{
				await WaitForClusterAsync(cancellationToken);
			}
			else
			{
				_logger.LogInformation("Creating Cluster {Name}", Constants.AppName);

				await CreateClusterAsync(cancellationToken);
				await WaitForClusterAsync(cancellationToken);
			}
		}

		private async Task<bool> ClusterExistsAsync(CancellationToken cancellationToken)
		{
			var result = await ProcessUtil.ExecuteAsync(_kindExecutableFile, new[] { "get", "clusters" }, cancellationToken);
			return result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Any(x => x.Equals(Constants.AppName));
		}

		private async Task CreateClusterAsync(CancellationToken cancellationToken)
		{
			await ProcessUtil.ExecuteAsync(_kindExecutableFile, new[] { "create", "cluster", "--name", Constants.AppName, "--config", _clusterConfigurationYamlFile }, cancellationToken);
		}

		private async Task WaitForClusterAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Waiting For Cluster");

			while (!await ClusterExistsAsync(cancellationToken))
			{
				await Task.Delay(2000, cancellationToken);
			}
		}

		private async Task<string> GetClusterKubeConfigAsync(CancellationToken cancellationToken)
		{
			return await ProcessUtil.ExecuteAsync(_kindExecutableFile, new[] { "get", "kubeconfig", "--name", Constants.AppName }, cancellationToken);
		}

		private async Task EnsureIngressNginxIsRunningAsync(CancellationToken cancellationToken)
		{
			var ingressNginx = "ingress-nginx";
			var ingressNginxController = "ingress-nginx-controller";

			if (await DeploymentExistsAsync(ingressNginxController, ingressNginx, cancellationToken))
			{
				await WaitForDeploymentAsync(ingressNginxController, ingressNginx, cancellationToken);
			}
			else
			{
				_logger.LogInformation("Installing {Name}", ingressNginx.Titleize());

				await ApplyYamlAsync(_ingressNginxYamlFile, ingressNginx, cancellationToken);
				await WaitForDeploymentAsync(ingressNginxController, ingressNginx, cancellationToken);
			}
		}

		private async Task EnsureExampleIsRunningAsync(CancellationToken cancellationToken)
		{
			var example = "example";

			if (await DeploymentExistsAsync(example, example, cancellationToken))
			{
				await WaitForDeploymentAsync(example, example, cancellationToken);
			}
			else
			{
				_logger.LogInformation("Installing {Name}", example.Titleize());

				await ApplyYamlAsync(_exampleYamlFile, example, cancellationToken);
				await WaitForDeploymentAsync(example, example, cancellationToken);
			}
		}
	}
}