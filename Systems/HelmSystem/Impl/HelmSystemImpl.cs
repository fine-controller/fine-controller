using Common.Models;
using Common.Utils;
using k8s;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Systems.HelmSystem.Impl
{
	internal class HelmSystemImpl : IHelmSystem
	{
		private const string HELM_EXECUTABLE_FOLDER = "./KubernetesSystem/Helm";

		private readonly ILogger _logger;
		private readonly string _helmExecutableFile;
		
		public HelmSystemImpl
		(
			ILogger<HelmSystemImpl> logger,
			AppCancellationToken appCancellationToken
		)
		{
			if (logger is null)
			{
				throw new ArgumentNullException(nameof(logger));
			}

			if (appCancellationToken is null)
			{
				throw new ArgumentNullException(nameof(appCancellationToken));
			}

			_logger = logger;

			// Determine which executable to use

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				_logger.LogInformation("Operating System : Windows {OSArchitecture}", RuntimeInformation.OSArchitecture);

				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						_helmExecutableFile = $"{HELM_EXECUTABLE_FOLDER}/windows-amd64";
						break;

					default:
						throw new ApplicationException("Not supported");
				}
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				_logger.LogInformation("Operating System : OSX {OSArchitecture}", RuntimeInformation.OSArchitecture);

				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						_helmExecutableFile = $"{HELM_EXECUTABLE_FOLDER}/darwin-amd64";
						break;

					case Architecture.Arm64:
						_helmExecutableFile = $"{HELM_EXECUTABLE_FOLDER}/darwin-arm64";
						break;

					default:
						throw new ApplicationException("Not supported");
				}
			}
			else
			{
				_logger.LogInformation("Operating System : Linux {OSArchitecture}", RuntimeInformation.OSArchitecture);

				switch (RuntimeInformation.OSArchitecture)
				{
					case Architecture.X64:
						_helmExecutableFile = $"{HELM_EXECUTABLE_FOLDER}/linux-amd64";
						break;

					case Architecture.Arm64:
						_helmExecutableFile = $"{HELM_EXECUTABLE_FOLDER}/linux-arm64";
						break;

					default:
						throw new ApplicationException("Not supported");
				}
			}

			// Chmod the executable for non-windows OS

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				_logger.LogInformation("Chmoding {Executable}", _helmExecutableFile);
				ProcessUtil.ExecuteAsync("chmod", $"+x {_helmExecutableFile}", appCancellationToken.Token).GetAwaiter().GetResult();
			}
		}

		public async Task ApplyAsync<T>(IEnumerable<T> customResourceDefinitions, CancellationToken cancellationToken) where T : IKubernetesObject
		{
			var yaml = KubernetesYaml.SerializeAll(customResourceDefinitions.Select(x => (object)x));
			await Task.CompletedTask;
		}
	}
}
