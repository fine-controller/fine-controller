using CliWrap;
using CliWrap.Buffered;
using Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Utils
{
	public static class ProcessUtil
	{
		public static async Task<string> ExecuteAsync(string targetFilePath, IEnumerable<string> arguments, CancellationToken cancellationToken, Action<string> standardOutputCallback = null, Action<string> errorOutputCallback = null)
		{
			if (string.IsNullOrWhiteSpace(targetFilePath))
			{
				throw new ArgumentNullException(nameof(targetFilePath));
			}

			if (arguments is null)
			{
				throw new ArgumentNullException(nameof(arguments));
			}

			var command = Cli.Wrap(targetFilePath);
			
			command = command.WithArguments(arguments);
			
			if (standardOutputCallback is not null)
			{
				command = command.WithStandardOutputPipe(PipeTarget.ToDelegate(standardOutputCallback));
			}
			
			if (errorOutputCallback is not null)
			{
				command = command.WithStandardErrorPipe(PipeTarget.ToDelegate(errorOutputCallback));
			}

			var result = await command.ExecuteBufferedAsync(cancellationToken);
			
			if (result.ExitCode != 0)
			{
				throw new AppException(result.StandardError);
			}

			return result.StandardOutput;
		}
	}
}
