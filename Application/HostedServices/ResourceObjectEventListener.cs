using Common.Models;
using Common.Utils;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Systems.KubernetesSystem.Models;

namespace Application.HostedServices
{
	internal class ResourceObjectEventListener : IHostedService
    {
		private Listener _listener;
		private FileStorage _fileStorage;
		private readonly ILogger _logger;
		private readonly AppData _appData;
		private readonly AppSettings _appSettings;
		private readonly KubernetesClient _kubernetesClient;

		public string Group { get; internal set; }
		public string Version { get; internal set; }
		public string NamePlural { get; internal set; }

		public ResourceObjectEventListener
		(
			AppData appData,
			AppSettings appSettings,
			KubernetesClient kubernetesClient,
			ILogger<ResourceObjectEventListener> logger
		)
        {
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_appData = appData ?? throw new ArgumentNullException(nameof(appData));
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			_kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));
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
			// init

			CheckParameters();

			// vars

			var isReconnecting = false;
			var logTag = $"{NameUtil.GetLongKind(Group, Version, NamePlural)} Events";

			_fileStorage = new FileStorage(Path.Combine(_appSettings.DataPath, NameUtil.GetLongKind(Group, Version, NamePlural)));

			// connect

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

						isReconnecting = true; // it's all reconnections from now on

						// dispose current listener (if any) and create a new one

						DisposeListener();

						_listener = new(logTag, _logger);

						// start streaming

						_logger.LogInformation("{LogTag} : {Message}", logTag, "Streaming");

						// stream deletions

						foreach (var deletedResourceObjectFile in _fileStorage.RemoveFilesExcept(((JsonElement)await _kubernetesClient.Client.CustomObjects.ListClusterCustomObjectAsync(Group, Version, NamePlural, allowWatchBookmarks: false, watch: false, cancellationToken: cancellationToken)).GetProperty("items").Deserialize<IEnumerable<JsonObject>>().Select(item => GetResourceObjectFileName(item[Constants.MetadataCamelCase]?[Constants.NamespaceCamelCase]?.GetValue<string>(), item[Constants.MetadataCamelCase]?[Constants.NameCamelCase]?.GetValue<string>()))))
						{
							var deleteResourceObjectJson = await File.ReadAllTextAsync(deletedResourceObjectFile.FilePath, cancellationToken);
							var deleteResourceObjectJsonObject = JsonNode.Parse(deleteResourceObjectJson).AsObject();
							await OnEventAsync(WatchEventType.Deleted, deleteResourceObjectJsonObject, cancellationToken);
							deletedResourceObjectFile.RemoveFile();
						}

						// stream new events

						_listener.HttpOperationResponse = await _kubernetesClient.Client.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync(Group, Version, NamePlural, allowWatchBookmarks: false, watch: true, cancellationToken: cancellationToken);

						_listener.Watcher = _listener.HttpOperationResponse.Watch
						(
							onEvent: async (WatchEventType eventType, object eventData) =>
							{
								if (eventType == WatchEventType.Error)
								{
									// note it

									_logger.LogError("{LogTag} : Error (GONE suspected)", logTag);

									// reconnect

									connect();

									// return

									return;
								}

								await OnEventAsync(eventType, ((JsonElement)eventData).Deserialize<JsonObject>(), cancellationToken);
							},
							onError: exception =>
							{
								_logger.LogError(exception, "{LogTag} : Error", logTag);
							},
							onClosed: () =>
							{
								_logger.LogWarning("{LogTag} : Closed", logTag);
								connect();// reconnect
							}
						);
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

		private void CheckParameters()
		{
			if (string.IsNullOrWhiteSpace(Group) || Group == "-")
			{
				Group = string.Empty;
			}

			if (string.IsNullOrWhiteSpace(Version))
			{
				throw new InvalidOperationException($"{nameof(Version)} is required");
			}

			if (string.IsNullOrWhiteSpace(NamePlural))
			{
				throw new InvalidOperationException($"{nameof(NamePlural)} is required");
			}

			Group = Group?.Trim();
			Version = Version.Trim();
			NamePlural = NamePlural.Trim();
		}

		private async Task OnEventAsync(WatchEventType eventType, JsonObject eventData, CancellationToken cancellationToken)
		{
			// vars

			var resourceObject = new ResourceObject(eventType, eventData);
			var resourceObjectFileName = GetResourceObjectFileName(resourceObject.Namespace(), resourceObject.Name());

			// store

			await _fileStorage.WriteFileAsync(resourceObjectFileName, resourceObject.Data.ToString(), cancellationToken);

			// add to queue

			try
			{
				await _appData.ResourceObjects.AddAsync(resourceObject, cancellationToken);
			}
			catch (Exception exception)
			{
				var resourceObjectLongName = resourceObject.LongName;

				_logger.LogError(exception, "Failed to process event | ResourceObject : {ResourceObjectLongName} | EventType : {EventType}", resourceObjectLongName, eventType);
			}

			// unstore

			if (eventType == WatchEventType.Deleted)
			{
				_fileStorage.RemoveFile(resourceObjectFileName);
			}
		}

		private string GetResourceObjectFileName(string @namespace, string name)
		{
			return $"{@namespace}/{name}".Replace('\\', '/').Trim('/');
		}

		public async Task StopAsync(CancellationToken cancellationToken)
        {
			DisposeListener();
			await Task.CompletedTask;
        }

		private sealed class Listener : IDisposable
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
				GC.SuppressFinalize(this);

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
