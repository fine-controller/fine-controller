using Common.Models;
using Common.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using Systems.KubernetesSystem;

// vars

var assembly = Assembly.GetExecutingAssembly();
var builder = Host.CreateApplicationBuilder(args);
var appCancellationToken = new AppCancellationToken();
var appSettings = builder.Configuration.Get<AppSettings>();
var eventProcessorType = typeof(IResourceObjectEventHandler);

// initialize app settings

appSettings.IsProduction = builder.Environment.IsProduction();
appSettings.RootPath = Path.GetDirectoryName(assembly.Location);
Validator.ValidateObject(appSettings, new ValidationContext(appSettings), true);

// initialize services

builder.Services.AddServices(appSettings);
builder.Services.AddSingleton(appCancellationToken);

assembly
	.GetTypes()
	.Where(type => eventProcessorType.IsAssignableFrom(type) && !type.IsInterface)
	.ToList().ForEach(type => builder.Services.AddSingleton(eventProcessorType, type));

// initialize app

var app = builder.Build();
var appLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

appLifetime.ApplicationStarted.Register(() =>
{
	app.Services
		.GetRequiredService<IKubernetesSystem>()
		.StartStreamingResourceObjectEventsAsync(string.Empty, Constants.V1CamelCase, Constants.ServicesCamelCase, appCancellationToken.Token);
});

appLifetime.ApplicationStopping.Register(() =>
{
	appCancellationToken.Source.Cancel();
});

// run app

await app.RunAsync();