using CliWrap;
using CliWrap.Buffered;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Utils
{
	public class ProcessUtil
	{
		public static async Task<string> ExecuteAsync(string targetFilePath, string arguments, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(targetFilePath))
			{
				throw new ArgumentNullException(nameof(targetFilePath));
			}

			if (arguments is null)  // empty/whitespace acceptable
			{
				throw new ArgumentNullException(nameof(arguments));
			}

			var result = await Cli.Wrap(targetFilePath)
					.WithArguments(arguments)
					.ExecuteBufferedAsync(cancellationToken);

			if (result.ExitCode != 0)
			{
				throw new ApplicationException(result.StandardError);
			}

			return result.StandardOutput;
		}
	}
}
