using Common.Models;
using Common.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Systems.KubernetesSystem.HostedServices
{
	internal class ServicePortForwarder : IHostedService
	{
		private readonly ILogger _logger;

		public int LocalPort { get; set; }
		public int ServicePort { get; set; }
		public string Namespace { get; set; }
		public string ServiceName { get; set; }
		public string KubeCtlExecutableFile { get; set; }
		
		public ServicePortForwarder
		(
			ILogger<ServicePortForwarder> logger
		)
		{
			_logger = logger;
		}
		
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(Namespace))
			{
				throw new ApplicationException(nameof(Namespace));
			}

			if (string.IsNullOrWhiteSpace(ServiceName))
			{
				throw new ApplicationException(nameof(ServiceName));
			}

			if (string.IsNullOrWhiteSpace(KubeCtlExecutableFile))
			{
				throw new ApplicationException(nameof(KubeCtlExecutableFile));
			}

			var isReconnecting = false;
			var logTag = $"Port Forward {NameUtil.GetResourceObjectKindLongName(string.Empty, Constants.V1CamelCase, Constants.ServicePascalCase)}/{ServiceName}/{ServicePort} -> localhost:{LocalPort}";

			void connect()
			{
				Task.Run(async () =>
				{
					try
					{
						if (cancellationToken.IsCancellationRequested)
						{
							_logger.LogInformation("{LogTag} : Exiting", logTag);
							return;
						}

						_logger.LogInformation("{LogTag} : {Message}", logTag, isReconnecting ? "Reconnecting" : "Connecting");

						if (isReconnecting)
						{
							await Task.Delay(2000, cancellationToken);
						}

						isReconnecting = true; // from this moment onwards it's reconnections

						await ProcessUtil.ExecuteAsync(KubeCtlExecutableFile, $"port-forward --namespace {Namespace} service/{ServiceName} {LocalPort}:{ServicePort}", cancellationToken);
					}
					catch (Exception exception)
					{
						_logger.LogError(exception, "{LogTag} : Error", logTag);
						connect();
					}
				}, cancellationToken);
			}

			connect();

			await Task.CompletedTask;
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await Task.CompletedTask;
		}
	}
}
