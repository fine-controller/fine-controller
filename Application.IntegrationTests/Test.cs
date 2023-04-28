using NUnit.Framework;

namespace Application.IntegrationTests
{
	public class Test
	{
		[Test]
		public void Test1() => Assert.That((object)true, Is.True);
	}
}
