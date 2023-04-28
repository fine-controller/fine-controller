using Common.Utils;
using NUnit.Framework;
using Shouldly;

namespace Application.IntegrationTests
{
	public class Test
	{
		[Test]
		public void Test1() => HashUtil.Hash("hello").ShouldNotBeNullOrWhiteSpace();
	}
}
