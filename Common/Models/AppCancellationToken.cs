using System.Threading;

namespace Common.Models
{
	public class AppCancellationToken
	{
		public CancellationToken Token => Source.Token;
		public CancellationTokenSource Source { get; } = new();
	}
}
