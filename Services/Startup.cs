using Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Services.Impl;
using System;
using Systems;

namespace Services
{
    public static class Startup
	{
		public static IServiceCollection AddServices(this IServiceCollection services, AppSettings appSettings)
		{
			if (services is null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			if (appSettings is null)
			{
				throw new ArgumentNullException(nameof(appSettings));
			}

			services.AddSystems(appSettings);
			services.AddSingleton<IResourceObjectEventEventService, ResourceObjectEventEventServiceImpl>();
			services.AddSingleton<IWebApiResourceObjectService, WebApiResourceObjectServiceImpl>();

			return services;
		}
	}
}
