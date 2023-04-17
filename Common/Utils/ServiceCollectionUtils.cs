using Common.Models;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Common.Utils
{
	public static class ServiceCollectionUtils
	{
		public static IServiceCollection AddSingleton<TService, TRealImpl, TFakeImpl>(this IServiceCollection services, AppSettings appSettings) where TService : class where TRealImpl : class, TService where TFakeImpl : class, TService
		{
			if (services is null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			if (appSettings is null)
			{
				throw new ArgumentNullException(nameof(appSettings));
			}

			if (appSettings.IsProduction)
			{
				return services.AddSingleton<TService, TRealImpl>();
			}

			return services.AddSingleton<TService, TFakeImpl>();
		}

		public static IServiceCollection AddScoped<TService, TRealImpl, TFakeImpl>(this IServiceCollection services, AppSettings appSettings) where TService : class where TRealImpl : class, TService where TFakeImpl : class, TService
		{
			if (services is null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			if (appSettings is null)
			{
				throw new ArgumentNullException(nameof(appSettings));
			}

			if (appSettings.IsProduction)
			{
				return services.AddScoped<TService, TRealImpl>();
			}

			return services.AddScoped<TService, TFakeImpl>();
		}

		public static IServiceCollection AddTransient<TService, TRealImpl, TFakeImpl>(this IServiceCollection services, AppSettings appSettings) where TService : class where TRealImpl : class, TService where TFakeImpl : class, TService
		{
			if (services is null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			if (appSettings is null)
			{
				throw new ArgumentNullException(nameof(appSettings));
			}

			if (appSettings.IsProduction)
			{
				return services.AddTransient<TService, TRealImpl>();
			}

			return services.AddTransient<TService, TFakeImpl>();
		}
	}
}
