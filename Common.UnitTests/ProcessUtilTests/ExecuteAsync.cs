using Common.Utils;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.UnitTests.ProcessUtilTests
{
	[TestFixture]
	public class ExecuteAsync
	{
		[Test]
		public void WhenTargetFilePathIsNull_ThrowsArgumentNullException()
		{
			// arrange

			var targetFilePath = default(string);
			var arguments = new List<string> { RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "-n" : "-c", "5", "localhost" };
			var cancellationToken = CancellationToken.None;

			// act

			var exception = Assert.ThrowsAsync<ArgumentNullException>(async () => await ProcessUtil.ExecuteAsync(targetFilePath, arguments, cancellationToken));

			// assert

			exception.Message.ShouldContain("Value cannot be null.");
			exception.ParamName.ShouldBe(nameof(targetFilePath));
		}

		[Test]
		public void WhenTargetFilePathIsEmpty_ThrowsArgumentNullException()
		{
			// arrange

			var targetFilePath = "";
			var arguments = new List<string> { RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "-n" : "-c", "5", "localhost" };
			var cancellationToken = CancellationToken.None;

			// act

			var exception = Assert.ThrowsAsync<ArgumentNullException>(async () => await ProcessUtil.ExecuteAsync(targetFilePath, arguments, cancellationToken));

			// assert

			exception.Message.ShouldContain("Value cannot be null.");
			exception.ParamName.ShouldBe(nameof(targetFilePath));
		}

		[Test]
		public void WhenTargetFilePathIsWhiteSpace_ThrowsArgumentNullException()
		{
			// arrange

			var targetFilePath = "  ";
			var arguments = new List<string> { RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "-n" : "-c", "5", "localhost" };
			var cancellationToken = CancellationToken.None;

			// act

			var exception = Assert.ThrowsAsync<ArgumentNullException>(async () => await ProcessUtil.ExecuteAsync(targetFilePath, arguments, cancellationToken));

			// assert

			exception.Message.ShouldContain("Value cannot be null.");
			exception.ParamName.ShouldBe(nameof(targetFilePath));
		}

		[Test]
		public void WhenArgumentsIsNull_ThrowsArgumentNullException()
		{
			// arrange

			var targetFilePath = "ping";
			var arguments = default(IEnumerable<string>);
			var cancellationToken = CancellationToken.None;

			// act

			var exception = Assert.ThrowsAsync<ArgumentNullException>(async () => await ProcessUtil.ExecuteAsync(targetFilePath, arguments, cancellationToken));

			// assert

			exception.Message.ShouldContain("Value cannot be null.");
			exception.ParamName.ShouldBe(nameof(arguments));
		}

		[Test]
		public void WhenCommandFails_ThrowsAppException_NoneExistant()
		{
			// arrange

			var targetFilePath = "nonexistent-command";
			var arguments = new List<string> { RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "-n" : "-c", "5", "localhost" };
			var cancellationToken = CancellationToken.None;

			// act

			var exception = Assert.ThrowsAsync<ProcessUtilException>(async () => await ProcessUtil.ExecuteAsync(targetFilePath, arguments, cancellationToken));

			// assert

			exception.Message.ShouldBe("Execution failed");
			exception.InnerException.ShouldNotBeNull();
		}

		[Test]
		public void WhenCommandFails_ThrowsAppException_InvalidOptions()
		{
			// arrange

			var targetFilePath = "ping";
			var arguments = new List<string> { RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "-n" : "-c", "5" };
			var cancellationToken = CancellationToken.None;

			// act

			var exception = Assert.ThrowsAsync<ProcessUtilException>(async () => await ProcessUtil.ExecuteAsync(targetFilePath, arguments, cancellationToken));

			// assert

			exception.Message.ShouldBe("Execution failed");
			exception.InnerException.ShouldNotBeNull();
		}

		[Test]
		public void WhenCommandFails_WithErrorCallback_DoesNotThrow_ReturnsErrorMessage_And_CallbackProvidesErrorMessage()
		{
			// arrange

			var result = default(string);
			var targetFilePath = "nonexistent-command";
			var errorCallbackOutput = new StringBuilder();
			var arguments = new List<string> { RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "-n" : "-c", "5", "localhost" };
			var cancellationToken = CancellationToken.None;

			// act

			Assert.DoesNotThrowAsync(async () => result = await ProcessUtil.ExecuteAsync(targetFilePath, arguments, cancellationToken, errorOutputCallback: output => errorCallbackOutput.AppendLine(output)));

			// assert

			result = string.Join(Environment.NewLine, result?.Trim('\r', '\n')?.Trim()?.Split('\r', '\n', StringSplitOptions.RemoveEmptyEntries));
			var errorCallbackOutputString = string.Join(Environment.NewLine, errorCallbackOutput.ToString()?.Trim('\r', '\n')?.Trim()?.Split('\r', '\n', StringSplitOptions.RemoveEmptyEntries));

			result.ShouldNotBeNullOrWhiteSpace();
			errorCallbackOutputString.ShouldNotBeNullOrWhiteSpace();
			result.ShouldBe(errorCallbackOutputString);
		}

		[Test]
		public async Task WhenCommandSucceeds_ReturnsStandardOutput()
		{
			// arrange

			var targetFilePath = "ping";
			var arguments = new List<string> { RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "-n" : "-c", "5", "localhost" };
			var cancellationToken = CancellationToken.None;

			// act

			var result = await ProcessUtil.ExecuteAsync(targetFilePath, arguments, cancellationToken);

			// assert

			result.ShouldNotBeNullOrWhiteSpace();
			result.Trim().Split().Length.ShouldBeGreaterThanOrEqualTo(5);
		}

		[Test]
		public async Task WhenCommandSucceeds_WithSuccessCallback_ReturnsStandardOutput_And_CallbackProvidesStandardOutput()
		{
			// arrange

			var targetFilePath = "ping";
			var standardCallbackOutput = new StringBuilder();
			var arguments = new List<string> { RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "-n" : "-c", "5", "localhost" };
			var cancellationToken = CancellationToken.None;

			// act

			var result = await ProcessUtil.ExecuteAsync(targetFilePath, arguments, cancellationToken, standardOutputCallback: output => standardCallbackOutput.AppendLine(output));

			// assert

			result = string.Join(Environment.NewLine, result?.Trim('\r', '\n')?.Trim()?.Split('\r', '\n', StringSplitOptions.RemoveEmptyEntries));
			var standardCallbackOutputString = string.Join(Environment.NewLine, standardCallbackOutput.ToString()?.Trim('\r', '\n')?.Trim()?.Split('\r', '\n', StringSplitOptions.RemoveEmptyEntries));

			result.ShouldNotBeNullOrWhiteSpace();
			standardCallbackOutputString.ShouldNotBeNullOrWhiteSpace();
			result.ShouldBe(standardCallbackOutputString);
		}
	}
}
