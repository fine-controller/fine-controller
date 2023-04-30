using CliWrap;
using CliWrap.Buffered;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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

			try
			{
				var command = Cli.Wrap(targetFilePath);

				command = command.WithArguments(arguments);
				command = command.WithValidation(CommandResultValidation.None);

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
					throw new ProcessUtilException(string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError);
				}

				return result.StandardOutput;
			}
			catch (Exception exception)
			{
				if (errorOutputCallback is not null)
				{
					errorOutputCallback(exception.Message);
					return exception.Message;
				}
				else
				{
					throw new ProcessUtilException("Execution failed", exception);
				}
			}
		}
	}

	[Serializable]
	public class ProcessUtilException : ApplicationException
	{
		public ProcessUtilException(string message) : base(message) { }
		public ProcessUtilException(string message, Exception innerException) : base(message, innerException) { }
		protected ProcessUtilException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
