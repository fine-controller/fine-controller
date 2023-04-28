using Common.Models;
using Common.Utils;
using k8s;
using k8s.Autorest;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Systems.KubernetesSystem.Models;

namespace Systems.KubernetesSystem.HostedServices
{
	internal class ResourceObjectEventStreamer : IHostedService
    {
		private Listener _listener;
		private readonly ILogger _logger;
		private readonly AppSettings _appSettings;
		private readonly KubernetesClient _kubernetesClient;
		private readonly IEnumerable<IResourceObjectEventHandler> _resourceObjectEventHandlers;

		public string Group { get; internal set; }
		public string Version { get; internal set; }
		public string NamePlural { get; internal set; }

		public ResourceObjectEventStreamer
		(
			AppSettings appSettings,
			KubernetesClient kubernetesClient,
			ILogger<ResourceObjectEventStreamer> logger,
            IEnumerable<IResourceObjectEventHandler> resourceObjectEventHandlers
        )
        {
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			_kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));
			_resourceObjectEventHandlers = resourceObjectEventHandlers ?? throw new ArgumentNullException(nameof(resourceObjectEventHandlers));
			_ = kubernetesClient.Client ?? throw new ArgumentException($"{nameof(kubernetesClient.Client)} is required", nameof(kubernetesClient));
		}

		private void DisposeListener()
		{
			try
			{
				_listener?.Dispose();
			}
			catch (Exception exception)
			{
				_logger.LogWarning(exception, "Failed to dispose listener");
			}
		}

        public async Task StartAsync(CancellationToken cancellationToken)
        {
			if (string.IsNullOrWhiteSpace(Group) || Group == "-")
			{
				Group = string.Empty;
			}

			if (string.IsNullOrWhiteSpace(Version))
			{
				throw new ApplicationException($"{nameof(Version)} is required");
			}

			if (string.IsNullOrWhiteSpace(NamePlural))
			{
				throw new ApplicationException($"{nameof(NamePlural)} is required");
			}

			Group = Group?.Trim();
			Version = Version.Trim();
			NamePlural = NamePlural.Trim();
			
			var isReconnecting = false;

			var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = _appSettings.IsProduction ? -1 : 1,
			};

			var logTag = $"{NameUtil.GetKindLongName(Group, Version, NamePlural)} Events";

			void connect()
			{
				Task.Run(async () =>
				{
					try
					{
						// exit if cancellation token is cancelled

						if (cancellationToken.IsCancellationRequested)
						{
							_logger.LogInformation("{LogTag} : Exiting", logTag);
							return;
						}

						// delay if reconnecting

						if (isReconnecting)
						{
							await Task.Delay(2000, cancellationToken);
						}
						else
						{
							isReconnecting = true; // it's all reconnections from now on
						}

						// dispose current listener (if any) and create a new one

						DisposeListener();
						_listener = new(logTag, _logger);

						// start streaming

						_logger.LogInformation("{LogTag} : {Message}", logTag, "Streaming");

						_listener.HttpOperationResponse = await _kubernetesClient.Client.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync(Group, Version, NamePlural, allowWatchBookmarks: false, watch: true, cancellationToken: cancellationToken);
						
						_listener.Watcher = _listener.HttpOperationResponse.Watch((WatchEventType eventType, object eventData) =>
						{
							if (eventType == WatchEventType.Error)
							{
								// TODO : How do i know it's GONE
								_logger.LogError("{LogTag} : Error (GONE suspected)", logTag);
								connect(); // reconnect
								return;
							}

							var resourceObject = new ResourceObject(((JsonElement)eventData).Deserialize<JsonObject>(), eventType);

							Parallel.ForEach(_resourceObjectEventHandlers, parallelOptions, async resourceObjectEventHandler =>
							{
								try
								{
									await resourceObjectEventHandler.HandleAsync(resourceObject, cancellationToken);
								}
								catch (Exception exception)
								{
									var resourceObjectLongName = resourceObject.LongName;
									var handlerName = resourceObjectEventHandler.GetType().Name;
									
									_logger.LogError(exception, "Failed to process event | Handler : {HandlerName} | ResourceObject : {ResourceObjectLongName} | EventType : {EventType}", handlerName, resourceObjectLongName, eventType);
								}
							});
						},
						onError: exception =>
						{
							_logger.LogError(exception, "{LogTag} : Error", logTag);
						},
						onClosed: () =>
						{
							_logger.LogWarning("{LogTag} : Closed", logTag);
							connect();// reconnect
						});
					}
					catch (Exception exception)
					{
						_logger.LogError(exception, "{LogTag} : Error", logTag);
						connect(); // reconnect
					}
				}, cancellationToken);
			}

			// connect for the first time

			connect();

			// force async

			await Task.CompletedTask;
		}

        public async Task StopAsync(CancellationToken cancellationToken)
        {
			DisposeListener();
			await Task.CompletedTask;
        }

		private class Listener : IDisposable
		{
			private readonly string _logTag;
			private readonly ILogger _logger;

			public Watcher<object> Watcher { get; internal set; }
			public HttpOperationResponse<object> HttpOperationResponse { get; internal set; }

			public Listener
			(
				string logTag,
				ILogger logger
			)
			{
				_logTag = logTag ?? throw new ArgumentNullException(nameof(logTag));
				_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			}

			public void Dispose()
			{
				try
				{
					Watcher?.Dispose();
				}
				catch (Exception exception)
				{
					_logger.LogWarning(exception, "{LogTag} : Failed to dispose watcher", _logTag);
				}

				try
				{
					HttpOperationResponse?.Dispose();
				}
				catch (Exception exception)
				{
					_logger.LogWarning(exception, "{LogTag} : Failed to dispose http operation response", _logTag);
				}
			}
		}
	}
}
