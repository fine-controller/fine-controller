using Common;
using Common.Models;
using Common.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using Systems.BackgroundServiceSystem;
using Systems.HostedServiceSystem.Impl;
using Systems.KubernetesSystem;
using Systems.KubernetesSystem.HostedServices;
using Systems.KubernetesSystem.Impl;
using Systems.KubernetesSystem.Models;

namespace Systems
{
    public static class Startup
	{
		public static IServiceCollection AddSystems(this IServiceCollection services, AppSettings appSettings)
		{
			if (services is null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			if (appSettings is null)
			{
				throw new ArgumentNullException(nameof(appSettings));
			}

			services.AddCommon(appSettings);
			
			services.AddSingleton<KubernetesClient>();
			services.AddTransient<ResourceObjectEventStreamer>();
			services.AddSingleton<IHostedServiceSystem, HostedServiceSystemImpl>();
			services.AddSingleton<IKubernetesSystem, KubernetesSystemImpl, KindKubernetesSystemImpl>(appSettings);

			return services;
		}
	}
}
