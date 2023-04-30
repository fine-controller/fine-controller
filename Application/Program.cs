using Application.HostedServices;
using Common.Models;
using Common.Utils;
using k8s.Autorest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Systems;
using Systems.ApiSystem;
using Systems.BackgroundServiceSystem;
using Systems.KubernetesSystem;

// vars

var assembly = Assembly.GetExecutingAssembly();
var builder = Host.CreateApplicationBuilder(args);
var appCancellationToken = new AppCancellationToken();
var appSettings = builder.Configuration.Get<AppSettings>();

// initialize app settings

appSettings.IsProduction = builder.Environment.IsProduction();
appSettings.RootPath = Path.GetDirectoryName(assembly.Location);
Validator.ValidateObject(appSettings, new ValidationContext(appSettings), true);

// initialize services

builder.Services.AddSystems(appSettings);
builder.Services.AddSingleton(appCancellationToken);
builder.Services.AddTransient<ResourceObjectEventListener>();
builder.Services.AddSingleton<ResourceObjectEventDispatcher>();

// initialize app

var app = builder.Build();
var appData = app.Services.GetRequiredService<AppData>();
var apiSystem = app.Services.GetRequiredService<IApiSystem>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var kubernetesSystem = app.Services.GetRequiredService<IKubernetesSystem>();
var hostedServiceSystem = app.Services.GetRequiredService<IHostedServiceSystem>();
var hostApplicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

while (!await apiSystem.IsRunningAsync(appCancellationToken.Token))
{
	logger.LogInformation("Waiting for API to start : {BaseUrl}", apiSystem.GetBaseUrl());
	await Task.Delay(1000);
}

hostApplicationLifetime.ApplicationStarted.Register(async () =>
{
	try
	{
		// start event dispatcher

		await apiSystem.InitializeAsync(appCancellationToken.Token);
		var resourceObjectEventDispatcher = app.Services.GetRequiredService<ResourceObjectEventDispatcher>();
		await hostedServiceSystem.AddAsync(nameof(resourceObjectEventDispatcher), resourceObjectEventDispatcher, appCancellationToken.Token);

		// start event listeners

		await kubernetesSystem.AddOrUpdateCustomResouceDefinitionsAsync(appData.CustomResourceDefinitions, appCancellationToken.Token);

		foreach (var customResourceSpec in appData.CustomResourceDefinitions.Select(x => x.Spec))
		{
			var customResourceListener = app.Services.GetRequiredService<ResourceObjectEventListener>();

			customResourceListener.Group = customResourceSpec.Group;
			customResourceListener.Version = customResourceSpec.Versions[0]!.Name;
			customResourceListener.NamePlural = customResourceSpec.Names.Plural;

			await hostedServiceSystem.AddAsync(Guid.NewGuid().ToString(), customResourceListener, appCancellationToken.Token);
		}

		foreach (var knownKindApiEndpoints in appData.KnownKindApiEndpoints)
		{
			var knownResourceListener = app.Services.GetRequiredService<ResourceObjectEventListener>();

			knownResourceListener.Group = knownKindApiEndpoints.GroupLowerCase;
			knownResourceListener.Version = knownKindApiEndpoints.VersionLowerCase;
			knownResourceListener.NamePlural = knownKindApiEndpoints.KnownKindLowerCase;

			await hostedServiceSystem.AddAsync(Guid.NewGuid().ToString(), knownResourceListener, appCancellationToken.Token);
		}
	}
	catch (Exception exception)
	{
		if (exception is HttpOperationException httpOperationException && httpOperationException.Response.StatusCode == HttpStatusCode.Forbidden)
		{
			logger.LogError(exception, "Failed to start because of a 'Forbidden' error : Please check RBAC for {AppName} container", Constants.AppName);
		}
		else
		{
			logger.LogError(exception, "Failed to start");
		}

		hostApplicationLifetime.StopApplication();
	}
});

hostApplicationLifetime.ApplicationStopping.Register(() =>
{
	appCancellationToken.Source.Cancel();
});

// run app

await app.RunAsync();