using Common.Models;
using Common.Utils;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Systems.BackgroundServiceSystem;
using Systems.KubernetesSystem.HostedServices;
using Systems.KubernetesSystem.Models;

namespace Systems.KubernetesSystem.Impl
{
    internal class KindKubernetesSystemImpl : KubernetesSystemImpl
	{
		private const int HTTP_PORT = 8880;
		private const int HTTPS_PORT = 8881;
		private const string KIND_FOLDER = "./KubernetesSystem/Assets/Kind";
		private const string INGRESS_NGINX_FILE = $"{KIND_FOLDER}/ingress-nginx.yaml";
		private const string CLUSTER_CONFIGURATION_FILE = $"{KIND_FOLDER}/cluster-configuration.yaml";
		private static readonly string INGRESS_TEMPLATE = File.ReadAllText($"{KIND_FOLDER}/ingress-template.yaml");

		private readonly string _kindExecutableFile;

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
						break;

					case Architecture.Arm64:
						_kindExecutableFile = $"{KIND_FOLDER}/darwin-arm64";
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
						break;

					case Architecture.Arm64:
						_kindExecutableFile = $"{KIND_FOLDER}/linux-arm64";
						break;

					default:
						throw new ApplicationException("Not supported");
				}
			}

			// Chmod the executable for non-windows OS

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				ProcessUtil.ExecuteAsync("chmod", new[] { "+x", _kindExecutableFile }, _appCancellationToken.Token).GetAwaiter().GetResult();
			}

			// Create Kind cluster (if it does not already exist)

			if (IsClusterRunningAsync().GetAwaiter().GetResult())
			{
				WaitForClusterAsync().GetAwaiter().GetResult();
			}
			else
			{
				_logger.LogInformation("Creating cluster {Name}", Constants.FineController);

				CreateClusterAsync().GetAwaiter().GetResult();
				WaitForClusterAsync().GetAwaiter().GetResult();
			}

			// Create kubernetes client

			var configYaml = GetClusterConfigAsync().GetAwaiter().GetResult();
			using var configYamlStream = StreamUtils.FromString(configYaml);
			var kubeConfig = KubernetesClientConfiguration.LoadKubeConfig(configYamlStream);
			var kubernetesClientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigObject(kubeConfig);

			kubernetesClient.Client?.Dispose();
			kubernetesClient.Client = new Kubernetes(kubernetesClientConfiguration);

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

			// Cluster KubeConfig

			_logger.LogInformation("Cluster KubeConfig : \n\n {ConfigYaml}", configYaml);
		}

		private async Task<bool> IsClusterRunningAsync()
		{
			var result = await ProcessUtil.ExecuteAsync(_kindExecutableFile, new[] { "get", "clusters" }, _appCancellationToken.Token);
			return result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Any(x => x.Equals(Constants.FineController));
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

		private async Task CreateClusterAsync()
		{
			await ProcessUtil.ExecuteAsync(_kindExecutableFile, new[] { "create", "cluster", "--name", Constants.FineController, "--config", CLUSTER_CONFIGURATION_FILE }, _appCancellationToken.Token);
		}

		private async Task InstallIngressNginxAsync()
		{
			await ProcessUtil.ExecuteAsync(_kubeCtlExecutableFile, new[] { "apply", "-n", "ingress-nginx", "-f", INGRESS_NGINX_FILE }, _appCancellationToken.Token);
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

		private async Task<string> GetClusterConfigAsync()
		{
			return await ProcessUtil.ExecuteAsync(_kindExecutableFile, new[] { "get", "kubeconfig", "--name", Constants.FineController }, _appCancellationToken.Token);
		}

		internal override async Task<string> GetWebApiUrlAsync(WebApiResourceObject webApiResourceObject, CancellationToken cancellationToken)
		{
			if (webApiResourceObject is null)
			{
				throw new ArgumentNullException(nameof(webApiResourceObject));
			}

			var portResourceObject = webApiResourceObject.Spec?.Ports?.SingleOrDefault(x => x.Port == webApiResourceObject.FineControllerPort);

			if (portResourceObject is null)
			{
				throw new ApplicationException($"Service does not have port '{webApiResourceObject.FineControllerPort}'");
			}

			if (portResourceObject.Protocol?.Equals("TCP", StringComparison.OrdinalIgnoreCase) != true)
			{
				throw new ApplicationException($"Service '{webApiResourceObject.FineControllerPort}' is not 'TCP' protocol");
			}

			// add/update ingress

			var ingressResourceObject = KubernetesYaml.Deserialize<V1Ingress>(INGRESS_TEMPLATE.Replace("{{service-namespace}}", webApiResourceObject.Namespace()).Replace("{{service-name}}", webApiResourceObject.Name()).Replace("{{service-port}}", webApiResourceObject.FineControllerPort?.ToString()));

			try
			{
				await _kubernetesClient.Client.NetworkingV1.ReplaceNamespacedIngressAsync(ingressResourceObject, webApiResourceObject.Name(), webApiResourceObject.Namespace(), cancellationToken: cancellationToken);
			}
			catch (HttpOperationException exception) when (exception.Response.StatusCode == HttpStatusCode.NotFound)
			{
				await _kubernetesClient.Client.NetworkingV1.CreateNamespacedIngressAsync(ingressResourceObject, webApiResourceObject.Namespace(), cancellationToken: cancellationToken);
			}

			// compose url

			var scheme = webApiResourceObject.FineControllerHttps ? "https" : "http";
			var port = webApiResourceObject.FineControllerHttps ? HTTPS_PORT : HTTP_PORT;
			var url = $"{scheme}://localhost:{port}/{webApiResourceObject.Name()}";

			// return

			return url;
		}
	}
}