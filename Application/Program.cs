using Common.Models;
using Common.Utils;
using k8s.Autorest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Systems.ApiSystem;
using Systems.KubernetesSystem;

// vars

var assembly = Assembly.GetExecutingAssembly();
var builder = Host.CreateApplicationBuilder(args);
var appCancellationToken = new AppCancellationToken();
var appSettings = builder.Configuration.Get<AppSettings>();
var resourceObjectEventHandlerType = typeof(IResourceObjectEventHandler);

// initialize app settings

appSettings.IsProduction = builder.Environment.IsProduction();
appSettings.RootPath = Path.GetDirectoryName(assembly.Location);
appSettings.API_HOST = appSettings.API_HOST?.Trim('"', '\'', ' ')?.Trim();
appSettings.API_GROUP = appSettings.API_GROUP?.Trim('"', '\'', ' ')?.Trim();
appSettings.API_SPEC_PATH = appSettings.API_SPEC_PATH?.Trim('"', '\'', ' ', '/')?.Trim();
Validator.ValidateObject(appSettings, new ValidationContext(appSettings), true);

// initialize services

builder.Services.AddServices(appSettings);
builder.Services.AddSingleton(appCancellationToken);

assembly
	.GetTypes()
	.Where(type => resourceObjectEventHandlerType.IsAssignableFrom(type) && !type.IsInterface)
	.ToList().ForEach(type => builder.Services.AddSingleton(resourceObjectEventHandlerType, type));

// initialize app

var app = builder.Build();
var appData = app.Services.GetRequiredService<AppData>();
var apiSystem = app.Services.GetRequiredService<IApiSystem>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var kubernetesSystem = app.Services.GetRequiredService<IKubernetesSystem>();
var appLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

while (!await apiSystem.IsRunningAsync(appCancellationToken.Token))
{
	logger.LogInformation("Waiting for API to start : {BaseUrl}", apiSystem.GetBaseUrl());
	await Task.Delay(1000);
}

appLifetime.ApplicationStarted.Register(async () =>
{
	try
	{
		await apiSystem.LoadSpecAsync(appCancellationToken.Token);
		await kubernetesSystem.AddOrUpdateCustomResouceDefinitionsAsync(appData.CustomResourceDefinitions, appCancellationToken.Token);

		foreach (var definition in appData.CustomResourceDefinitions)
		{
			await kubernetesSystem.StartStreamingResourceObjectEventsAsync(definition.Spec.Group, definition.Spec.Versions[0].Name, definition.Spec.Names.Plural, appCancellationToken.Token);
		}

		foreach (var knownKindApiEndpoints in appData.KnownKindApiEndpoints)
		{
			await kubernetesSystem.StartStreamingResourceObjectEventsAsync(knownKindApiEndpoints.GroupLowerCase ?? string.Empty, knownKindApiEndpoints.VersionLowerCase, knownKindApiEndpoints.KnownKindLowerCase, appCancellationToken.Token);
		}
	}
	catch (Exception exception)
	{
		if (exception is HttpOperationException httpOperationException && httpOperationException.Response.StatusCode == HttpStatusCode.Forbidden)
		{
			logger.LogError(exception, "Failed to start because of a 'Forbidden' error : Please check RBAC for {FineController} container", Constants.FineController);
		}
		else
		{
			logger.LogError(exception, "Failed to start");
		}

		Environment.Exit(1);
	}
});

appLifetime.ApplicationStopping.Register(() =>
{
	appCancellationToken.Source.Cancel();
});

// run app

await app.RunAsync();