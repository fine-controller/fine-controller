using Microsoft.Extensions.DependencyInjection;
using System;

namespace Common.Utils
{
	// Why
	// - The general service provider does not communicate what Type of objects it resolves thereby reducing readability
	// - Also, the temptation is strong to wind up bypassing the contructor inject completly thereby reducing readability
	
	public interface IServiceProvider<T>
	{
		public T GetService();
		public T GetRequiredService();
	}

	public class ServiceProvider<T> : IServiceProvider<T>
	{
		private readonly IServiceProvider _serviceProvider;

		public ServiceProvider
		(
			IServiceProvider serviceProvider
		)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		public T GetService()
		{
			return _serviceProvider.GetService<T>();
		}

		public T GetRequiredService()
		{
			return _serviceProvider.GetRequiredService<T>();
		}
	}
}